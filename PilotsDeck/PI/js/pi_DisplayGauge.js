// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Empty.png",
	ErrorImage: "Images/Error.png",
	Address: "",
	IsEncoder: false,
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
	UseControlDelay: false,
	UseLvarReset: false,
	SwitchOnState: "",
	SwitchOffState: "",
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
	IndicatorFlip: false,
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
	RectCoord: "7; 45; 60; 21"
};

var imageSelectBoxes = ["DefaultImage", "ErrorImage"];
var firstLoad = true;
var lastDrawArc = false;

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('AddressColorOff', 5);
	
	//CURRENT VALUE
	settingsModel.SwitchOnCurrentValue = false;
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');

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

	//LINE
	toggleConfigItem(settingsModel.CenterLine, 'CenterLineColor');
	toggleConfigItem(settingsModel.CenterLine, 'CenterLineThickness');

	//RANGE
	toggleConfigItem(settingsModel.DrawWarnRange, 'SymmRange');
	toggleConfigItem(settingsModel.DrawWarnRange, 'CriticalColor');
	toggleConfigItem(settingsModel.DrawWarnRange, 'CriticalRange');
	toggleConfigItem(settingsModel.DrawWarnRange, 'WarnColor');
	toggleConfigItem(settingsModel.DrawWarnRange, 'WarnRange');

	//FONT
	toggleConfigItem(settingsModel.ShowText, 'Format');
	toggleConfigItem(settingsModel.ShowText, 'ValueMappings');
	toggleConfigItem(settingsModel.ShowText && settingsModel.DrawWarnRange, 'UseWarnColors');
	toggleConfigItem(settingsModel.ShowText, 'FontInherit');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontName');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontSize');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontStyle');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontColor');
	toggleConfigItem(settingsModel.ShowText, 'RectCoord');
}
