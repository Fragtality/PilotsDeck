// global websocket, used to communicate from/to Stream Deck software
// as well as some info about our plugin, as sent by Stream Deck software

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

var websocket = null,
	uuid = null,
	inInfo = null,
	actionInfo = {},
	ImageFiles = "",
	ActionTypes = "",
	KorryFiles = "",
	GaugeOrientations = "",
	FontNames = "",
	FontStyles = "",
	displayInfo = {};

function fillImageSelectBox(values, elementID, configured) {
	if (!document.getElementById(elementID))
		return;

	values = values.split('|');
	for (i = 0; i < values.length; i++) {
		var option = document.createElement("option");
		var idxFilename = values[i].split('/').length - 1;
		option.text = values[i].split('/')[idxFilename].split('.')[0];
		option.value = values[i];
		if (values[i] == configured)
			option.selected = true;
		document.getElementById(elementID).add(option);
	}
}

function fillImagePreview(elementID) {
	if (document.getElementById(elementID) && document.getElementById("Prev_" + elementID)) {
		var img = document.getElementById(elementID).value;
		document.getElementById("Prev_" + elementID).src = "../" + img;
		var alt = img.replace(".png", "");
		alt = alt.substring(alt.lastIndexOf("/") + 1);
		document.getElementById("Prev_" + elementID).alt = alt;
	}
}

function fillFontSelectBox(values, elementID, configured) {
	if (!document.getElementById(elementID))
		return;

	values = values.split('|');
	for (i = 0; i < values.length; i++) {
		var option = document.createElement("option");
		option.text = values[i];
		option.value = values[i];
		if (values[i] == configured)
			option.selected = true;
		document.getElementById(elementID).add(option);
	}
}

function fillTypeSelectBox(values, elementID, configured) {
	if (!document.getElementById(elementID))
		return;

	if (values || values != "") {
		values = values.split('|');
		for (i = 0; i < values.length; i++) {
			var option = document.createElement("option");
			var type = values[i].split('=');
			option.text = type[1];
			option.value = type[0];
			if (type[0] == configured)
				option.selected = true;
			document.getElementById(elementID).add(option);
		}
	}
}

function fillActionSelectBoxes() {
	if (ActionTypes && ActionTypes != "") {
		fillTypeSelectBox(ActionTypes, 'ActionType', settingsModel.ActionType);
		fillTypeSelectBox(ActionTypes, 'ActionTypeLong', settingsModel.ActionTypeLong);
		fillTypeSelectBox(ActionTypes, 'ActionTypeGuard', settingsModel.ActionTypeGuard);
		if (settingsModel.IsEncoder) {
			fillTypeSelectBox(ActionTypes, 'ActionTypeLeft', settingsModel.ActionTypeLeft);
			fillTypeSelectBox(ActionTypes, 'ActionTypeRight', settingsModel.ActionTypeRight);
			fillTypeSelectBox(ActionTypes, 'ActionTypeTouch', settingsModel.ActionTypeTouch);
		}
	}
}

