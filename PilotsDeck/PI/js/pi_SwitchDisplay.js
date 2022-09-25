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
	SwitchOnState: "",
	SwitchOffState: "",
	UseControlDelay: false,
	UseLvarReset: false,
	HasLongPress: false,
	AddressActionLong: "",
	ActionTypeLong: 0,
	SwitchOnStateLong: "",
	SwitchOffStateLong: "",
	OnImage: "Images/KorryOnBlueTop.png",
	OnState: "",
	OffImage: "Images/KorryOffWhiteBottom.png",
	OffState: "",
	SwitchOnCurrentValue: true,
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
	setPattern('Address', 5);
	setPattern('AddressAction', settingsModel.ActionType);
	setPattern('AddressActionLong', settingsModel.ActionTypeLong);

	//On/Off States & SwitchOnCurrent
	if (settingsModel.ActionType != 3 && settingsModel.ActionType != 4 && settingsModel.ActionType != 11)
		toggleConfigItem(false, 'SwitchOnCurrentValue');
	else
		toggleConfigItem(true, 'SwitchOnCurrentValue');

	var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
	toggleOnOffState(settingsModel.ActionType, 'SwitchOnState', 'SwitchOffState', settingsModel.SwitchOnCurrentValue);
	if (settingsModel.HasLongPress && longAllowed)
		toggleOnOffState(settingsModel.ActionTypeLong, 'SwitchOnStateLong', 'SwitchOffStateLong', false);
	else
		toggleOnOffState(-1, 'SwitchOnStateLong', 'SwitchOffStateLong');

	toggleControlDelay(settingsModel);
	toggleLvarReset(settingsModel);

	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationValueAny');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.IndicationValueAny, 'IndicationValue');

	//LongPress
	toggleConfigItem(longAllowed, 'HasLongPress');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'ActionTypeLong');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'AddressActionLong');
}