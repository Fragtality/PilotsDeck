// Implement settingsModel for the Action
var settingsModel = {
	EnableSwitching: false,
	ProfilesInstalled: false,
	UseDefault: true,
	DefaultProfile: "",
	MappingsJson: ""
};

// Fill Select Boxes for Actions here
function fillSelectBoxes() {

}

// Show/Hide elements on Form (required function)
function updateForm() {
	toggleConfigItem(settingsModel.UseDefault, 'DefaultProfile');

	if (!settingsModel.MappingsJson)
		return;
	var profileMappings = JSON.parse(settingsModel.MappingsJson);

	var divProfiles = document.getElementById('divWrapper');

	if (profileMappings && profileMappings.length) {
		for (var i = 0; i < profileMappings.length; i++) {
			if (!document.getElementById('Config_' + i)) {
				var divOuter = document.createElement('div');
				divOuter.className = "spdi-item";
				divOuter.id = "Config_" + i;

				var divLabel = document.createElement('div');
				divLabel.className = "spdi-item-label";
				divLabel.id = "lbl" + i;
				divLabel.innerHTML = profileMappings[i].Name;

				var input = document.createElement('input');
				input.className = "spdi-item-value"
				input.id = i;
				input.type = "text";
				input.value = profileMappings[i].Mappings;
				input.setAttribute("onchange", "setJsonSettings(event.target.value, event.target.id)");

				divOuter.appendChild(divLabel);
				divOuter.appendChild(input);

				divProfiles.appendChild(divOuter);
			}
		}
	}
}

const setJsonSettings = (value, index) => {
	var profileMappings = JSON.parse(settingsModel.MappingsJson)
	profileMappings[parseInt(index)].Mappings = value;
	var jsonStr = JSON.stringify(profileMappings);
	setSettings(jsonStr, "MappingsJson");
}