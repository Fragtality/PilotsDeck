// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Empty.png",
	ErrorImage: "Images/Error.png",
	Address: "",
	AddressAction: "",
	ActionType: 0,
	OnState: "",
	OffState: "",
	HasLongPress: false,
	AddressActionLong: "",
	ActionTypeLong: 0,
	OnStateLong: "",
	OffStateLong: "",
	DecodeBCD: false,
	Scalar: 1,
	Format: "",
	DrawBox: true,
	BoxSize: 2,
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
	FontSize: 10,
	FontStyle: 0,
	FontColor: "#ffffff",
	RectCoord: "-1; 0; 0; 0"
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
	if (FontStyles && FontStyles != "") {
		fillTypeSelectBox(FontStyles, 'FontStyle', settingsModel.FontStyle);
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
	setPattern('AddressAction', settingsModel.ActionType);
	setPattern('AddressActionLong', settingsModel.ActionTypeLong);

	//On/Off States
	toggleOnOffState(settingsModel.ActionType, 'OnState', 'OffState');
	if (settingsModel.HasLongPress)
		toggleOnOffState(settingsModel.ActionTypeLong, 'OnStateLong', 'OffStateLong');
	else
		toggleOnOffState(-1, 'OnStateLong', 'OffStateLong');

	//LongPress
	toggleConfigItem(settingsModel.HasLongPress, 'ActionTypeLong');
	toggleConfigItem(settingsModel.HasLongPress, 'AddressActionLong');

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
