var imageSelectBoxes = [];
var toggleOnControlsMap = [];
var toggleOffControlsMap = [];
var toggleOnDivMap = [];
var toggleOffDivMap = []

function updateForm() {
}

function updateProfileSwitcher(settingsModel) {
	var element = document.getElementById("EnableSwitching");
	if (element != null)
		element.checked = settingsModel.EnableSwitching;

	element = document.getElementById("SwitchBack");
	if (element != null)
		element.checked = settingsModel.SwitchBack;

	element = document.getElementById("AircraftPathString");
	if (element != null && settingsModel.AircraftPathString != null)
		element.innerHTML = settingsModel.AircraftPathString;

	element = document.getElementById("LoadedMappings");
	if (!element)
		return;

	element.innerHTML = "";
	settingsModel.LoadedMappings.forEach(mapping => {
		var li = document.createElement('li');
		li.appendChild(document.createTextNode(escape(mapping)));
		element.appendChild(li);
	});
}