# FBW A32NX Files for PilotsDeck
Here you'll find a mostly working / ready to use Example I made myself for the FlyByWire A320 Neo. A registered Version of FSUIPC7 is required. It has the most critical Buttons/Switches/Korries from the Overhead, MIP/Glareshield, Pedestal, FCU and MCDU (with Full Keyboard).<br/>
My Setup consists of a XL Deck as the "Main Deck" and a SD Deck with supplementary/supportive Actions (e.g. showing the FCU while being in the "Flight" Folder on the XL), so there are two Profiles designed to be used together:
- **MSFS-FBW32N-XL:** XL Profile with Folders for the different Flight Phases and Folders for Lights (Int+Ext), Overhead, EFIS, MCP, CDU and MIP/Pedestal
- **MSFS-FBW32N:** SD (15 Buttons) Profile FCU, Radio and Transponder<br/>

So with a SD only you'll can still use the SD-Profile separately (and generally the Plugin to build your own Profile). Also there is a third "SDLIB" Profile with nearly all Buttons from the XL-Profile. It's intended to be used to built your own SD Profile with it.

Mix and match as you like! :relaxed:<br/>

![ExampleFNX00](../../img/ExampleFNX00.jpg)<br/>
![ExampleFBW00](../../img/ExampleFBW00.jpg)<br/>
![ExampleFNX02](../../img/ExampleFNX02.jpg)<br/>

# Installation
- Tested with A32NX Development Release fe80bbf (20.06.22)
- Minimum Plugin-Version is 0.7.0
- Uses the Fonts *"Digital 7"* and *"Alte Din 1451 Mittelschrift"* - you can find them freely on the Internet
- Just double-click on the Profiles to add them to your StreamDeck Software
- Place the Lua-Files in your FSUIPC7 Folder (the Folder where your FSUIPC7.ini is located)
- Place the myOffsets.txt in your FSUIPC7 Folder (or merge it with your existing one)
- The cameras.cfg is optional (for the Views via vJoy-Driver Buttons)! If you want to use it: %appdata%\Microsoft Flight Simulator\SimObjects\Airplanes\FlyByWire_A320_NEO
- Either start the Scripts (Auto + Sync) manually ...
- ... or add them as Auto-Scripts to your FSUIPC7.ini. Start MSFS/FSUIPC7 once so the Files are added (if you're not familiar with adding them manually). Then add the following to your FSUIPC7.ini:<br/>
```
[Auto.FBW320]
1=Lua FBW320_AUTO
2=Lua FBW320_SYNC
```
Assuming your FSUIPC Profile is named "FBW320"! Replace that with the correct Name. If already using Auto-Scripts, change the Numbers accordingly (these Scripts don't need to be run first).<br/>
If you don't have a FSUIPC Profile for the FlyByWire, start them as "Global" Scripts:
```
[Auto]
1=Lua FBW320_AUTO
2=Lua FBW320_SYNC
```

# Configuration
The "myOffset.txt" File is used to make some SimVars accessible via FSUIPC Offsets. Addresses at/above 0x58A0 are used (normally associated with "Project Magenta")! If these are used by some other Addon you have to change them in the Lua and the Profile.


## Profiles
The View Buttons use the vJoy Device Driver and Custom Cameras. You have to install the vJoy Device Driver (and configure a Joystick), map the vJoy's Buttons to the "Load Custom Camera" Bindings and Save/Create your Custom Cameras for the respective Button. If you want to use these! When you don't want to use the View-Buttons you don't need the vJoy Driver installed.<br/>
The Mapping I use:<br/>
| vJoy Button# | Mapping in MSFS | Title in Profile |
| --- | --- | --- | 
| 1 | Load Custom Camera 1 | Captain |
| 2 | Load Custom Camera 2 | Overhead |
| 3 | Load Custom Camera 3 | Pedestal |
| 4 | Load Custom Camera 4 | ECAM / MIP |
| 5 | Load Custom Camera 5 | MCDU |
| 6 | Load Custom Camera 6 | EFB |
| 7 | Load Custom Camera 7 | Eng L (Passenger, front of Engine) |
| 8 | Load Custom Camera 8 | Eng R (Passenger, front of Engine) |
| 9 | Load Custom Camera 9 | Pax L (Passenger View, behind Wing) |
| 10 | Load Custom Camera 0 | Pax R (Passenger View, behind Wing) |
| 11 | Toggle External View | External |
| 12 | Toggle Cockpit View | Cockpit |


## FBW320_AUTO
This Script contains the Functions addressed by some Actions in the Profiles. They are addressed via "LuaToggle:FBW320_AUTO:*NUM*" in PilotsDeck.<br/>
Which *NUM*ber maps to which Function can be found at the End of the File! This Script is essential for these Buttons to work. These Functions also be mapped in the FSUIPC GUI to Joystick Buttons with "LuaToggle FBW320_AUTO" and as Parameter the *NUM*ber.

## FBW320_SYNC
This Script is essential for some Buttons / Displays from the Profiles to show their State / Value (Ext Power, FCU, Baro, Gear, Clock).<br/>
It uses Offsets at 0x5800 and above to generate Informations for some Buttons - if these are used by some other Addon you have to change them in the Lua and the Profile.<br/>
It also reloads the WASM Module (once) as soon as both Batteries and External Power is on.

# Usage
I hope/think most Buttons should be self-explanatory. Some Buttons use a Long-Press Actions, for Example:<br/>
- Light Switches which have 3 States. The normal Action is to switch from Off to mid-Position with the Long Press Action to switch from mid-Position to On. In the "Taxi" Folder this is swapped (so that you can quickly toggle all Lights when entering/leaving the Runway).
- The FCU-Display (between the +/- Buttons) are also Buttons. They either toggle between Managed/Selected (normal Press) or toggle things like SPD/MACH, HDG/TRK, Alt Scale. The VS Display/Button is Pull (normal) or Push (long).<br/><br/>
❗ Some Lvars are created very late by the FlyByWire - if some Buttons don't work or their State is not reflected correctly: go into FSUIPC7 and Reload the WASM Module. That should fix it!
