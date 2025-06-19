if (document.getElementById("DefaultActions") && defaultHtml)
	document.getElementById("DefaultActions").innerHTML = defaultHtml;
if (document.getElementById("FormatValue") && formatHtml)
	document.getElementById("FormatValue").innerHTML = formatHtml;
if (document.getElementById("EncoderActions") && encoderHtml)
	document.getElementById("EncoderActions").innerHTML = encoderHtml;
if (document.getElementById("FontOptions") && fontHtml)
	document.getElementById("FontOptions").innerHTML = fontHtml;
if (document.getElementById("GuardActions") && guardHtml)
	document.getElementById("GuardActions").innerHTML = guardHtml;

var settingsModel = {};

var websocket = null,
	uuid = null,
	inInfo = null,
	actionInfo = {},
	displayInfo = {},
	ImageDictionary = {};

function fillImageSelectBox(imageList, selectFile, configured) {
	var selectedOption = null;
	var firstoption = null;
	Object.entries(imageList).forEach(([key, value]) => {
		var option = document.createElement("option");
		option.text = key;
		option.value = value;
		if (value == configured && configured != null) {
			option.selected = true;
			selectedOption = option;
		}
		if (firstoption == null) {
			firstoption = option;
		}
		selectFile.add(option);
	});
	
	if (selectedOption == null) {
		selectedOption = firstoption;
		selectedOption.selected = true;
	}
	
	return selectedOption;
}

function fillImageSelectGroup(dictionary, elementID, configured) {
	var selectDir = document.getElementById("Dir" + elementID);
	var selectFile = document.getElementById(elementID);
	if (!selectDir || !selectFile)
		return;

	var imageList = null
	Object.entries(dictionary).forEach(([key, value]) => {
		var option = document.createElement("option");
		option.text = key;
		option.value = key;
		if (configured != null && configured.indexOf(key) !== -1) {
			option.selected = true;
			imageList = value;
		}
		selectDir.add(option);
	});
	
	if (imageList == null) {
		selectDir.selectedOption = "/";
		imageList = dictionary["/"];
	}

	var selectedOption = fillImageSelectBox(imageList, selectFile, configured);
	if (selectedOption.value != configured)
		setSettings(selectedOption.value, elementID);
}

function setImagePreview(elementID) {
	if (document.getElementById(elementID) && document.getElementById("Prev_" + elementID)) {
		var img = document.getElementById(elementID).value;
		if (!img)
			return;
		document.getElementById("Prev_" + elementID).src = "../../" + img;
		var alt = img.replace(".png", "");
		alt = alt.substring(alt.lastIndexOf("/") + 1);
		document.getElementById("Prev_" + elementID).alt = alt;
	}
}

function fillImageSelectBoxes() {
	imageSelectBoxes.forEach((id) => {
		fillImageSelectGroup(ImageDictionary, id, settingsModel[id])
	});
	updateImagePreviews();
}

function updateImageSelectBox(target, value) {
	var selectFile = document.getElementById(target);
	if (!selectFile)
		return;

	selectFile.innerHTML = "";
	imageList = ImageDictionary[value];
	var selectedOption = fillImageSelectBox(imageList, selectFile, "");
	setSettings(selectedOption.value, target);
	setImagePreview(target);
}

function updateImagePreviews() {
	imageSelectBoxes.forEach((id) => setImagePreview(id));
}

function fillTypeSelectBox(values, elementID, configured) {
	var element = document.getElementById(elementID);
	if (!element)
		return;

	Object.entries(values).forEach(([key, value]) => {
		var option = document.createElement("option");
		option.text = key;
		option.value = value;
		if (value == configured)
			option.selected = true;
		element.add(option);
	});
}

function fillActionSelectBoxes(actionList) {
	fillTypeSelectBox(actionList, 'ActionType', settingsModel.ActionType);
	fillTypeSelectBox(actionList, 'ActionTypeLong', settingsModel.ActionTypeLong);
	fillTypeSelectBox(actionList, 'ActionTypeGuard', settingsModel.ActionTypeGuard);
	if (settingsModel.IsEncoder) {
		fillTypeSelectBox(actionList, 'ActionTypeLeft', settingsModel.ActionTypeLeft);
		fillTypeSelectBox(actionList, 'ActionTypeRight', settingsModel.ActionTypeRight);
		fillTypeSelectBox(actionList, 'ActionTypeTouch', settingsModel.ActionTypeTouch);
	}
}

