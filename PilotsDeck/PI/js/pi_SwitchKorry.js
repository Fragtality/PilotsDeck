// Implement settingsModel for the Action
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
	UseImageGuardMapping: false,
	ImageGuardMap: ""
};

var imageSelectBoxes = ["DefaultImage", "ErrorImage", "ImageGuard"];
var korrySelectBoxes = ["TopImage", "BotImage"];

function updateForm() {
	//PATTERN
	setPattern('AddressTop', 5);
	setPattern('AddressBot', 5);

	//only Top adr
	toggleConfigItem(!settingsModel.UseOnlyTopAddr, 'AddressBot');
	toggleConfigItem(!settingsModel.UseOnlyTopAddr, 'BotState');
	toggleConfigItem(!settingsModel.UseOnlyTopAddr, 'BotImage');
	setFormItem(!settingsModel.UseOnlyTopAddr, 'Prev_BotImage');
	toggleConfigItem(!settingsModel.UseOnlyTopAddr, 'ShowBotNonZero');

	//non-zero
	toggleConfigItem(!settingsModel.ShowTopNonZero, 'TopState')
	toggleConfigItem(!settingsModel.UseOnlyTopAddr && !settingsModel.ShowBotNonZero, 'BotState')

	//CURRENT VALUE
	settingsModel.SwitchOnCurrentValue = false;
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');
}
