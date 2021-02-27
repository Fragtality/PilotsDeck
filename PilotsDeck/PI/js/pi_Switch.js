// Implement settingsModel for the Action
var settingsModel = {
		DefaultImage: "Images/Switch.png",
		ErrorImage: "Images/SwitchError.png",
		AddressAction: "",
		ActionType: 0,
		OnState: "",
		OffState: "",
		HasLongPress: false,
        AddressActionLong: "",
        ActionTypeLong: 0,
        OnStateLong: "",
        OffStateLong: ""
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

	//On/Off States
	var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
	toggleOnOffState(settingsModel.ActionType, 'OnState', 'OffState');
	if (settingsModel.HasLongPress && longAllowed)
		toggleOnOffState(settingsModel.ActionTypeLong, 'OnStateLong', 'OffStateLong');
	else
		toggleOnOffState(-1, 'OnStateLong', 'OffStateLong');
	
	//LongPress
	toggleConfigItem(longAllowed, 'HasLongPress');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'ActionTypeLong');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'AddressActionLong');

}