function refreshSettings(settings) {
	if (settings) {
		for (var key in settings) {
			settingsModel[key] = settings[key];
			var elem = document.getElementById(key);
			if (elem && elem.type == "checkbox") {
				elem.checked = settingsModel[key];
			}
			else if (elem) {
				elem.value = settingsModel[key];
			}
		}
	}
}

function toggleConfigItem(value, name) {
	var key = "Config_" + name;
	var element = document.getElementById(key);

	if (!element)
		return;

	if (value) {
		element.style.display = displayInfo[key];
	}
	else if (element.style.display != "none") {
		displayInfo[key] = element.style.display;
		element.style.display = "none";
	}
}

function setFormItem(value, name) {
	var element = document.getElementById(name);
	if (!element)
		return;

	if (value) {
		element.style.display = displayInfo[name];
	}
	else if (element.style.display != "none") {
		displayInfo[name] = element.style.display;
		element.style.display = "none";
	}
}

function setPattern(field, type, donotrequest) {
	if (!document.getElementById(field)) {
		return;
	}

	var regName = "[^:\\s][a-zA-Z0-9\\x2D\\x5F]+";
	var regNameXP = "[^:\\s][a-zA-Z0-9\\x2D\\x5F\\x2B]+";
	var validNameKvar = "[^:\\s][a-zA-Z0-9\\x2D\\x5F\\x2E]+|#[0-9]+";
	var regNameMultipleXP = "[a-zA-Z0-9\\x2D\\x5F\\x2B]+";
	var regLVarName = "[^:\\s][a-zA-Z0-9\\x2D\\x5F\\x2E\\x20]+([\\x3A][0-9]+){0,1}";	
	var regLvar = `^((L:|[^:0-9\\x2F]){1}(${regLVarName})){1}$`;
	var strHvar = `((?!K:)(?!B:)(H:|[^:0-9]){1}${regName}(:[0-9]+){0,1}){1}`;
	var regHvar = `^(${strHvar}){1}(:${strHvar})*$`;
	var regDref = `^((${regNameXP}[\\x2F]){1}(${regNameMultipleXP}[\\x2F])*${regNameMultipleXP}){1}(([\\x5B][0-9]+[\\x5D])|(:s[0-9]+)){0,1}$`;
	var strPathXP = `(${regNameXP}[\\x2F]){1}(${regNameMultipleXP}[\\x2F])*(${regNameMultipleXP}){1}`;
	var regCmdXP = `^(${strPathXP}){1}(:${strPathXP})*$`;
	var regOffset = "^((0x){0,1}[0-9A-Fa-f]{4}:[0-9]{1,4}((:[ifsa]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$";
	var regAvar = `^^\\(((A|E|L):){0,1}([\\w][\\w ]+(:\\d+){0,1}),\\s{0,1}([\\w][\\w/ ]+)\\)$`;
	var regBvarValue = `^(B:${regName}){1}$`;
	var strBvarCmd = `((B:){0,1}${regName}(:[\\x2D\\x2B]{0,1}[0-9]+([\\x2C\\x2E]{1}[0-9]+){0,1}){0,1}){1}`;
	var regBvarCmd = `^(${strBvarCmd}){1}(:${strBvarCmd})*$`;
	var validKvar = `((?!lua:)(?!H:)(?!B:)(K:|[^:0-9]){1}${validNameKvar}){1}`;
	var rxKvarVariable = `^K:[^0-9]${validNameKvar}$`;
	var rxKvarCmd = `^${validKvar}:(0x){0,1}[0-9]+(:${validKvar}:(0x){0,1}[0-9]+)+$|^${validKvar}(:(0x){0,1}[0-9]+){0,5}$`;
	var regLuaFunc = `^(Lua|lua|LUA){1}:${regName}(\\.lua){0,1}(:${regName}){1}(\\({1}[^\\)]+\\){1}){0,1}$`;
	var regInternal = `^(X:){1}${regName}$`;
	var regCalcRead = `^C:[^\\s].+$`
	
	if (type == 0) //macro
		document.getElementById(field).pattern = `^([^:0-9]{1}${regName}:(${regName}){0,1}(:${regName}){0,}){1}$`;
	else if (type == 1) //script
		document.getElementById(field).pattern = `^(Lua(Set|Clear|Toggle|Value)?:){1}${regName}(:[0-9]{1,4})*$`;
	else if (type == 2) //control
		document.getElementById(field).pattern = "^^([0-9]+)$|^(([0-9]+\\=[0-9]+(:[0-9]+)*){1}(:([0-9]+\\=[0-9]+(:[0-9]+)*){1})*)$";
	else if (type == 3)  //lvar
		document.getElementById(field).pattern = regLvar;
	else if (type == 4)  //offset
		document.getElementById(field).pattern = regOffset;
	else if (type == 5) //offset | lvar | dref | avar | bvar | luafunc | internal | calcRead
		document.getElementById(field).pattern = `${regOffset}|${regDref}|${regAvar}|${regBvarValue}|${regInternal}|${rxKvarVariable}|${regLuaFunc}|${regCalcRead}|${regLvar}`;
	else if (type == 6) //vjoy
		document.getElementById(field).pattern = "^(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$";
	else if (type == 7) //vjoy Drv
		document.getElementById(field).pattern = "^(1[0-6]|[0-9]){1}:([0-9]|[0-9]{2}|1[0-1][0-9]|12[0-8]){1}(:t)?$";
	else if (type == 8) //HVar
		document.getElementById(field).pattern = regHvar;
	else if (type == 10) //XPCmd
		document.getElementById(field).pattern = regCmdXP;
	else if (type == 11) //XPWRef
		document.getElementById(field).pattern = regDref;
	else if (type == 12) //Avar
		document.getElementById(field).pattern = regAvar;
	else if (type == 13 && donotrequest) //Bvar Cmd
		document.getElementById(field).pattern = regBvarCmd;
	else if (type == 13 && !donotrequest) //Bvar Value
		document.getElementById(field).pattern = regBvarValue;
	else if (type == 14) //LuaFunc
		document.getElementById(field).pattern = regLuaFunc;
	else if (type == 15) //Internal
		document.getElementById(field).pattern = regInternal;
	else if (type == 16) //Kvar
		document.getElementById(field).pattern = rxKvarCmd;
	else
		document.getElementById(field).pattern = ".*";
}

