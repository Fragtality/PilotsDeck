# ToLiSS A321 Files for PilotsDeck
Here you'll find a mostly working / ready to use Example I use myself for the ToLiSS A321. I have only the A321 but if it uses the same Commands and DataRefs, the Integration should work for the A319 also out-of-the-box.<br/>
My Setup consists of a XL Deck as the "Main Deck" and a SD Deck with supplementary/supportive Actions (e.g. showing the FCU while being in the "Flight" Folder on the XL), so there are two Profiles designed to be used together:
- **XP-ToLiSS-A321-XL:** XL Profile with Folders for the different Flight Phases and Folders for Lights (Int+Ext), Overhead, EFIS/Glareshield, FCU, MCDU and MIP/Pedestal
- **XP-ToLiSS-A321:** SD (15 Buttons) Profile FCU, Radio and Transponder<br/>

Mix and match as you like :relaxed:<br/>

![ExampleFNX01](../../img/ExampleFNX01.jpg)<br/>
![ExampleFNX02](../../img/ExampleFNX02.jpg)<br/>
![ExampleFNX03](../../img/ExampleFNX03.jpg)<br/>
![ExampleFNX04](../../img/ExampleFNX04.jpg)<br/>


# Installation
- Tested with ToLiSS A321 V1.4 on X-Plane 12
- Install/Update to at least PilotsDeck 0.7.3!
- Uses the Fonts *"Digital 7"* and *"Alte Din 1451 Mittelschrift"* - you can find them freely on the Internet
- Just double-click on the Profiles you want to use to add them to your StreamDeck Software
- Install FlyWithLua NG (XP11) or NG+ (XP12) and place the Lua-Files in the Script Folder
- Install the vJoy-Driver - some Buttons like the Fire Tests or CVR Test use them

# Configuration
The Sync-Script synchronizes the FD-Buttons for both Captain and FO. If you don't want that, set the Variable SYNC_FDLS to false.<br/>
The View-Buttons use the Quick-Look Commands and probably won't match your saved Locations. You can either edit the Actions to use another Location/Command OR you can setup your Camera/View and save the Location by Long-Pressing the coresponding Button on the StreamDeck.

## Button Mappings
These Commands need to be mapped in X-Plane to the vJoy Virtual Joystick:<br/>
| vJoy Button# | Command DataRef | Command Description |
| --- | --- | --- | 
| 31 | AirbusFBW/PaxOxyReleaseButtonPress | Release the passenger oxygen masks |
| 32 | AirbusFBW/CVRTest | CVR Test button pressed |
| 33 | AirbusFBW/FireTestENG1 | Test Fire detection circuits for engine 1 |
| 34 | AirbusFBW/FireTestAPU | Test Fire detection circuits for engine APU |
| 35 | AirbusFBW/FireTestENG2 | Test Fire detection circuits for engine 2 |
| 36 | AirbusFBW/CabVSUp| Manual Cabin Pressure Control held in 'UP' direction |
| 37 | AirbusFBW/CabVSDown | Manual Cabin Pressure Control held in 'DOWN' direction |
| 38 | sim/flight_controls/rudder_trim_left | Yaw trim left |
| 39 | sim/flight_controls/rudder_trim_right | Yaw trim right |
| 40 | sim/flight_controls/pitch_trim_down | Pitch trim down |
| 41 | sim/flight_controls/pitch_trim_up | Pitch trim up |
(The Button Number referr to the Number in the Blue Circle)<br/>

## PilotsDeck-TLS321-Cmd
This Script contains Lua Functions addressed by some Actions in the Profiles. They are prefixed with "FlyWithLua/TLS2PLD".<br/>

## PilotsDeck-TLS321-Sync
This Script runs every 6 Frames to generate some custom DataRefs used in the Profiles - it is vital for the FCU Displays. They are also prefixed with "FlyWithLua/TLS2PLD".<br/>
You can change the Update Frequency with the FRAME_MOD Variable at the top of the File.<br/>

# Usage
I hope/think most Buttons should be self-explanatory. Some Buttons use a Long-Press Actions, for Example:<br/>
- Light Switches which have 3 States. The normal Action is to switch from Off to mid-Position with the Long Press Action to switch from mid-Position to On. In the "Taxi" Folder this is swapped (so that you can quickly toggle all Lights when entering/leaving the Runway).
- The FCU-Displays (between the +/- Buttons) are also Buttons. They either toggle between Managed/Selected (normal Press) or toggle things like SPD/MACH, HDG/TRK, Alt Scale. The VS Display/Button is Pull (normal) or Push (long).
- Generally on other Multi-Position Switches the normal and long Press either are mapped to the different Directions (e.g. normal is UP, long is DOWN) OR the Action automatically toggles between the usual States (e.g. the XPDR from XPNDR to TA to TARA and back to XPNDR)
