var settingsModel = {
	EnableSwitching: false,
	AircraftPathString: "",
	LoadedMappings: []
};
firstUpdate = true;

var imageSelectBoxes = [];
var toggleOnControlsMap = [];
var toggleOffControlsMap = [];
var toggleOnDivMap = [];
var toggleOffDivMap = []

function updateForm() {
	var pathstring = document.getElementById("AircraftPathString");
	if (settingsModel.AircraftPathString != null)
		pathstring.innerText = escape(settingsModel.AircraftPathString);

	var listElement = document.getElementById("LoadedMappings");
	for (var m = 0; m < settingsModel.LoadedMappings.length; m++) {
		var li = document.createElement('li');
		li.innerText = escape(settingsModel.LoadedMappings[m]);
		listElement.appendChild(li);
	}
}