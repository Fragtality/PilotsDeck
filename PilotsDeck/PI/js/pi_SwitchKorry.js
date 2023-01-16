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
	BotRect: "9; 45; 54; 20"
  };

function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
	}
	if (KorryFiles && KorryFiles != "") {
		fillImageSelectBox(KorryFiles, 'TopImage', settingsModel.TopImage);
		fillImageSelectBox(KorryFiles, 'BotImage', settingsModel.BotImage);
	}
}

function updateForm() {
	//PATTERN
	setPattern('AddressTop', 5);
	setPattern('AddressBot', 5);

	//only Top adr
	toggleConfigItem(!settingsModel.UseOnlyTopAddr, 'AddressBot');

	//non-zero
	toggleConfigItem(!settingsModel.ShowTopNonZero, 'TopState')
	toggleConfigItem(!settingsModel.ShowBotNonZero, 'BotState')

	//CURRENT VALUE
	document.getElementById('SwitchOnCurrentValue').checked = false;
	toggleConfigItem(false, 'SwitchOnCurrentValue');
}
