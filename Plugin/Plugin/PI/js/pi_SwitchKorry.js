var imageSelectBoxes = ["DefaultImage", "ImageGuard", "TopImage", "BotImage"];
var toggleOnControlsMap = [];
var toggleOffControlsMap = [];
var toggleOnDivMap = [];
var toggleOffDivMap = []

function updateForm() {
	//PATTERN
	setPattern('AddressTop', 5);
	setPattern('AddressBot', 5);

	//TOP
	if (settingsModel.ShowTopImage)
		document.getElementById('ShowTopImage').checked = true;
	else
		document.getElementById('ShowTopImage').checked = false;
	toggleConfigItem(settingsModel.ShowTopImage, 'AddressTop');
	toggleConfigItem(settingsModel.ShowTopImage && !settingsModel.UseImageMapping && !settingsModel.ShowTopNonZero, 'TopState');
	toggleConfigItem(settingsModel.ShowTopImage && !settingsModel.UseImageMapping, 'DirTopImage');
	toggleConfigItem(settingsModel.ShowTopImage && !settingsModel.UseImageMapping, 'TopImage');
	toggleConfigItem(settingsModel.ShowTopImage && settingsModel.UseImageMapping, 'ImageMap');
	setFormItem(settingsModel.ShowTopImage && !settingsModel.UseImageMapping, 'Prev_TopImage');
	toggleConfigItem(settingsModel.ShowTopImage && !settingsModel.UseImageMapping, 'ShowTopNonZero');


	//BOT
	if (settingsModel.ShowBotImage)
		document.getElementById('ShowBotImage').checked = true;
	else
		document.getElementById('ShowBotImage').checked = false;
	toggleConfigItem(settingsModel.ShowBotImage, 'AddressBot');
	toggleConfigItem(settingsModel.ShowBotImage && !settingsModel.UseImageMapping && !settingsModel.ShowBotNonZero, 'BotState');
	toggleConfigItem(settingsModel.ShowBotImage && !settingsModel.UseImageMapping, 'DirBotImage');
	toggleConfigItem(settingsModel.ShowBotImage && !settingsModel.UseImageMapping, 'BotImage');
	toggleConfigItem(settingsModel.ShowBotImage && settingsModel.UseImageMapping, 'ImageMapBot');
	setFormItem(settingsModel.ShowBotImage && !settingsModel.UseImageMapping, 'Prev_BotImage');
	toggleConfigItem(settingsModel.ShowBotImage && !settingsModel.UseImageMapping, 'ShowBotNonZero');
}
