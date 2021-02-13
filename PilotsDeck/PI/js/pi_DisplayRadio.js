// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Arrow.png",
	ErrorImage: "Images/SwitchError.png",
	AddressRadioActiv: "",
	AddressRadioStandby: "",
	AddressAction: "",
	ActionType: 0,
	OffState: "",
	DecodeBCD: false,
	Scalar: 1,
	Format: "",
	StbyHasDiffFormat: false,
	DecodeBCDStby: false,
	ScalarStby: 1,
	FormatStby: "",
	IndicationImage: "Images/ArrowBright.png",
    FontInherit: true,
	FontName: "Arial",
	FontSize: 10,
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
	}
}

// Show/Hide elements on Form (required function)
function updateForm() {
	//PATTERN
	setPattern('AddressRadioActiv', 5);
	setPattern('AddressRadioStandby', 5);
	setPattern('AddressAction', settingsModel.ActionType);

	//On/Off States
	if (settingsModel.ActionType == 0) { //macro
		toggleConfigItem(false, 'OffState');
	}
	else if (settingsModel.ActionType == 1) { //script
		toggleConfigItem(false, 'OffState');
	}
	else if (settingsModel.ActionType == 2) { //control
		toggleConfigItem(false, 'OffState');
	}
	else if (settingsModel.ActionType == 3)  { //lvar
		toggleConfigItem(true, 'OffState');
	}
	else if (settingsModel.ActionType == 4)  { //offset
		toggleConfigItem(true, 'OffState');
	}
	else {
		toggleConfigItem(false, 'OffState');
	}

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
