var settingsModel = {
	DefaultImage: "Images/Empty.png",
	ErrorImage: "Images/Error.png",
	IsEncoder: false,
	Address: "",
	Address2: "",
	DecodeBCD: false,
	Scalar: "1",
	Format: "",
	ValueMappings: "",
	HasAction: false,
	AddressAction: "",
	AddressMonitor: "",
	AddressActionOff: "",
	ActionType: 0,
	SwitchOnCurrentValue: false,
	ToggleSwitch: false,
	HoldSwitch: false,
	UseControlDelay: false,
	UseLvarReset: false,
	SwitchOnState: "",
	SwitchOffState: "",
	HasLongPress: false,
	AddressActionLong: "",
	ActionTypeLong: 0,
	SwitchOnStateLong: "",
	SwitchOffStateLong: "",
	MinimumValue: "0",
	MaximumValue: "100",
	GaugeSize: "58; 10",
	BarOrientation: 0,
	GaugeColor: "#006400",
	UseColorSwitching: false,
	AddressColorOff: "",
	StateColorOff: "",
	GaugeColorOff: "#636363",
	DrawArc: false,
	StartAngle: "135",
	SweepAngle: "180",
	Offset: "0; 0",
	IndicatorColor: "#c7c7c7",
	IndicatorSize: "10",
	IndicatorFlip: true,
	CenterLine: false,
	CenterLineColor: "#ffffff",
	CenterLineThickness: "2",
	DrawWarnRange: false,
	SymmRange: false,
	CriticalColor: "#8b0000",
	WarnColor: "#ff8c00",
	CriticalRange: "0; 10",
	WarnRange: "10; 20",
	ShowText: true,
	UseWarnColors: true,
	FontInherit: true,
	FontName: "Arial",
	FontSize: "10",
	FontStyle: 0,
	FontColor: "#ffffff",
	RectCoord: "7; 45; 60; 21",
	RectCoord2: "7; 6; 60; 21",
	IsGuarded: false,
	AddressGuardActive: "",
	GuardActiveValue: "",
	AddressActionGuard: "",
	AddressActionGuardOff: "",
	ActionTypeGuard: 0,
	SwitchOnStateGuard: "",
	SwitchOffStateGuard: "",
	ImageGuard: "Images/GuardCross.png",
	GuardRect: "0; 0; 72; 72",
	UseImageGuardMapping: false,
	ImageGuardMap: "",
	UseImageMapping: false,
	ImageMap: ""
};

var imageSelectBoxes = ["DefaultImage", "ErrorImage", "ImageGuard"];
var toggleOnControlsMap = ["ImageMap"];
var toggleOffControlsMap = ["DefaultImage", "ErrorImage"];
var toggleOnDivMap = [];
var toggleOffDivMap = []
var firstLoad = true;
var lastDrawArc = false;

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('Address2', 5);
	setPattern('AddressColorOff', 5);

	//CURRENT VALUE
	settingsModel.SwitchOnCurrentValue = false;
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');

	//FORMAT
	toggleConfigItem(false, 'Format');
	toggleConfigItem(false, 'ValueMappings');

	//COLOR
	toggleConfigItem(settingsModel.UseColorSwitching, 'AddressColorOff');
	toggleConfigItem(settingsModel.UseColorSwitching, 'GaugeColorOff');
	toggleConfigItem(settingsModel.UseColorSwitching, 'StateColorOff');

	//ARC<>BAR
	if (lastDrawArc != settingsModel.DrawArc && !firstLoad) {
		if (settingsModel.DrawArc) {
			settingsModel.RectCoord = "16; 27; 60; 21";
			document.getElementById('RectCoord').value = "16; 27; 60; 21";
			settingsModel.GaugeSize = "48; 6";
			document.getElementById('GaugeSize').value = "48; 6";
		}
		else {
			settingsModel.RectCoord = "7; 45; 60; 21";
			document.getElementById('RectCoord').value = "7; 45; 60; 21";
			settingsModel.GaugeSize = "58; 10";
			document.getElementById('GaugeSize').value = "58; 10";
		}
	}
	lastDrawArc = settingsModel.DrawArc;
	firstLoad = false;

	//ARC
	toggleConfigItem(settingsModel.DrawArc, 'StartAngle');
	toggleConfigItem(settingsModel.DrawArc, 'SweepAngle');
	toggleConfigItem(settingsModel.DrawArc, 'Offset');
	toggleConfigItem(!settingsModel.DrawArc, 'BarOrientation');
	toggleConfigItem(settingsModel.DrawArc, 'IndicatorFlip');

	//LINE
	toggleConfigItem(settingsModel.CenterLine, 'CenterLineColor');
	toggleConfigItem(settingsModel.CenterLine, 'CenterLineThickness');

	//RANGE
	toggleConfigItem(settingsModel.DrawWarnRange, 'SymmRange');
	toggleConfigItem(settingsModel.DrawWarnRange, 'CriticalColor');
	toggleConfigItem(settingsModel.DrawWarnRange, 'CriticalRange');
	toggleConfigItem(settingsModel.DrawWarnRange, 'WarnColor');

	//FONT
	toggleConfigItem(settingsModel.ShowText, 'Format');
	toggleConfigItem(settingsModel.ShowText, 'ValueMappings');
	toggleConfigItem(settingsModel.ShowText, 'UseWarnColors');
	toggleConfigItem(settingsModel.ShowText, 'FontInherit');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontName');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontSize');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontStyle');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontColor');
	toggleConfigItem(settingsModel.ShowText, 'RectCoord');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.DrawArc, 'RectCoord2');
}
