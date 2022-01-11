// Implement settingsModel for the Action
var settingsModel = {
		DefaultImage: "Images/Empty.png",
		ErrorImage: "Images/Error.png",
		AddressAction: "",
		ActionType: 0,
		SwitchOnState: "",
		SwitchOffState: "",
		UseControlDelay: false,
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
	if (ActionTypes && ActionTypes != "") {
		fillTypeSelectBox(ActionTypes, 'ActionType', settingsModel.ActionType);
		fillTypeSelectBox(ActionTypes, 'ActionTypeLong', settingsModel.ActionTypeLong);
	}
}

function updateForm() {
	//PATTERN
	setPattern('AddressAction', settingsModel.ActionType);
	setPattern('AddressActionLong', settingsModel.ActionTypeLong);
	setPattern('AddressTop', 5);
	setPattern('AddressBot', 5);

	//On/Off States & SwitchOnCurrent
	var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
	toggleOnOffState(settingsModel.ActionType, 'SwitchOnState', 'SwitchOffState', settingsModel.SwitchOnCurrentValue);
	if (settingsModel.HasLongPress && longAllowed)
		toggleOnOffState(settingsModel.ActionTypeLong, 'SwitchOnStateLong', 'SwitchOffStateLong', settingsModel.SwitchOnCurrentValue);
	else
		toggleOnOffState(-1, 'SwitchOnStateLong', 'SwitchOffStateLong');
	toggleConfigItem(false, 'SwitchOnCurrentValue');

	toggleControlDelay(settingsModel);

	//LongPress
	toggleConfigItem(longAllowed, 'HasLongPress');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'ActionTypeLong');
	toggleConfigItem(settingsModel.HasLongPress && longAllowed, 'AddressActionLong');

	//only Top adr
	toggleConfigItem(!settingsModel.UseOnlyTopAddr, 'AddressBot');

	//non-zero
	toggleConfigItem(!settingsModel.ShowTopNonZero, 'TopState')
	toggleConfigItem(!settingsModel.ShowBotNonZero, 'BotState')
}
