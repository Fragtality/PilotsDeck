var websocket = null,
	uuid = null,
	inInfo = null,
	actionInfo = {};

function clickCopy() {
	sendToPlugin("SettingsModelCopy");
}

function clickPaste() {
	sendToPlugin("SettingsModelPaste");
}

function clickDesigner() {
	sendToPlugin("OpenDesigner");
}

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo, inActionInfo) {
	uuid = inUUID;
	actionInfo = JSON.parse(inActionInfo);
	inInfo = JSON.parse(inInfo);
	websocket = new WebSocket('ws://localhost:' + inPort);

	websocket.onopen = function () {
		var json = { event: inRegisterEvent, uuid: inUUID };
		websocket.send(JSON.stringify(json));

		var element = document.getElementById("LabelUUID");
		if (element != null) {
			element.value = actionInfo?.context.toUpperCase();
		}
	};

	websocket.onmessage = function (evt) {
		var jsonObj = JSON.parse(evt.data);
		var sdEvent = jsonObj['event'];
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