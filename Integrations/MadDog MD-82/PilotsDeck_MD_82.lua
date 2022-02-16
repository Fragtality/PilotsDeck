--[[
1:	Copy this Script to your FSUIPC Folder
2:	Add this Script to your FSUIPC ini File:
			- Start/Stop P3D or manually add it to the [LuaFiles] Section (Preferred: let FSUIPC add it automatically)
			- Add it to your [Auto] Section, if this doesn't exist:
			[Auto]
			1=Lua PilotsDeck
3:	Add your Lua Code to this File for more advanced Checks. If the File is changed, you have to restart P3D.
4:	Write your Results to the general use Offset Range 66C0-66FF (64 bytes). You can and have to manage the Offset Addresses yourself ;)
5:	Configure the Plugin to read your Offsets.
		
	Quick Reference:
	type	lua command								bytes	signed	next offset		address in streamdeck
	integer	ipc.writeSB(0x66XX, value)				1		yes		X+1				66XX:1:i:s
	integer	ipc.writeUB(0x66XX, value)				1		no		X+1				66XX:1:i
	float	ipc.writeFLT(0x66XX, value)				4		n/a		X+4				66XX:4:f
	string	ipc.writeSTR(0x66XX, value, Length)		L+1		n/a		X+L+1 (zero!)	66XX:L:s
--]]

function Pilotsdeck_Poll ()
		AP_VS_DISP ()
		AP_HDG_DISP ()
		
end

event.timer(200, "Pilotsdeck_Poll")

function AP_VS_DISP ()

	VS = ipc.readUW("07F2")
	if VS < 10000 then
	ipc.writeSTR(0x04E2, string.format("%04.f", VS, 4)) else
    ipc.writeSTR(0x04E2, string.format("%05.f", VS - 65536), 5)
	 end
	
end

function AP_HDG_DISP ()
	HDG=ipc.readLvar("md_ipc_ap_hdg")
	ipc.writeSTR(0x04E7, string.format("%03.f", HDG, 3))
end	