function isValueCommand(actionType, doNotRequest) {
	return actionType == 3 || actionType == 4 || actionType == 11 || actionType == 12 || actionType == 15 || (actionType == 13 && !doNotRequest);
	//lvar, offset, xpwref, avar, internal, bvar requested
}

function isNonvalueCommand(actionType, doNotRequest) {
	return actionType == 0 || actionType == 1 || actionType == 2 || actionType == 6 || actionType == 7 || actionType == 8
		|| actionType == 9 || actionType == 10 || actionType == 14 || actionType == 16 || (actionType == 13 && doNotRequest);
	//macro, script, control, vjoy, vjoydrv, hvar, calc, xpcmd, luafunc, kvar, bvar not requested
}

function isToggleable(actionType, doNotRequest) {
	return isNonvalueCommand(actionType, doNotRequest);
}

function isHoldableCommand(actionType, address, doNotRequest) {
	if (actionType == 6 || actionType == 7) { //vjoys
		return isVjoyToggle(actionType, address);
	}
	else {
		return isNonvalueCommand(actionType, doNotRequest);
	}
}

function isHoldableValue(actionType, doNotRequest) {
	return isValueCommand(actionType, doNotRequest);
}

function isHoldable(actionType, doNotRequest) {
	return isNonvalueCommand(actionType, doNotRequest) || isValueCommand(actionType, doNotRequest);
}

function isResetable(actionType, doNotRequest) {
	return isValueCommand(actionType, doNotRequest);
}

function hasCommandDelay(actionType, doNotRequest) {
	return actionType == 0 || actionType == 1 || actionType == 2 || actionType == 8 || actionType == 10 || (actionType == 13 && doNotRequest) || actionType == 16;
	//macro, script, control, hvar, xpcmd, bvar not requested, kvar
}

function isVjoyToggle(actionType, address) {
	return (actionType == 6 && address.includes(":t")) || (actionType == 7 && address.includes(":t"));
}

function isLongPressAllowed(actionType, address, holdswitch) {
	return !holdswitch && (
		(actionType != 6 && actionType != 7)
		|| ((actionType == 6 || actionType == 7) && isVjoyToggle(actionType, address))
	) 
}

function isBvar(actionType) {
	return actionType == 13;
}

function isXpCmd(actionType) {
	return actionType == 10;
}

function setChecked(state, name) {
	var element = document.getElementById(name);
	if (!element)
		return;

	if (element.checked != state)
		element.checked = state;
}

