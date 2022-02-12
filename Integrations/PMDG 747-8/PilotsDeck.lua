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
		IAS_DISPLAY ()
		VS_DISPLAY ()
		HDG ()
		readBARO()
	
end

event.timer(200, "Pilotsdeck_Poll")

function IAS_DISPLAY ()
	IAS=ipc.readUD(0x6599)
	if ipc.readUB(0x659D) == 0 then
	ipc.writeUD(0x04E0, IAS) else
	ipc.writeUD(0x04E0, 500)
	end
end

function VS_DISPLAY ()
	VS=ipc.readSW(0x65A2)
	if ipc.readUB(0x65A4) == 0 then
	ipc.writeSW(0x66D1, VS) else
	ipc.writeSW(0x66D1, 5000)
	end
end

function HDG ()
	local hdgval = ipc.readUW(0x659E)
	hdgval = string.format("%0.f", hdgval)
	if hdgval == "360" then
		hdgval = "0"
	end

	hdg = string.format("%03.f", hdgval)
	ipc.writeSTR(0x66D4, hdg, 8)
	end
	
function readBARO()
	local unitMode = ipc.readUB(0x6561)
	local pressure = ipc.readUW(0x0330)
	
	
	if pressure ~= 16211 then
	if unitMode == 0 then
		ipc.writeSTR(0x66E1, string.format("%.0f", pressure * 0.1845585973546601), 4)
		else
		ipc.writeSTR(0x66E1, string.format("%.0f", pressure * 0.0625), 4)
		end
	else
	ipc.writeSTR(0x66E1, "STD", 4)
	end
	
end