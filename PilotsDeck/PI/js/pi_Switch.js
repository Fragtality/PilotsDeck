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

function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
	}
}

function updateForm() {
	//SwitchOnCurrent
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');
}
