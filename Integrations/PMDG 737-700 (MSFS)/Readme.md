# PMDG 737-700 (MSFS) Files for PilotsDeck
Here is a User Contribution for a 737-700 Profile - thanks Ado198 for sharing this! :thumbsup:<br/>


# Installation
- Update to at least PilotsDeck 0.6.5
- Copy the images to the Images Sub-folder of the Pilotsdeck plugin directory.
- Double click on the StreamDeck Profile to install it.
- Copy the PilotsDeck_737.lua in the FSUIPC main folder
- Locate the 737.ini file and insert the following line at the end of the file:
```
	[SDK]
    EnableDataBroadcast=1
```
The location of ini file should be in %localappdata%\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalState\packages\pmdg-aircraft737\work (MS Store) or %appdata%\Microsoft Flight Simulator\Packages\pmdg-aircraft737\work (Steam).
- Run MSFS to let FSUIPC register the LUA and then restart MSFS (Restarting FSUIPC7 also should do the Trick).
- Open FSUIPC7.ini and add the above lua File under an Auto start section, preferably for your FSUIPC-Profile used for the 737 (if exists).<br/>
```
	[Auto.PMDG737] ----- change to your profile name.
	1=Lua PilotsDeck_737
```
If the above section does not exist in your ini file just create it.<br/>
If you don't have a specific FSUIPC-Profile for 737, it is just "\[Auto\]" to run it globally.<br/>

# Usage and Limitations
- The displays on the streamdeck sometimes don't display info at start up. Reload the WASM module from the FSUIPC addons menu.
- VS display does not show negative values. This is a PMDG issue and should fix itself once PMDG correct it.
- The cameras are configured for my liking using the Njoy driver. It's very easy to use and I recommend it. However, you can just delete them and   replace them with your own.
- A lot of the buttons including the the displays have long presses associated so test all buttons to see what they do.
- Have fun and make this profile your own by customizing to meet your needs.
