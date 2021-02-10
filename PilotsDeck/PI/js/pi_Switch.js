// Implement settingsModel for the Action
var settingsModel = {
		DefaultImage: "Images/Switch.png",
		ErrorImage: "Images/SwitchError.png",
		AddressAction: "",
		ActionType: 0,
		OnState: "",
		OffState: ""
  };

function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
	}
	if (ActionTypes || ActionTypes != "") {
		fillTypeSelectBox(ActionTypes, 'ActionType', settingsModel.ActionType);
	}
}

function updateForm() {
	//PATTERN
	setPattern('AddressAction', settingsModel.ActionType);

	//On/Off States
	if (settingsModel.ActionType == 0) { //macro
		toggleConfigItem(false, 'Config_OnState', 'lblOnState', 'OnState');
		toggleConfigItem(false, 'Config_OffState', 'lblOffState', 'OffState');
	}
	else if (settingsModel.ActionType == 1) { //script
		toggleConfigItem(false, 'Config_OnState', 'lblOnState', 'OnState');
		toggleConfigItem(false, 'Config_OffState', 'lblOffState', 'OffState');
	}
	else if (settingsModel.ActionType == 2) { //control
		toggleConfigItem(false, 'Config_OnState', 'lblOnState', 'OnState');
		toggleConfigItem(false, 'Config_OffState', 'lblOffState', 'OffState');
	}
	else if (settingsModel.ActionType == 3)  { //lvar
		toggleConfigItem(true, 'Config_OnState', 'lblOnState', 'OnState');
		toggleConfigItem(true, 'Config_OffState', 'lblOffState', 'OffState');
	}
	else if (settingsModel.ActionType == 4)  { //offset
		toggleConfigItem(true, 'Config_OnState', 'lblOnState', 'OnState');
		toggleConfigItem(true, 'Config_OffState', 'lblOffState', 'OffState');
	}
	else {
		toggleConfigItem(false, 'Config_OnState', 'lblOnState', 'OnState');
		toggleConfigItem(false, 'Config_OffState', 'lblOffState', 'OffState');
	}

}
