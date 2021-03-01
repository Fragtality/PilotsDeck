// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/SwitchDefault.png",
	ErrorImage: "Images/Error.png",
	Address: "",
    HasIndication: false,
    IndicationImage: "Images/Fault.png",
    IndicationValue: "0",
	AddressAction: "",
	ActionType: 0,
	HasLongPress: false,
	AddressActionLong: "",
	ActionTypeLong: 0,
	OnStateLong: "",
	OffStateLong: "",
	OnImage: "Images/KorryOnBlueTop.png",
	OnState: "",
	OffImage: "Images/KorryOffWhiteBottom.png",
	OffState: "",
	IndicationValueAny: false
  };

function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
		fillImageSelectBox(ImageFiles, 'IndicationImage', settingsModel.IndicationImage);
		fillImageSelectBox(ImageFiles, 'OnImage', settingsModel.OnImage);
		fillImageSelectBox(ImageFiles, 'OffImage', settingsModel.OffImage);
	}
	if (ActionTypes && ActionTypes != "") {
		fillTypeSelectBox(ActionTypes, 'ActionType', settingsModel.ActionType);
		fillTypeSelectBox(ActionTypes, 'ActionTypeLong', settingsModel.ActionTypeLong);
	}
}

function updateForm() {
	//ACTION TYPE pattern
	setPattern('AddressAction', settingsModel.ActionType);
	setPattern('AddressActionLong', settingsModel.ActionTypeLong);

	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationValueAny');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.IndicationValueAny, 'IndicationValue');

	//On/Off States
	var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
	if (settingsModel.HasLongPress && longAllowed)
		toggleOnOffState(settingsModel.ActionTypeLong, 'OnStateLong', 'OffStateLong');
	else
		toggleOnOffState(-1, 'OnStateLong', 'OffStateLong');

	//LongPress
	toggleConfigItem(longAllowed, 'HasLongPress');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'ActionTypeLong');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'AddressActionLong');
}