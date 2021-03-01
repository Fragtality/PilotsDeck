// global websocket, used to communicate from/to Stream Deck software
// as well as some info about our plugin, as sent by Stream Deck software 
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

function setPattern(field, type) {
	var regName = "[a-zA-Z0-9\x2D\x5F]+";
	var regLvar = `^([^0-9]{1}(L:){0,1}${regName}){1}(:(L:){0,1}${regName})*$`;
	var regOffset = "((0x){0,1}[0-9A-F]{4}:[0-9]{1,3}(:[ifs]{1}(:s)?)?){1}";
	
	if (type == 0) //macro
		document.getElementById(field).pattern = `^([^0-9]{1}${regName}(:${regName}){1,}){1}$`;
	else if (type == 1) //script
		document.getElementById(field).pattern = `^Lua(Set|Clear|Toggle)?:${regName}(:[0-9]+)?$`;
	else if (type == 2) //control
		document.getElementById(field).pattern = "^[0-9]+(:[0-9]+)*$";
	else if (type == 3)  //lvar
		document.getElementById(field).pattern = regLvar;
	else if (type == 4)  //offset
		document.getElementById(field).pattern = regOffset;
	else if (type == 5) //offset | lvar
		document.getElementById(field).pattern = `${regOffset}|${regLvar}`;
	else if (type == 6) //vjoy
		document.getElementById(field).pattern = "^(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$";
	else
		document.getElementById(field).pattern = "";
}

function isLongPressAllowed(actionType, address) {
	return actionType != 6 || (actionType == 6 && address.includes(":t"));
}

function toggleOnOffState(actionType, onField, offField) {
	//On/Off States
	if (actionType == 0) { //macro
		toggleConfigItem(false, onField);
		toggleConfigItem(false, offField);
	}
	else if (actionType == 1) { //script
		toggleConfigItem(false, onField);
		toggleConfigItem(false, offField);
	}
	else if (actionType == 2) { //control
		toggleConfigItem(false, onField);
		toggleConfigItem(false, offField);
	}
	else if (actionType == 3) { //lvar
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else if (actionType == 4) { //offset
		toggleConfigItem(true, onField);
		toggleConfigItem(true, offField);
	}
	else if (actionType == 6) { //vjoy
		toggleConfigItem(false, onField);
		toggleConfigItem(false, offField);
	}
	else {
		toggleConfigItem(false, onField);
		toggleConfigItem(false, offField);
	}
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

	updateForm();
		
	websocket.onopen = function () {
		var json = { event: inRegisterEvent, uuid: inUUID };
		// register property inspector to Stream Deck
		websocket.send(JSON.stringify(json));
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
				fillSelectBoxes();
				if (jsonObj.payload && jsonObj.payload.MappingsJson != null) {
					refreshSettings(jsonObj.payload);
					updateForm();
                }
				break;
			case "didReceiveSettings":
				refreshSettings(jsonObj.payload.settings.settingsModel);
				updateForm();
				break;
			default:
				break;
		}
	};
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
	updateForm();
};
