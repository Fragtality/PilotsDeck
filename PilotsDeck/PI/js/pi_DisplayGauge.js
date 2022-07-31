// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Empty.png",
	ErrorImage: "Images/Error.png",
	Address: "",
	DecodeBCD: false,
	Scalar: "1",
	Format: "",
	ValueMappings: "",
	HasAction: false,
	AddressAction: "",
	ActionType: 0,
	SwitchOnCurrentValue: true,
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

// Fill Select Boxes for Actions here
function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
	}
	if (FontNames && FontNames != "") {
		fillFontSelectBox(FontNames, 'FontName', settingsModel.FontName);
	}
	if (FontStyles && FontStyles != "") {
		fillTypeSelectBox(FontStyles, 'FontStyle', settingsModel.FontStyle);
	}
	if (GaugeOrientations && GaugeOrientations != "") {
		fillTypeSelectBox(GaugeOrientations, 'BarOrientation', settingsModel.BarOrientation);
	}
	if (ActionTypes && ActionTypes != "") {
		fillTypeSelectBox(ActionTypes, 'ActionType', settingsModel.ActionType);
		fillTypeSelectBox(ActionTypes, 'ActionTypeLong', settingsModel.ActionTypeLong);
	}
}

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('Address', 5);
	setPattern('AddressColorOff', 5);
	setPattern('AddressAction', settingsModel.ActionType);
	setPattern('AddressActionLong', settingsModel.ActionTypeLong);

	//ACTION
	if (settingsModel.HasAction) {
		//On/Off States & SwitchOnCurrent
		setFormItem(true, 'hdrSecondControl');
		toggleConfigItem(true, 'ActionType');
		toggleConfigItem(true, 'AddressAction');

		var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
		toggleOnOffState(settingsModel.ActionType, 'SwitchOnState', 'SwitchOffState', false);
		if (settingsModel.HasLongPress && longAllowed)
			toggleOnOffState(settingsModel.ActionTypeLong, 'SwitchOnStateLong', 'SwitchOffStateLong', false);
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
	else {
		setFormItem(false, 'hdrSecondControl');
		toggleConfigItem(false, 'ActionType');
		toggleConfigItem(false, 'AddressAction');
		toggleOnOffState(-1, 'SwitchOnState', 'SwitchOffState');
		toggleOnOffState(-1, 'SwitchOnStateLong', 'SwitchOffStateLong');
		toggleConfigItem(false, 'SwitchOnCurrentValue');
		toggleConfigItem(false, 'UseControlDelay');
		toggleConfigItem(false, 'UseLvarReset');

		toggleConfigItem(false, 'HasLongPress');
		toggleConfigItem(false, 'ActionTypeLong');
		toggleConfigItem(false, 'AddressActionLong');
	}

	//COLOR
	toggleConfigItem(settingsModel.UseColorSwitching, 'AddressColorOff');
	toggleConfigItem(settingsModel.UseColorSwitching, 'GaugeColorOff');
	toggleConfigItem(settingsModel.UseColorSwitching, 'StateColorOff');

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
	toggleConfigItem(settingsModel.ShowText && settingsModel.DrawWarnRange, 'UseWarnColors');
	toggleConfigItem(settingsModel.ShowText, 'FontInherit');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontName');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontSize');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontStyle');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontColor');
	toggleConfigItem(settingsModel.ShowText, 'RectCoord');
}
