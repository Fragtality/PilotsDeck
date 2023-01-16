// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Arrow.png",
	ErrorImage: "Images/Error.png",
	IsEncoder: false,
	AddressRadioActiv: "",
	AddressRadioStandby: "",
	AddressAction: "",
	AddressMonitor: "",
	AddressActionOff: "",
	ActionType: 0,
	SwitchOnState: "",
	SwitchOffState: "",
	SwitchOnCurrentValue: false,
	ToggleSwitch: false,
	UseControlDelay: false,
	UseLvarReset: false,
	HasLongPress: false,
	AddressActionLong: "",
	ActionTypeLong: 0,
	SwitchOnStateLong: "",
	SwitchOffStateLong: "",
	AddressActionLeft: "",
	ActionTypeLeft: 0,
	SwitchOnStateLeft: "",
	SwitchOffStateLeft: "",
	AddressActionRight: "",
	ActionTypeRight: 0,
	SwitchOnStateRight: "",
	SwitchOffStateRight: "",
	AddressActionTouch: "",
	ActionTypeTouch: 0,
	SwitchOnStateTouch: "",
	SwitchOffStateTouch: "",
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
}

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('AddressRadioActiv', 5);
	setPattern('AddressRadioStandby', 5);

	//CURRENT VALUE
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');

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
