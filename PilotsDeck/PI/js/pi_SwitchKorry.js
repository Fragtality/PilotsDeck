// Implement settingsModel for the Action
var settingsModel = {
		DefaultImage: "Images/Empty.png",
		ErrorImage: "Images/Error.png",
		AddressAction: "",
		ActionType: 0,
		OnState: "",
		OffState: "",
		HasLongPress: false,
        AddressActionLong: "",
        ActionTypeLong: 0,
        OnStateLong: "",
		OffStateLong: "",
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

	//On/Off States
	var longAllowed = isLongPressAllowed(settingsModel.ActionType, settingsModel.AddressAction);
	toggleOnOffState(settingsModel.ActionType, 'OnState', 'OffState');
	if (settingsModel.HasLongPress && longAllowed)
		toggleOnOffState(settingsModel.ActionTypeLong, 'OnStateLong', 'OffStateLong');
	else
		toggleOnOffState(-1, 'OnStateLong', 'OffStateLong');

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