function refreshSettings(settings) {
	if (settings) {
		for (var key in settings) {
			if (settingsModel.hasOwnProperty(key)) {
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
}

function toggleConfigItem(value, name) {
	var block = "Config_" + name;
	var label = "lbl" + name;

	if (!document.getElementById(name))
		return;

	if (value) {
		document.getElementById(block).style.display = displayInfo[block];
		document.getElementById(label).style.display = displayInfo[label];
		document.getElementById(name).style.display = displayInfo[name];
	}
	else if (document.getElementById(block).style.display != "none") {
		displayInfo[block] = document.getElementById(block).style.display;
		displayInfo[label] = document.getElementById(label).style.display;
		displayInfo[name] = document.getElementById(name).style.display;

		document.getElementById(block).style.display = "none";
		document.getElementById(label).style.display = "none";
		document.getElementById(name).style.display = "none";
	}
}

function setFormItem(value, name) {
	if (!document.getElementById(name))
		return;

	if (value) {
		document.getElementById(name).style.display = displayInfo[name];
	}
	else if (document.getElementById(name).style.display != "none") {
		displayInfo[name] = document.getElementById(name).style.display;

		document.getElementById(name).style.display = "none";
	}
}

function setPattern(field, type) {
	if (!document.getElementById(field))
		return;

	var regName = "[^:\\s][a-zA-Z0-9\x2D\x5F]+";
	var regNameXP = "[^:\\s][a-zA-Z0-9\x2D\x5F\x2B]+";
	var regNameMultiple = "[a-zA-Z0-9\x2D\x5F]+";
	var regNameMultipleXP = "[a-zA-Z0-9\x2D\x5F\x2B]+";
	//var regLVarName = "[^:\\s][a-zA-Z0-9\x2D\x5F\x2E\x3A]+";
	var regLVarName = "[^:\\s][a-zA-Z0-9\x2D\x5F\x2E\x20]+([\x3A][0-9]+){0,1}";
	var regLvar = `^((L:){0,1}${regLVarName}){1}$`;
	var strHvar = `((H:){0,1}${regName}){1}`;
	var regHvar = `^(${strHvar}){1}(:${strHvar})*$`;
	var regDref = `^(${regNameXP}[\x2F]){1}(${regNameMultipleXP}[\x2F])*(${regNameMultipleXP}(([\x5B][0-9]+[^\x2F0-9a-zA-Z])|(:s[0-9]+)){0,1}){1}$`;
	var strPathXP = `(${regNameXP}[\x2F]){1}(${regNameMultipleXP}[\x2F])*(${regNameMultipleXP}){1}`;
	var regCmdXP = `^(${strPathXP}){1}(:${strPathXP})*$`;
	var regOffset = "^((0x){0,1}[0-9A-Fa-f]{4}:[0-9]{1,3}((:[ifs]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$";
	var regAvar = `^\\((A:){0,1}[\\w][\\w ]+(:\\d+){0,1},\\s{0,1}[\\w][\\w ]+\\)$`;
	var regBvar = `^(B:${regLVarName}){1}$`;
	var regLuaFunc = `^(Lua|lua){1}:${regName}(\.lua){0,1}:${regName}([\(]{1}[^\)]+[\)]{1}){0,1}`;
	var regInternal = `^X:${regName}$`;
	var regCalcRead = `^C:[^\s].+$`
	
	if (type == 0) //macro
		document.getElementById(field).pattern = `^([^0-9]{1}${regName}(:${regName}){1,}){1}$`;
	else if (type == 1) //script
		document.getElementById(field).pattern = `^Lua(Set|Clear|Toggle|Value)?:${regName}(:[0-9]{1,4})*$`;
	else if (type == 2) //control
		document.getElementById(field).pattern = "^([0-9]+)$|^(([0-9]+\=[0-9]+(:[0-9]+)*){1}(:([0-9]+\=[0-9]+(:[0-9]+)*){1})*)$|^[0-9]+(:[0-9]+)*$";    //"^[0-9]+(:[0-9]+)*$";
	else if (type == 3)  //lvar
		document.getElementById(field).pattern = regLvar;
	else if (type == 4)  //offset
		document.getElementById(field).pattern = regOffset;
	else if (type == 5) //offset | lvar | dref | avar | bvar | luafunc | internal | calcRead
		document.getElementById(field).pattern = `${regOffset}|${regDref}|${regAvar}|${regBvar}|${regInternal}|${regLuaFunc}|${regCalcRead}|${regLvar}`;
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
	else if (type == 13) //Bvar
		document.getElementById(field).pattern = regBvar;
	else if (type == 14) //LuaFunc
		document.getElementById(field).pattern = regLuaFunc;
	else if (type == 15) //Internal
		document.getElementById(field).pattern = regInternal;
	else
		document.getElementById(field).pattern = ".*";
}

function isLongPressAllowed(actionType, address, settingsModel) {
	return (actionType != 6 || (actionType == 6 && address.includes(":t")))
		&& (actionType != 7 || (actionType == 7 && address.includes(":t")))
		&& (!settingsModel.HoldSwitch);
}

function isActionTypeSelected(actionType, settingsModel) {
	if (settingsModel.ActionType == actionType)
		return true;
	else if (settingsModel.HasLongPress && settingsModel.ActionTypeLong == actionType)
		return true;
	else if (settingsModel.IsEncoder) {
		if (settingsModel.ActionTypeLeft == actionType)
			return true;
		else if (settingsModel.ActionTypeRight == actionType)
			return true;
		else if (settingsModel.ActionTypeTouch == actionType)
			return true;
	}
	else
		return false;
}

function toggleControlDelay(settingsModel) {
	var delayField = "UseControlDelay";

	if (isActionTypeSelected(2, settingsModel))	//control - optional/last state
		toggleConfigItem(true, delayField);
	else if (isActionTypeSelected(10, settingsModel) || isActionTypeSelected(8, settingsModel)) { //xpcmd & hvar - default true
		toggleConfigItem(true, delayField);
	}
	else {
		toggleConfigItem(false, delayField);
		settingsModel.UseControlDelay = false;
		document.getElementById(delayField).checked = false;
	}

	return settingsModel;
}

function toggleSwitchToggle(settingsModel) {
	var currentValueField = "SwitchOnCurrentValue";
	var monitorField = "AddressMonitor";
	var alternateField = "AddressActionOff";

	if (settingsModel.ToggleSwitch && isActionToggleable(settingsModel.ActionType)) {
		toggleConfigItem(true, alternateField);
		toggleConfigItem(true, monitorField);
		toggleConfigItem(false, currentValueField);
		settingsModel.SwitchOnCurrentValue = false;
		document.getElementById(currentValueField).checked = false;
	}	

	return settingsModel;
}

function toggleSwitchHold(settingsModel) {
	var currentValueField = "SwitchOnCurrentValue";
	var alternateField = "AddressActionOff";

	if (isActionHoldable(settingsModel.ActionType) && settingsModel.HoldSwitch) { //everything but vJoys
		toggleConfigItem(!isActionHoldableValue(settingsModel.ActionType), alternateField)
		toggleConfigItem(false, currentValueField);
		settingsModel.SwitchOnCurrentValue = false;
		document.getElementById(currentValueField).checked = false;
	}

	return settingsModel;
}

function isActionToggleable(actionType) {
	return (actionType >= 0 && actionType <= 2) || (actionType >= 8 && actionType <= 10) || actionType == 13 || actionType == 14;
}

function isActionHoldable(actionType) {
	return actionType != 6 && actionType != 7;
}

function isActionHoldableValue(actionType) {
	return actionType != 6 && actionType != 7 && isActionResetable(actionType);
}

function isActionResetable(actionType) {
	return (actionType >= 3 && actionType <= 5) || (actionType >= 11 && actionType <= 13) || actionType == 15;
}

function setSwitchOptions(settingsModel) {
	var toggleField = "ToggleSwitch";
	var holdField = "HoldSwitch";
	var resetField = "UseLvarReset";
	var actionType = settingsModel.ActionType;

	if (settingsModel.ToggleSwitch && isActionToggleable(actionType)) {
		settingsModel.HoldSwitch = false;
		document.getElementById(holdField).checked = false;
		toggleConfigItem(false, holdField);

		settingsModel.UseLvarReset = false;
		document.getElementById(resetField).checked = false;
		toggleConfigItem(false, resetField);
	}
	else if (settingsModel.HoldSwitch && isActionHoldable(actionType)) {
		settingsModel.ToggleSwitch = false;
		document.getElementById(toggleField).checked = false;
		toggleConfigItem(false, toggleField);

		settingsModel.UseLvarReset = false;
		document.getElementById(resetField).checked = false;
		toggleConfigItem(false, resetField);
	}
	else if (settingsModel.UseLvarReset && isActionResetable(actionType)) {
		settingsModel.ToggleSwitch = false;
		document.getElementById(toggleField).checked = false;
		toggleConfigItem(false, toggleField);

		settingsModel.HoldSwitch = false;
		document.getElementById(holdField).checked = false;
		toggleConfigItem(false, holdField);
	}
	else {
		settingsModel.ToggleSwitch = false;
		document.getElementById(toggleField).checked = false;
		toggleConfigItem(isActionToggleable(actionType), toggleField);

		settingsModel.HoldSwitch = false;
		document.getElementById(holdField).checked = false;
		toggleConfigItem(isActionHoldable(actionType), holdField);

		settingsModel.UseLvarReset = false;
		document.getElementById(resetField).checked = false;
		toggleConfigItem(isActionResetable(actionType), resetField);
	}

	toggleConfigItem(false, "AddressActionOff");
	toggleConfigItem(false, "UseControlDelay");
	toggleConfigItem(false, "SwitchOnCurrentValue");
	toggleConfigItem(false, "AddressMonitor");
	toggleConfigItem(false, "SwitchOnState");
	toggleConfigItem(false, "SwitchOffState");	

	return settingsModel;
}

function toggleOnOffState(actionType, onField, offField, switchCurrent, toggleSwitch = false) {
	//On/Off States
	if (actionType == 3 && !switchCurrent) { //lvar
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else if (actionType == 4 && !switchCurrent) { //offset
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else if (actionType == 11 && !switchCurrent) { //xp write ref
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else if (actionType == 12 && !switchCurrent) { //avar
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else if (actionType == 13 && !switchCurrent) { //bvar
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else if (actionType == 15 && !switchCurrent) { //internal
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else if (isActionToggleable(actionType) && toggleSwitch) { //control/command & toggle
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else {
		toggleConfigItem(false, onField);
		toggleConfigItem(false, offField);
	}
}

function fillSelectBoxes() {
	//Images
	if (ImageFiles && ImageFiles != "") {
		imageSelectBoxes.forEach((id) =>
			fillImageSelectBox(ImageFiles, id, settingsModel[id])
		);
	}
	if (KorryFiles && KorryFiles != "" && settingsModel.TopImage != null && settingsModel.BotImage != null) {
		korrySelectBoxes.forEach((id) =>
			fillImageSelectBox(KorryFiles, id, settingsModel[id])
		);
	}
	updateImagePreviews();

	//Fonts
	if (FontNames && FontNames != "" && settingsModel.FontName != null) {
		fillFontSelectBox(FontNames, 'FontName', settingsModel.FontName);
	}
	if (FontStyles && FontStyles != "" && settingsModel.FontStyle != null) {
		fillTypeSelectBox(FontStyles, 'FontStyle', settingsModel.FontStyle);
	}

	//Gauge
	if (GaugeOrientations && GaugeOrientations != "" && settingsModel.BarOrientation != null) {
		fillTypeSelectBox(GaugeOrientations, 'BarOrientation', settingsModel.BarOrientation);
	}
}

function updateImagePreviews() {
	if (ImageFiles && ImageFiles != "") {
		imageSelectBoxes.forEach((id) => fillImagePreview(id));
	}
	if (KorryFiles && KorryFiles != "" && settingsModel.TopImage != null && settingsModel.BotImage != null) {
		korrySelectBoxes.forEach((id) => fillImagePreview(id));
	}
}

function toggleImageMapping(useMap) {
	toggleOnControlsMap.forEach((id) => toggleConfigItem(useMap, id));
	toggleOffControlsMap.forEach((id) => toggleConfigItem(!useMap, id));
	toggleOnDivMap.forEach((id) => setFormItem(useMap, id));
	toggleOffDivMap.forEach((id) => setFormItem(!useMap, id));
}

function commonFormUpdate() {
	//ENCODER ACTIONS
	if (!settingsModel.IsEncoder) {
		if (document.getElementById("EncoderActions"))
			document.getElementById("EncoderActions").style.display = "none";
	}
	else {
		if ((settingsModel.GaugeSize != null && settingsModel.HasAction) || settingsModel.HasAction == null)
		document.getElementById("EncoderActions").style.display = "inline";
	}

	//DEFAULT ACTIONS
	var hasAction = (settingsModel.GaugeSize != null && settingsModel.HasAction) || settingsModel.HasAction == null
	if (document.getElementById("DefaultActions") && hasAction) {
		document.getElementById("DefaultActions").style.display = "inline";
		if (document.getElementById("GuardActions"))
			document.getElementById("GuardActions").style.display = "inline";

		toggleConfigItem(true, 'IsGuarded');

		//PATTERNS
		setPattern('Address', 5);
		setPattern('AddressMonitor', 5);
		setPattern('AddressAction', settingsModel.ActionType);
		setPattern('AddressActionOff', settingsModel.ActionType);
		setPattern('AddressActionLong', settingsModel.ActionTypeLong);

		if (settingsModel.IsEncoder) {
			setPattern('AddressActionLeft', settingsModel.ActionTypeLeft);
			setPattern('AddressActionRight', settingsModel.ActionTypeRight);
			setPattern('AddressActionTouch', settingsModel.ActionTypeTouch);

			toggleOnOffState(settingsModel.ActionTypeLeft, 'SwitchOnStateLeft', 'SwitchOffStateLeft', false);
			toggleOnOffState(settingsModel.ActionTypeRight, 'SwitchOnStateRight', 'SwitchOffStateRight', false);
			toggleOnOffState(settingsModel.ActionTypeTouch, 'SwitchOnStateTouch', 'SwitchOffStateTouch', false);
		}

		//OPTIONS / ALTERNATIVE
		settingsModel = setSwitchOptions(settingsModel);
		settingsModel = toggleSwitchHold(settingsModel);
		settingsModel = toggleSwitchToggle(settingsModel);
		settingsModel = toggleControlDelay(settingsModel);
		

		//LONG
		var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction, settingsModel);
		toggleOnOffState(settingsModel.ActionType, 'SwitchOnState', 'SwitchOffState', settingsModel.SwitchOnCurrentValue, settingsModel.ToggleSwitch);
		if (settingsModel.HasLongPress && longAllowed)
			toggleOnOffState(settingsModel.ActionTypeLong, 'SwitchOnStateLong', 'SwitchOffStateLong', false);
		else
			toggleOnOffState(-1, 'SwitchOnStateLong', 'SwitchOffStateLong');

		toggleConfigItem(longAllowed, 'HasLongPress');
		if (!longAllowed) {
			settingsModel.HasLongPress = false;
			document.getElementById('HasLongPress').checked = false;
		}
		toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'ActionTypeLong');
		toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'AddressActionLong');
	}
	else {
		if (document.getElementById("DefaultActions"))
			document.getElementById("DefaultActions").style.display = "none";
		if (document.getElementById("GuardActions"))
			document.getElementById("GuardActions").style.display = "none";

		toggleConfigItem(false, 'IsGuarded');
		setPattern('Address', 5);
	}

	//GUARDED
	var isGuarded = settingsModel.IsGuarded
	toggleConfigItem(isGuarded, 'AddressGuardActive');
	toggleConfigItem(isGuarded, 'GuardActiveValue');
	toggleConfigItem(isGuarded, 'ActionTypeGuard');
	toggleConfigItem(isGuarded, 'AddressActionGuard');
	toggleConfigItem(isGuarded, 'AddressActionGuardOff');
	toggleConfigItem(isGuarded, 'SwitchOnStateGuard');
	toggleConfigItem(isGuarded, 'SwitchOffStateGuard');
	toggleConfigItem(isGuarded, 'ImageGuard'); 
	toggleConfigItem(isGuarded, 'GuardRect');


	if (isGuarded) {
		setPattern('AddressGuardActive', 5);
		setPattern('AddressActionGuard', settingsModel.ActionTypeGuard);
		toggleOnOffState(settingsModel.ActionTypeGuard, 'SwitchOnStateGuard', 'SwitchOffStateGuard', settingsModel.ToggleSwitch);
	
		if (isActionHoldable(settingsModel.ActionTypeGuard) && settingsModel.HoldSwitch) {
			toggleConfigItem(!isActionHoldableValue(settingsModel.ActionTypeGuard), 'AddressActionGuardOff')
			toggleConfigItem(isActionHoldableValue(settingsModel.ActionTypeGuard), 'SwitchOnStateGuard');
			toggleConfigItem(isActionHoldableValue(settingsModel.ActionTypeGuard), 'SwitchOffStateGuard');
		}
		else if (settingsModel.ToggleSwitch && isActionToggleable(settingsModel.ActionTypeGuard)) {
			toggleConfigItem(true, 'AddressActionGuardOff');
			toggleOnOffState(settingsModel.ActionTypeGuard, 'SwitchOnStateGuard', 'SwitchOffStateGuard', false)
		}
		else {
			toggleConfigItem(false, 'AddressActionGuardOff')
		}
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

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo, inActionInfo) {
	uuid = inUUID;
	actionInfo = JSON.parse(inActionInfo);
	inInfo = JSON.parse(inInfo);
	websocket = new WebSocket('ws://localhost:' + inPort);

	if (actionInfo.payload.settings.settingsModel)
		refreshSettings(actionInfo.payload.settings.settingsModel);
	else
		refreshSettings(settingsModel);

	commonFormUpdate();
		
	websocket.onopen = function () {
		var json = { event: inRegisterEvent, uuid: inUUID };
		// register property inspector to Stream Deck
		websocket.send(JSON.stringify(json));

		sendToPlugin("propertyInspectorConnected");
	};

	websocket.onmessage = function (evt) {
		// Received message from Stream Deck
		var jsonObj = JSON.parse(evt.data);
		var sdEvent = jsonObj['event'];
		switch (sdEvent) {
			case "sendToPropertyInspector":
				if (jsonObj.payload && jsonObj.payload.ActionTypes && jsonObj.payload.ActionTypes != "") {
					if (!ActionTypes || ActionTypes == "") {
						ActionTypes = jsonObj.payload.ActionTypes;
					}
					else {
						ActionTypes = jsonObj.payload.ActionTypes;
					}
				}
				if (jsonObj.payload && jsonObj.payload.ImageFiles && jsonObj.payload.ImageFiles != "") {
					if (!ImageFiles || ImageFiles == "") {
						ImageFiles = jsonObj.payload.ImageFiles;
					}
					else {
						ImageFiles = jsonObj.payload.ImageFiles;
					}
				}
				if (jsonObj.payload && jsonObj.payload.KorryFiles && jsonObj.payload.KorryFiles != "") {
					if (!KorryFiles || KorryFiles == "") {
						KorryFiles = jsonObj.payload.KorryFiles;
					}
					else {
						KorryFiles = jsonObj.payload.KorryFiles;
					}
				}
				if (jsonObj.payload && jsonObj.payload.FontNames && jsonObj.payload.FontNames != "") {
					if (!FontNames || FontNames == "") {
						FontNames = jsonObj.payload.FontNames;
					}
					else {
						FontNames = jsonObj.payload.FontNames;
					}
				}
				if (jsonObj.payload && jsonObj.payload.FontStyles && jsonObj.payload.FontStyles != "") {
					if (!FontStyles || FontStyles == "") {
						FontStyles = jsonObj.payload.FontStyles;
					}
					else {
						FontStyles = jsonObj.payload.FontStyles;
					}
				}
				if (jsonObj.payload && jsonObj.payload.GaugeOrientations && jsonObj.payload.GaugeOrientations != "") {
					if (!GaugeOrientations || GaugeOrientations == "") {
						GaugeOrientations = jsonObj.payload.GaugeOrientations;
					}
					else {
						GaugeOrientations = jsonObj.payload.GaugeOrientations;
					}
				}
				if (jsonObj.payload && settingsModel) {
					var refresh = settingsModel.IsEncoder != jsonObj.payload.IsEncoder;
					settingsModel.IsEncoder = jsonObj.payload.IsEncoder;
					if (refresh)
						commonFormUpdate();
				}

				fillSelectBoxes();
				fillActionSelectBoxes();

				if (jsonObj.payload && jsonObj.payload.MappingsJson != null) {
					refreshSettings(jsonObj.payload);
					commonFormUpdate();
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
