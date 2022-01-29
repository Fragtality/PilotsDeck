// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Arrow.png",
	ErrorImage: "Images/Error.png",
	AddressRadioActiv: "",
	AddressRadioStandby: "",
	AddressAction: "",
	ActionType: 0,
	SwitchOnState: "",
	SwitchOffState: "",
	SwitchOnCurrentValue: false,
	UseControlDelay: false,
	HasLongPress: false,
	AddressActionLong: "",
	ActionTypeLong: 0,
	SwitchOnStateLong: "",
	SwitchOffStateLong: "",
	DecodeBCD: false,
	Scalar: "1",
	Format: "",
	StbyHasDiffFormat: false,
	DecodeBCDStby: false,
	ScalarStby: "1",
	FormatStby: "",
	IndicationImage: "Images/ArrowBright.png",
    FontInherit: true,
	FontName: "Arial",
	FontSize: "10",
	FontColor: '#ffffff',
	FontColorStby: '#e0e0e0',
	RectCoord: "3; 1; 64; 32",
	RectCoordStby: "3; 42; 64; 31"
};

// Fill Select Boxes for Actions here
function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
		fillImageSelectBox(ImageFiles, 'IndicationImage', settingsModel.IndicationImage);
	}
	if (FontNames && FontNames != "") {
		fillFontSelectBox(FontNames, 'FontName', settingsModel.FontName);
	}
	if (ActionTypes && ActionTypes != "") {
		fillTypeSelectBox(ActionTypes, 'ActionType', settingsModel.ActionType);
		fillTypeSelectBox(ActionTypes, 'ActionTypeLong', settingsModel.ActionTypeLong);
	}
}

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('AddressRadioActiv', 5);
	setPattern('AddressRadioStandby', 5);
	setPattern('AddressAction', settingsModel.ActionType);
	setPattern('AddressActionLong', settingsModel.ActionTypeLong);

	//On/Off States
	var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
	toggleOnOffState(settingsModel.ActionType, 'SwitchOnState', 'SwitchOffState', false);
	if (settingsModel.HasLongPress && longAllowed)
		toggleOnOffState(settingsModel.ActionTypeLong, 'SwitchOnStateLong', 'SwitchOffStateLong', false);
	else
		toggleOnOffState(-1, 'SwitchOnStateLong', 'SwitchOffStateLong');

	toggleControlDelay(settingsModel);


	//LongPress
	toggleConfigItem(longAllowed, 'HasLongPress');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'ActionTypeLong');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'AddressActionLong');

	//FONT
	toggleConfigItem(!settingsModel.FontInherit, 'FontName');
	toggleConfigItem(!settingsModel.FontInherit, 'FontSize');
	toggleConfigItem(!settingsModel.FontInherit, 'FontColor');
	toggleConfigItem(!settingsModel.FontInherit, 'FontColorStby');

	//FORMAT
	toggleConfigItem(settingsModel.StbyHasDiffFormat, 'DecodeBCDStby');
	toggleConfigItem(settingsModel.StbyHasDiffFormat, 'ScalarStby');
	toggleConfigItem(settingsModel.StbyHasDiffFormat, 'FormatStby');
}
