var imageSelectBoxes = ["DefaultImage", "IndicationImage"];
var toggleOnControlsMap = ["ImageMap"];
var toggleOffControlsMap = ["DirIndicationImage", "IndicationImage"];
var toggleOnDivMap = [];
var toggleOffDivMap = []

function updateForm() {
	//PATTERN
	setPattern('Address', 5);

	//BOX
	toggleConfigItem(settingsModel.DrawBox, 'BoxSize');
	toggleConfigItem(settingsModel.DrawBox, 'BoxColor');
	toggleConfigItem(settingsModel.DrawBox, 'BoxRect');

	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'IndicationHideValue');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationUseColor');
	toggleConfigItem(settingsModel.HasIndication && settingsModel.IndicationUseColor, 'IndicationColor');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationValue');
	toggleConfigItem(settingsModel.HasIndication, 'UseImageMapping');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.UseImageMapping, 'DirIndicationImage');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.UseImageMapping, 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication && settingsModel.UseImageMapping, 'ImageMap');

	//FONT
	toggleConfigItem(!settingsModel.FontInherit, 'FontName');
	toggleConfigItem(!settingsModel.FontInherit, 'FontSize');
	toggleConfigItem(!settingsModel.FontInherit, 'FontStyle');
	toggleConfigItem(!settingsModel.FontInherit, 'FontColor');
}