function setResetField(state) {
	toggleConfigItem(state, 'UseLvarReset');
}

function setToggleField(state) {
	toggleConfigItem(state, 'ToggleSwitch');
}

function setHoldField(state) {
	toggleConfigItem(state, 'HoldSwitch');
}

function setBvarRequestField(state) {
	toggleConfigItem(state, 'DoNotRequestBvar');
}

function setUseXpCommandOnceField(state) {
	toggleConfigItem(state, 'UseXpCommandOnce');
}

function setOnOffFields(deckType, state) {
	var stateOn = `Config_SwitchOnState${deckType}`
	var stateOff = `Config_SwitchOffState${deckType}`
	setFormItem(state, stateOn);
	setFormItem(state, stateOff);
}

function checkButtonSettings(settingsModel) {
	if (settingsModel.ToggleSwitch && !isToggleable(settingsModel.ActionType, settingsModel.DoNotRequestBvar)) {
		settingsModel.ToggleSwitch = false;
		setChecked(false, 'ToggleSwitch');
	}
	if (settingsModel.HoldSwitch && !isHoldable(settingsModel.ActionType, settingsModel.DoNotRequestBvar)) {
		settingsModel.HoldSwitch = false;
		setChecked(false, 'HoldSwitch');
	}
	if (settingsModel.UseLvarReset && !isResetable(settingsModel.ActionType, settingsModel.DoNotRequestBvar)) {
		settingsModel.UseLvarReset = false;
		setChecked(false, 'UseLvarReset');
		setChecked(false, 'DoNotRequestBvar')
	}
	if (settingsModel.UseControlDelay && !hasCommandDelay(settingsModel.ActionType, settingsModel.DoNotRequestBvar)) {
		settingsModel.UseControlDelay = false;
		setChecked(false, 'UseControlDelay');
	}
}

function setActionFields(deckType, actionType, settingsModel) {
	//On/Off Fields
	if (deckType != "Long" && deckType != "Guard") {
		if (isValueCommand(actionType, settingsModel.DoNotRequestBvar, settingsModel.DoNotRequestBvar)) {
			setOnOffFields(deckType, true);
		}
		else {
			setOnOffFields(deckType, false);
		}
	}

	if (deckType == "") {
		if (settingsModel.ToggleSwitch) {
			var isToggle = isToggleable(actionType, settingsModel.DoNotRequestBvar);
			setToggleField(true);
			setHoldField(false);
			setResetField(false);
			toggleConfigItem(isToggle, 'AddressActionOff');
			toggleConfigItem(isToggle, 'AddressMonitor');
			setOnOffFields(deckType, isToggle);		
			setBvarRequestField(false);
			setUseXpCommandOnceField(false);
		}
		else if (settingsModel.HoldSwitch) {
			setToggleField(false);
			setHoldField(true);
			setResetField(false);
			toggleConfigItem(isHoldableCommand(actionType, settingsModel.AddressAction, settingsModel.DoNotRequestBvar), 'AddressActionOff');
			toggleConfigItem(false, 'AddressMonitor');
			setOnOffFields(deckType, isHoldableValue(actionType, settingsModel.DoNotRequestBvar));
			setBvarRequestField(isBvar(actionType));
			setUseXpCommandOnceField(isXpCmd(actionType));
		}
		else if (settingsModel.UseLvarReset) {
			setToggleField(false);
			setHoldField(false);
			setResetField(true);
			toggleConfigItem(false, 'AddressActionOff');
			toggleConfigItem(false, 'AddressMonitor');
			setBvarRequestField(false);
			setUseXpCommandOnceField(false);
		}
		else {
			setToggleField(isToggleable(actionType, settingsModel.DoNotRequestBvar));
			setHoldField(isHoldable(actionType, settingsModel.DoNotRequestBvar));
			setResetField(isResetable(actionType, settingsModel.DoNotRequestBvar));
			toggleConfigItem(false, 'AddressActionOff');
			toggleConfigItem(false, 'AddressMonitor');
			setBvarRequestField(isBvar(actionType));
			setUseXpCommandOnceField(isXpCmd(actionType));
		}

		toggleConfigItem(hasCommandDelay(actionType, settingsModel.DoNotRequestBvar), 'UseControlDelay');
	}
	else if (deckType == "Long") {
		var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction, settingsModel.HoldSwitch);
		toggleConfigItem(longAllowed, 'HasLongPress');
		if (settingsModel.HasLongPress && longAllowed) {
			toggleConfigItem(true, 'ActionTypeLong');
			toggleConfigItem(true, 'AddressActionLong');
			if (isValueCommand(actionType, settingsModel.DoNotRequestBvar)) {
				setOnOffFields(deckType, true);
			}
			else {
				setOnOffFields(deckType, false);
			}
		}
		else {
			toggleConfigItem(false, 'ActionTypeLong');
			toggleConfigItem(false, 'AddressActionLong')
			setOnOffFields(deckType, false);
		}
	}
	else if (deckType == "Guard") {
		var isGuarded = settingsModel.IsGuarded
		toggleConfigItem(isGuarded, 'AddressGuardActive');
		toggleConfigItem(isGuarded, 'GuardActiveValue');
		toggleConfigItem(isGuarded, 'ActionTypeGuard');
		toggleConfigItem(isGuarded, 'AddressActionGuard');
		toggleConfigItem(isGuarded && settingsModel.HoldSwitch && isNonvalueCommand(actionType, settingsModel.DoNotRequestBvar), 'AddressActionGuardOff');
		setOnOffFields(deckType, isGuarded && isValueCommand(actionType, settingsModel.DoNotRequestBvar));
		toggleConfigItem(isGuarded, 'GuardRect');
		setFormItem(isGuarded, 'HeadingMon');
		setFormItem(isGuarded, 'HeadingCmd');
		setFormItem(isGuarded, 'HeadingImg');
	}
}

