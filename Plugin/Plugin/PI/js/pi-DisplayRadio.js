var imageSelectBoxes = ["DefaultImage", "IndicationImage", "ImageGuard"];
var toggleOnControlsMap = [];
var toggleOffControlsMap = [];
var toggleOnDivMap = [];
var toggleOffDivMap = []

function updateForm() {
	//PATTERN
	setPattern('AddressRadioActiv', 5);
	setPattern('AddressRadioStandby', 5);

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
