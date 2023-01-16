// global websocket, used to communicate from/to Stream Deck software
// as well as some info about our plugin, as sent by Stream Deck software

if (document.getElementById("DefaultActions") && defaultHtml)
	document.getElementById("DefaultActions").innerHTML = defaultHtml;
if (document.getElementById("EncoderActions") && encoderHtml)
	document.getElementById("EncoderActions").innerHTML = encoderHtml;


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

	var regName = "[a-zA-Z0-9\x2D\x5F]+";
	var regLvar = `^([^0-9]{1}(L:){0,1}${regName}){1}$`;
	var strHvar = `((H:){0,1}${regName}){1}`;
	var regHvar = `^(${strHvar}){1}(:${strHvar})*$`;
	var regDref = `^(${regName}[\x2F]){1}(${regName}[\x2F])*(${regName}(([\x5B][0-9]+[^\x2F0-9a-zA-Z])|(:s[0-9]+)){0,1}){1}$`;
	var strPathXP = `(${regName}[\x2F]){1}(${regName}[\x2F])*(${regName}){1}`;
	var regCmdXP = `^(${strPathXP}){1}(:${strPathXP})*$`;
	var regOffset = "^((0x){0,1}[0-9A-Fa-f]{4}:[0-9]{1,3}((:[ifs]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$";
	
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
	else if (type == 5) //offset | lvar
		document.getElementById(field).pattern = `${regOffset}|${regLvar}|${regDref}`;
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
	else
		document.getElementById(field).pattern = ".*";
}

function isLongPressAllowed(actionType, address) {
	return (actionType != 6 || (actionType == 6 && address.includes(":t"))) && (actionType != 7 || (actionType == 7 && address.includes(":t")));
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

	if (isActionTypeSelected(2, settingsModel))
		toggleConfigItem(true, delayField);
	else if (isActionTypeSelected(10, settingsModel)) {
		toggleConfigItem(true, delayField);
		document.getElementById(delayField).checked = true;
	}
	else {
		toggleConfigItem(false, delayField);
		document.getElementById(delayField).checked = false;
	}
}

function toggleLvarReset(settingsModel) {
	var resetField = "UseLvarReset";

	if (settingsModel.ActionType == 3 || (settingsModel.HasLongPress && settingsModel.ActionTypeLong == 3))
		toggleConfigItem(true, resetField);
	else {
		document.getElementById(resetField).checked = false;
		toggleConfigItem(false, resetField);
	}
}

function toggleSwitchToggle(settingsModel) {
	var toggleField = "ToggleSwitch";
	var currentValueField = "SwitchOnCurrentValue";
	var monitorField = "AddressMonitor";
	var actionType = settingsModel.ActionType;

	if (actionType != 2 && actionType != 10) {
		settingsModel.ToggleSwitch = false;
		document.getElementById(toggleField).checked = false;
		toggleConfigItem(false, toggleField);
		toggleConfigItem(false, monitorField);
		settingsModel.AddressActionOff = "";
		document.getElementById("AddressActionOff").value = "";
		if (actionType == 3 || actionType == 4 || actionType == 5)
			toggleConfigItem(true, currentValueField);
	}
	else if (actionType == 2 || actionType == 10) {
		toggleConfigItem(true, toggleField);
		toggleConfigItem(false, currentValueField);
		settingsModel.SwitchOnCurrentValue = false;
		document.getElementById(currentValueField).checked = false;
		if (settingsModel.ToggleSwitch)
			toggleConfigItem(true, monitorField);
		else
			toggleConfigItem(false, monitorField);
	}

	toggleConfigItem(settingsModel.ToggleSwitch, "AddressActionOff");
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
	else if ((actionType == 2 || actionType == 10) && toggleSwitch) { //control/command & toggle
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else {
		toggleConfigItem(false, onField);
		toggleConfigItem(false, offField);
	}
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
	if (document.getElementById("DefaultActions") && ((settingsModel.GaugeSize != null && settingsModel.HasAction) || settingsModel.HasAction == null)) {
		document.getElementById("DefaultActions").style.display = "inline";

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
		toggleSwitchToggle(settingsModel);
		toggleControlDelay(settingsModel);
		toggleLvarReset(settingsModel);

		//LONG
		var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
		toggleOnOffState(settingsModel.ActionType, 'SwitchOnState', 'SwitchOffState', settingsModel.SwitchOnCurrentValue, settingsModel.ToggleSwitch);
		if (settingsModel.HasLongPress && longAllowed)
			toggleOnOffState(settingsModel.ActionTypeLong, 'SwitchOnStateLong', 'SwitchOffStateLong', false);
		else
			toggleOnOffState(-1, 'SwitchOnStateLong', 'SwitchOffStateLong');

		toggleConfigItem(longAllowed, 'HasLongPress');
		toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'ActionTypeLong');
		toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'AddressActionLong');
	}
	else if (document.getElementById("DefaultActions")) {
		document.getElementById("DefaultActions").style.display = "none";
	}

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
