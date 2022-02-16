# MadDog MD-82 Files for PilotsDeck
Here is a User Contribution for a MD-82 Profile - thanks Ado198 for sharing this! :thumbsup:<br/>


# Installation
- Update to at least PilotsDeck 0.6.3
- Copy the Lua files and the md_80.MCRO file into your fsuipc folder.
- Copy the GSX_AUTO.lua into your fsuipc folder.(This file was created by Fragtality and is optional but recommended.)
- Copy the images from the images folder into the Pilotsdeck plugin directory.
- Double click the profile to install it.
- Run P3D to let FSUIPC register the LUA and then close P3D.
- Open FSUIPC.ini and add the above lua files under an Auto start section, preferably under profile specific.<br/>

```
	[Auto.Md-82] ----- change to your profile name.
	1=Lua Md_82_ovhd
	2=Lua GSX_AUTO
	3=Lua PilotsDeck_MD_82
```
if the above section does not exist in your ini file just create it.<br/>

# Usage
- Double check as some buttons have a Long Press feature and some of the diplays are also buttons. (\*recommend you change longPressTicks from default "3" to "2" in the plugin settings options)
- The view (ie. Captain) buttons are configured using Vjoy. Open fsuipc and map them like any joystick button.
- If some fonts look a little off it may be because of missing fonts in your system. Just customize them to your liking using the plugin tool.
- Customize, modify, do whatever you want to this file to make it functional and useful to you.

Credit to Günter Steiner the creator of the LINDA file that was used for some references.
