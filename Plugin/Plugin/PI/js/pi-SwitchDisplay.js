var imageSelectBoxes = ["OnImage", "OffImage", "DefaultImage", "IndicationImage", "ImageGuard"];
var toggleOnControlsMap = ["ImageMap"];
var toggleOffControlsMap = ["DirIndicationImage", "IndicationImage"];
var toggleOnDivMap = [];
var toggleOffDivMap = ["DefaultMapping"]

function updateForm() {
	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'DirIndicationImage');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationValueAny');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.IndicationValueAny, 'IndicationValue');
}