// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Empty.png",
	ErrorImage: "Images/Error.png",
	Address: "",
	Address2: "",
	DecodeBCD: false,
	Scalar: 1,
    Format: "",
	MinimumValue: "0",
	MaximumValue: "100",
	BarSize: "58; 10",
	BarOrientation: 0,
	BarColor: "#006400",
	IndicatorColor: "#c7c7c7",
	IndicatorSize: 10,
	IndicatorFlip: true,
	CenterLine: false,
	CenterLineColor: "#ffffff",
	CenterLineThickness: 2,
	DrawWarnRange: false,
	SymmRange: false,
	CriticalColor: "#8b0000",
	WarnColor: "#ff8c00",
	CriticalRange: "0; 10",
	WarnRange: "11; 25",
	ShowText: true,
	UseWarnColors: true,
	FontInherit: true,
	FontName: "Arial",
	FontSize: 10,
	FontStyle: 0,
	FontColor: "#ffffff",
	RectCoord: "6; 45; 60; 21",
	RectCoord2: "6; 6; 60; 21"
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
}

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('Address', 5);
	setPattern('Address2', 5);

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
	toggleConfigItem(settingsModel.ShowText, 'UseWarnColors');
	toggleConfigItem(settingsModel.ShowText, 'FontInherit');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontName');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontSize');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontStyle');
	toggleConfigItem(settingsModel.ShowText && !settingsModel.FontInherit, 'FontColor');
	toggleConfigItem(settingsModel.ShowText, 'RectCoord');
	toggleConfigItem(settingsModel.ShowText, 'RectCoord2');
}
