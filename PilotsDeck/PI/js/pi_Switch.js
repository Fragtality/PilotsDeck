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
	UseControlDelay: false,
	UseLvarReset: false,
	SwitchOnCurrentValue: false,
	HasLongPress: false,
    AddressActionLong: "",
    ActionTypeLong: 0,
    SwitchOnStateLong: "",
    SwitchOffStateLong: ""
  };

var imageSelectBoxes = ["DefaultImage", "ErrorImage"];

function updateForm() {
	//SwitchOnCurrent
	settingsModel.SwitchOnCurrentValue = false;
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');
}
