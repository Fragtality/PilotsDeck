// Implement settingsModel for the Action
var settingsModel = {
	DefaultImage: "Images/SwitchDefault.png",
	ErrorImage: "Images/Error.png",
	Address: "",
    HasIndication: false,
    IndicationImage: "Images/Fault.png",
    IndicationValue: "0",
	AddressAction: "",
	ActionType: 0,
	OnImage: "Images/KorryOnBlueTop.png",
	OnState: "",
	OffImage: "Images/KorryOffWhiteBottom.png",
	OffState: "",
	IndicationValueAny: false
  };

function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
		fillImageSelectBox(ImageFiles, 'IndicationImage', settingsModel.IndicationImage);
		fillImageSelectBox(ImageFiles, 'OnImage', settingsModel.OnImage);
		fillImageSelectBox(ImageFiles, 'OffImage', settingsModel.OffImage);
	}
	if (ActionTypes && ActionTypes != "") {
		fillTypeSelectBox(ActionTypes, 'ActionType', settingsModel.ActionType);
	}
}

function updateForm() {
	//ACTION TYPE pattern
	setPattern('AddressAction', settingsModel.ActionType);

	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationValueAny');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.IndicationValueAny, 'IndicationValue');
}