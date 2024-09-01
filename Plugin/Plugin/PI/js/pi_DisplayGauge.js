var imageSelectBoxes = ["DefaultImage", "ImageGuard"];
var toggleOnControlsMap = ["ImageMap"];
var toggleOffControlsMap = ["DirDefaultImage", "DefaultImage"];
var toggleOnDivMap = [];
var toggleOffDivMap = []
var firstLoad = true;
var lastDrawArc = false;

function updateForm() {
	//PATTERN
	setPattern('AddressColorOff', 5);

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
