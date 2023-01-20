﻿// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Empty.png",
	ErrorImage: "Images/Error.png",
	Address: "",
	IsEncoder: false,
	DecodeBCD: false,
	Scalar: "1",
	Format: "",
	ValueMappings: "",
	DrawBox: true,
	BoxSize: "2",
	BoxColor: "#ffffff",
	BoxRect: "9; 21; 54; 44",
    HasIndication: false,
	IndicationHideValue: false,
	IndicationUseColor: false,
	IndicationColor: "#ffcc00",
    IndicationImage: "Images/Empty.png",
    IndicationValue: "0",
    FontInherit: true,
	FontName: "Arial",
	FontSize: "10",
	FontStyle: 0,
	FontColor: "#ffffff",
	RectCoord: "-1; 0; 0; 0"
};

var imageSelectBoxes = ["DefaultImage", "ErrorImage", "IndicationImage"];

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('Address', 5);

	//BOX
	toggleConfigItem(settingsModel.DrawBox, 'BoxSize');
	toggleConfigItem(settingsModel.DrawBox, 'BoxColor');
	toggleConfigItem(settingsModel.DrawBox, 'BoxRect');

	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'IndicationHideValue');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationUseColor');
	toggleConfigItem(settingsModel.HasIndication && settingsModel.IndicationUseColor, 'IndicationColor');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationValue');

	//FONT
	toggleConfigItem(!settingsModel.FontInherit, 'FontName');
	toggleConfigItem(!settingsModel.FontInherit, 'FontSize');
	toggleConfigItem(!settingsModel.FontInherit, 'FontStyle');
	toggleConfigItem(!settingsModel.FontInherit, 'FontColor');
}
