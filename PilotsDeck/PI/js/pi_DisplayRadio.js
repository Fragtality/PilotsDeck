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
	HoldSwitch: false,
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
	HasIndication: true,
	IndicationImage: "Images/ArrowBright.png",
    FontInherit: true,
	FontName: "Arial",
	FontSize: "10",
	FontColor: '#ffffff',
	FontColorStby: '#e0e0e0',
	RectCoord: "3; 1; 64; 32",
	RectCoordStby: "3; 42; 64; 31"
};

var imageSelectBoxes = ["DefaultImage", "ErrorImage", "IndicationImage"];

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('AddressRadioActiv', 5);
	setPattern('AddressRadioStandby', 5);

	//CURRENT VALUE
	settingsModel.SwitchOnCurrentValue = false;
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');

	//FONT
	toggleConfigItem(!settingsModel.FontInherit, 'FontName');
	toggleConfigItem(!settingsModel.FontInherit, 'FontSize');
	toggleConfigItem(!settingsModel.FontInherit, 'FontColor');
	toggleConfigItem(false, 'FontStyle');
	toggleConfigItem(!settingsModel.FontInherit, 'FontColorStby');

	//FORMAT
	toggleConfigItem(false, 'ValueMappings');
	toggleConfigItem(settingsModel.StbyHasDiffFormat, 'DecodeBCDStby');
	toggleConfigItem(settingsModel.StbyHasDiffFormat, 'ScalarStby');
	toggleConfigItem(settingsModel.StbyHasDiffFormat, 'FormatStby');
}
