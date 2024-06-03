// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/SwitchDefault.png",
	ErrorImage: "Images/Error.png",
	IsEncoder: false,
	Address: "",
    HasIndication: false,
    IndicationImage: "Images/Fault.png",
    IndicationValue: "0",
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
	SwitchOnCurrentValue: true,
	HasLongPress: false,
	AddressActionLong: "",
	ActionTypeLong: 0,
	SwitchOnStateLong: "",
	SwitchOffStateLong: "",
	OnImage: "Images/KorryOnBlueTop.png",
	OnState: "",
	OffImage: "Images/KorryOffWhiteBottom.png",
	OffState: "",
	IndicationValueAny: false,
	UseImageMapping: false,
	ImageMap: "",
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
	ImageGuardMap: ""
};

var imageSelectBoxes = ["OnImage", "OffImage", "DefaultImage", "ErrorImage", "IndicationImage", "ImageGuard"];
var toggleOnControlsMap = ["ImageMap"];
var toggleOffControlsMap = [];
var toggleOnDivMap = [];
var toggleOffDivMap = ["DefaultMapping"]

function updateForm() {
	//SwitchOnCurrent
	if ((settingsModel.ActionType != 3 && settingsModel.ActionType != 4 && settingsModel.ActionType != 11 && settingsModel.ActionType != 12) || settingsModel.UseImageMapping) {
		settingsModel.SwitchOnCurrentValue = false;
		document.getElementById('SwitchOnCurrentValue').checked = false;
		toggleConfigItem(false, 'SwitchOnCurrentValue');
	}
	else {
		toggleConfigItem(true, 'SwitchOnCurrentValue');
	}
	toggleOnOffState(settingsModel.ActionType, 'SwitchOnState', 'SwitchOffState', settingsModel.SwitchOnCurrentValue, settingsModel.ToggleSwitch);

	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationValueAny');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.IndicationValueAny, 'IndicationValue');
}