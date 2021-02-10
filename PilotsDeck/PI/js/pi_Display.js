// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/ValueFrame.png",
	ErrorImage: "Images/ValueError.png",
	Address: "",
	DecodeBCD: false,
	Scalar: 1,
    Format: "",
    HasIndication: false,
	IndicationHideValue: false,
	IndicationUseColor: false,
	IndicationColor: "#ffffff",
    IndicationImage: "Images/ValueFault.png",
    IndicationValue: "0",
    FontInherit: true,
	FontName: "Arial",
	FontSize: 10,
	FontStyle: 0,
	FontColor: '#ffffff',
	RectCoord: "11; 23; 48; 40"
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
	if (FontStyles || FontStyles != "") {
		fillTypeSelectBox(FontStyles, 'FontStyle', settingsModel.FontStyle);
	}
}

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('Address', 5);

	//FORMAT
	//toggleConfigItem(!settingsModel.DecodeBCD, 'Config_Scalar', 'lblScalar', 'Scalar');

	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'Config_IndicationHideValue', 'lblIndicationHideValue', 'IndicationHideValue');
	toggleConfigItem(settingsModel.HasIndication, 'Config_IndicationImage', 'lblIndicationImage', 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.IndicationHideValue, 'Config_IndicationUseColor', 'lblIndicationUseColor', 'IndicationUseColor');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.IndicationHideValue && settingsModel.IndicationUseColor, 'Config_IndicationColor', 'lblIndicationColor', 'IndicationColor');
	toggleConfigItem(settingsModel.HasIndication, 'Config_IndicationValue', 'lblIndicationValue', 'IndicationValue');

	//FONT
	toggleConfigItem(!settingsModel.FontInherit, 'Config_FontName', 'lblFontName', 'FontName')
	toggleConfigItem(!settingsModel.FontInherit, 'Config_FontSize', 'lblFontSize', 'FontSize')
	toggleConfigItem(!settingsModel.FontInherit, 'Config_FontStyle', 'lblFontStyle', 'FontStyle')
	toggleConfigItem(!settingsModel.FontInherit, 'Config_FontColor', 'lblFontColor', 'FontColor')
}
