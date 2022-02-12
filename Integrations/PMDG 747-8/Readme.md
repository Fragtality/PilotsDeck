# PMDG 747-8 Files for PilotsDeck
Here is a User Contribution for a 747-8 Profile - thanks Ado198 for sharing this! :thumbsup:<br/>


# Installation
- Update to at least PilotsDeck 0.6.3
- Copy the 747_8.lua file into your fsuipc folder.
- Copy the DSA_GSX_AUTO.lua into your fsuipc folder.(This file was created by Fragtality and is optional but recommended.) \[Dev comment: This is an older Version of the GSX_AUTO available from QualityWings2GSX, you might want to change it to the newer Version\]
- Copy the images from the images folder into the Pilotsdeck plugin directory.
- Run P3D to let FSUIPC register the LUA and then close P3D.
- Open FSUIPC.ini and add the 2 above lua files under an Auto start section, preferably under profile specific.
- ... or add them as Auto-Scripts to your FSUIPC6.ini. Then add the following to your FSUIPC6.ini:<br/>
```
	[Auto.747] ----- change to your profile name.
	1=Lua 747_8
	2=Lua DSA_GSX_AUTO
	3=Lua PilotsDeck
```
if the above section does not exist in your ini file just create it.<br/>
If you already use the included PilotsDeck.lua from the Plugin for other Things, you either have to merge them by Hand, or rename this Script-File from the Profile before Step 5.<br/>
**IMPORTANT**: To enable the data communication output from the PMDG aircraft, you will need to open the file 747QOTSII options ini file (which will be located in the FSX/P3D PMDG folder for the 747, and add the following lines to the end of the file:
```
	[SDK]
	EnableDataBroadcast=1
```
# Usage
- This file should work with every variant of the 747 as they mostly share all of the same variables. It would need some small modifications as there are 747-8 specific buttons. Also if you fly a passanger version just MAP the extra doors which are not needed for the cargo variant.
- Double check as some buttons have a Long Press feature. ie. the battery button - you must Long Press to open guard then press normal to turn on or off. The guard will then close automatically.
- If you open the 747_8 lua file you will notice some functions have a TCA comment. This was used to map my TCA throttle. You may do the same or just ignore them. The lua will still work.
- If some fonts look a little off it may be because of missing fonts in your system. Just customize them to your liking using the plugin tool.
- Customize, modify, do whatever you want to this file to make it functional and useful to you.

Credit to Günter Steiner the creator of the LINDA file that was used for some references.