function toggleImageMapping(useMap) {
	toggleOnControlsMap.forEach((id) => toggleConfigItem(useMap, id));
	toggleOffControlsMap.forEach((id) => toggleConfigItem(!useMap, id));
	toggleOnDivMap.forEach((id) => setFormItem(useMap, id));
	toggleOffDivMap.forEach((id) => setFormItem(!useMap, id));
}

function commonFormUpdate() {
	if (settingsModel.HasAction) {
		checkButtonSettings(settingsModel);
	}
	//ENCODER ACTIONS
	if (settingsModel.IsEncoder && settingsModel.HasAction) {
		if (document.getElementById("EncoderActions"))
			document.getElementById("EncoderActions").style.display = "block";
	}
	else {
		if (document.getElementById("EncoderActions") != null)
			document.getElementById("EncoderActions").style.display = "none";
	}

	//DEFAULT ACTIONS
	if (document.getElementById("DefaultActions") && settingsModel.HasAction) {
		document.getElementById("DefaultActions").style.display = "block";
		if (document.getElementById("GuardActions"))
			document.getElementById("GuardActions").style.display = "block";

		toggleConfigItem(true, 'IsGuarded');
		
		//PATTERNS
		setPattern('Address', 5);			
		setPattern('AddressMonitor', 5);
		setPattern('AddressAction', settingsModel.ActionType, settingsModel.DoNotRequestBvar);
		setPattern('AddressActionOff', settingsModel.ActionType, settingsModel.DoNotRequestBvar);
		setPattern('AddressActionLong', settingsModel.ActionTypeLong, settingsModel.DoNotRequestBvar);
		if (settingsModel.IsEncoder) {
			setPattern('AddressActionLeft', settingsModel.ActionTypeLeft, settingsModel.DoNotRequestBvar);
			setPattern('AddressActionRight', settingsModel.ActionTypeRight, settingsModel.DoNotRequestBvar);
			setPattern('AddressActionTouch', settingsModel.ActionTypeTouch, settingsModel.DoNotRequestBvar);
		}

		if (settingsModel.IsGuarded) {
			setPattern('AddressGuardActive', 5);
			setPattern('AddressActionGuard', settingsModel.ActionTypeGuard, settingsModel.DoNotRequestBvar);
			setPattern('AddressActionGuardOff', settingsModel.ActionTypeGuard, settingsModel.DoNotRequestBvar);
		}
		
		//ACTION FIELDS
		setActionFields('', settingsModel.ActionType, settingsModel);
		setActionFields('Long', settingsModel.ActionTypeLong, settingsModel);
		setActionFields('Guard', settingsModel.ActionTypeGuard, settingsModel);

		if (settingsModel.IsEncoder) {
			setActionFields('Left', settingsModel.ActionTypeLeft, settingsModel);
			setActionFields('Right', settingsModel.ActionTypeRight, settingsModel);
			setActionFields('Touch', settingsModel.ActionTypeTouch, settingsModel);
		}
	}
	else {
		if (document.getElementById("DefaultActions"))
			document.getElementById("DefaultActions").style.display = "none";
		if (document.getElementById("GuardActions"))
			document.getElementById("GuardActions").style.display = "none";

		toggleConfigItem(false, 'IsGuarded');
		setPattern('Address', 5);
	}

	//Image Mapping
	toggleImageMapping(settingsModel.UseImageMapping);

	toggleConfigItem(settingsModel.IsGuarded, 'UseImageGuardMapping');
	toggleConfigItem(settingsModel.UseImageGuardMapping && settingsModel.IsGuarded, 'ImageGuardMap');
	setFormItem(!settingsModel.UseImageGuardMapping && settingsModel.IsGuarded, 'DefaultGuardMapping');

	//PREVIEWS
	updateImagePreviews();

	//ACTION UPDATE
	updateForm();
}

