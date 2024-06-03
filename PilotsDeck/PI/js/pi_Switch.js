// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/Switch.png",
	ErrorImage: "Images/SwitchError.png",
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
	ImageMap: ""
  };

var imageSelectBoxes = ["DefaultImage", "ErrorImage", "ImageGuard"];
var toggleOnControlsMap = [];
var toggleOffControlsMap = [];
var toggleOnDivMap = [];
var toggleOffDivMap = []

function updateForm() {
	//SwitchOnCurrent
	settingsModel.SwitchOnCurrentValue = false;
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');
}
