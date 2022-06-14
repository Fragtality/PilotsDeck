--[[
1:	Copy this Script to your FSUIPC Folder
2:	Add this Script to your FSUIPC ini File:
			- Start/Stop P3D or manually add it to the [LuaFiles] Section (Preferred: let FSUIPC add it automatically)
			- Add it to your [Auto] Section, if this doesn't exist:
			[Auto]
			1=Lua PilotsDeck_737
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
		HDG ()
		CRS ()
		readBARO()
		
		
	
end

event.timer(200, "Pilotsdeck_Poll")



function HDG ()
	local hdgval = ipc.readLvar("ngx_HDGwindow")
	hdgval = string.format("%0.f", hdgval)
	if hdgval == "360" then
		hdgval = "0"
	end

	hdg = string.format("%03.f", hdgval)
	ipc.writeSTR(0x66D4, hdg, 8)
	end
	
function CRS()
	local crsval = ipc.readLvar("ngx_CRSwindowL")
	crsval = string.format("%0.f", crsval)
	if crsval == "360" then
		crsval = "0"
	end

	crs = string.format("%03.f", crsval)
	ipc.writeSTR(0x66E1, crs, 8)
	end
	
function readBARO()
	local unitMode = ipc.readLvar("switch_366_73X")
	local pressure = ipc.readUW(0x0330)
	
	
	
	if unitMode == 0 then
		ipc.writeSTR(0x66F1, string.format("%.0f", pressure * 0.1845585973546601), 4)
		else
		ipc.writeSTR(0x66F1, string.format("%.0f", pressure * 0.0625), 4)
		end
		
	end
	
	
	

	
	