function activateTabs(activeTab) {
	const allTabs = Array.from(document.querySelectorAll('.tab'));
	let activeTabEl = null;
	allTabs.forEach((el, i) => {
		el.onclick = () => clickTab(el);
		if (el.dataset?.target === activeTab) {
			activeTabEl = el;
		}
	});
	if (activeTabEl) {
		clickTab(activeTabEl);
	} else if (allTabs.length) {
		clickTab(allTabs[0]);
	}
}

function clickTab(clickedTab) {
	const allTabs = Array.from(document.querySelectorAll('.tab'));
	allTabs.forEach((el, i) => el.classList.remove('selected'));
	clickedTab.classList.add('selected');
	activeTab = clickedTab.dataset?.target;
	allTabs.forEach((el, i) => {
		if (el.dataset.target) {
			const t = document.querySelector(el.dataset.target);
			if (t) {
				t.style.display = el == clickedTab ? 'block' : 'none';
			}
		}
	});
}

function clickCopy() {
	sendToPlugin("SettingsModelCopy");
}

function clickPaste() {
	sendToPlugin("SettingsModelPaste");
}

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo, inActionInfo) {
	uuid = inUUID;
	actionInfo = JSON.parse(inActionInfo);
	inInfo = JSON.parse(inInfo);
	websocket = new WebSocket('ws://localhost:' + inPort);

	if (actionInfo.payload.settings.settingsModel) {
		refreshSettings(actionInfo.payload.settings.settingsModel);
		activateTabs();
		commonFormUpdate();
	}	
		
	websocket.onopen = function () {
		var json = { event: inRegisterEvent, uuid: inUUID };
		websocket.send(JSON.stringify(json));

		sendToPlugin("propertyInspectorConnected");
	};

	websocket.onmessage = function (evt) {
		var jsonObj = JSON.parse(evt.data);
		var sdEvent = jsonObj['event'];

		switch (sdEvent) {
			case "sendToPropertyInspector":
				if (jsonObj?.payload?.IsPropertyInspectorModel == true) {
					fillActionSelectBoxes(jsonObj.payload.ActionTypes);
					fillTypeSelectBox(jsonObj.payload.FontNames, 'FontName', settingsModel.FontName);
					fillTypeSelectBox(jsonObj.payload.FontStyles, 'FontStyle', settingsModel.FontStyle);
					fillTypeSelectBox(jsonObj.payload.GaugeOrientations, 'BarOrientation', settingsModel.BarOrientation);
					ImageDictionary = jsonObj.payload.ImageDictionary;
					fillImageSelectBoxes();
				}

				if (jsonObj?.payload?.IsProfileSwitcherModel == true) {
					updateProfileSwitcher(jsonObj.payload);
				}
				break;
			case "didReceiveSettings":
				refreshSettings(jsonObj.payload.settings.settingsModel);
				commonFormUpdate();
				break;
			default:
				break;
		}
	};
}

const sendToPlugin = (payload) => {
	if (websocket && websocket.readyState == 1) {
		var json = {
			"event": "sendToPlugin",
			"action": actionInfo.action,
			"context": uuid,
			"payload": {
				"settings": payload
			}
		};
		websocket.send(JSON.stringify(json));
	}
}

const setSettings = (value, param) => {
	if (websocket) {
		settingsModel[param] = value;
		var json = {
			"event": "setSettings",
			"context": uuid,
			"payload": {
				"settingsModel": settingsModel
			}
		};
		websocket.send(JSON.stringify(json));
	}
	commonFormUpdate();
};
