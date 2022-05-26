// Implement settingsModel for the Action
var settingsModel = {
		DefaultImage: "Images/Switch.png",
		ErrorImage: "Images/SwitchError.png",
		AddressAction: "",
		ActionType: 0,
		SwitchOnState: "",
		SwitchOffState: "",
		UseControlDelay: false,
		UseLvarReset: false,
		SwitchOnCurrentValue: false,
		HasLongPress: false,
        AddressActionLong: "",
        ActionTypeLong: 0,
        SwitchOnStateLong: "",
        SwitchOffStateLong: ""
  };

function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
	}
	if (ActionTypes && ActionTypes != "") {
		fillTypeSelectBox(ActionTypes, 'ActionType', settingsModel.ActionType);
		fillTypeSelectBox(ActionTypes, 'ActionTypeLong', settingsModel.ActionTypeLong);
	}
}

function updateForm() {
	//PATTERN
	setPattern('AddressAction', settingsModel.ActionType);
	setPattern('AddressActionLong', settingsModel.ActionTypeLong);

	//On/Off States & SwitchOnCurrent
	var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
	toggleOnOffState(settingsModel.ActionType, 'SwitchOnState', 'SwitchOffState', settingsModel.SwitchOnCurrentValue);
	if (settingsModel.HasLongPress && longAllowed)
		toggleOnOffState(settingsModel.ActionTypeLong, 'SwitchOnStateLong', 'SwitchOffStateLong', settingsModel.SwitchOnCurrentValue);
	else
		toggleOnOffState(-1, 'SwitchOnStateLong', 'SwitchOffStateLong');
	toggleConfigItem(false, 'SwitchOnCurrentValue');

	toggleControlDelay(settingsModel);
	toggleLvarReset(settingsModel);

	
	//LongPress
	toggleConfigItem(longAllowed, 'HasLongPress');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'ActionTypeLong');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'AddressActionLong');

}
