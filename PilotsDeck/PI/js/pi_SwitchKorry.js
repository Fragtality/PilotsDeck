var settingsModel = {
	DefaultImage: "Images/Empty.png",
	ErrorImage: "Images/Error.png",
	IsEncoder: false,
	AddressAction: "",
	AddressActionOff: "",
	ActionType: 0,
	SwitchOnState: "",
	SwitchOffState: "",
	AddressMonitor: "",
	ToggleSwitch: false,
	HoldSwitch: false,
	UseControlDelay: false,
	UseLvarReset: false,
	SwitchOnCurrentValue: false,
	HasLongPress: false,
    AddressActionLong: "",
    ActionTypeLong: 0,
    SwitchOnStateLong: "",
	SwitchOffStateLong: "",
	AddressTop: "",
	AddressBot: "",
	UseOnlyTopAddr: false,
	ShowTopImage: true,
	ShowBotImage: true,
	TopState: "",
	ShowTopNonZero: false,
	BotState: "",
	ShowBotNonZero: false,
	TopImage: "Images/korry/A-FAULT.png",
	BotImage: "Images/korry/A-ON-Blue.png",
	TopRect: "9; 21; 54; 20",
	BotRect: "9; 45; 54; 20",
	IsGuarded: false,
	AddressGuardActive: "",
	GuardActiveValue: "",
	AddressActionGuard: "",
	AddressActionGuardOff: "",
	ActionTypeGuard: 0,
	SwitchOnStateGuard: "",
	SwitchOffStateGuard: "",
	ImageGuard: "Images/GuardCross.png",
	GuardRect: "0; 0; 72; 72",
	UseImageGuardMapping: false,
	ImageGuardMap: "",
	UseImageMapping: false,
	ImageMap: "",
	ImageMapBot: ""
};

var imageSelectBoxes = ["DefaultImage", "ErrorImage", "ImageGuard"];
var korrySelectBoxes = ["TopImage", "BotImage"];
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
	toggleConfigItem(settingsModel.ShowBotImage && !settingsModel.UseImageMapping, 'BotImage');
	toggleConfigItem(settingsModel.ShowBotImage && settingsModel.UseImageMapping, 'ImageMapBot');
	setFormItem(settingsModel.ShowBotImage && !settingsModel.UseImageMapping, 'Prev_BotImage');
	toggleConfigItem(settingsModel.ShowBotImage && !settingsModel.UseImageMapping, 'ShowBotNonZero');

	//CURRENT VALUE
	settingsModel.SwitchOnCurrentValue = false;
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');
}
