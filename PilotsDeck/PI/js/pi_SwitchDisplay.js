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
	ImageMap: ""
  };

function fillSelectBoxes() {
	if (ImageFiles && ImageFiles != "") {
		fillImageSelectBox(ImageFiles, 'DefaultImage', settingsModel.DefaultImage);
		fillImageSelectBox(ImageFiles, 'ErrorImage', settingsModel.ErrorImage);
		fillImageSelectBox(ImageFiles, 'IndicationImage', settingsModel.IndicationImage);
		fillImageSelectBox(ImageFiles, 'OnImage', settingsModel.OnImage);
		fillImageSelectBox(ImageFiles, 'OffImage', settingsModel.OffImage);
	}
}

function updateForm() {
	//SwitchOnCurrent
	if ((settingsModel.ActionType != 3 && settingsModel.ActionType != 4 && settingsModel.ActionType != 11) || settingsModel.UseImageMapping) {
		document.getElementById('SwitchOnCurrentValue').checked = false;
		toggleConfigItem(false, 'SwitchOnCurrentValue');
	}
	else {
		document.getElementById('SwitchOnCurrentValue').checked = true;
		toggleConfigItem(true, 'SwitchOnCurrentValue');
	}

	//INDICATION
	toggleConfigItem(settingsModel.HasIndication, 'IndicationImage');
	toggleConfigItem(settingsModel.HasIndication, 'IndicationValueAny');
	toggleConfigItem(settingsModel.HasIndication && !settingsModel.IndicationValueAny, 'IndicationValue');

	//Image Mapping
	toggleConfigItem(settingsModel.UseImageMapping, 'ImageMap');
	setFormItem(!settingsModel.UseImageMapping, 'DefaultMapping');
}