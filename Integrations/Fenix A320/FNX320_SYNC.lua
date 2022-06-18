local FDswitched = false

function FNX_SYNC()
	FNX_SYNC_FD()
	FNX_READ_LANDING_LGT() --0x5400 + 1
	FNX_READ_BARO()	--0x5401 + 5
	FNX_READ_GEAR() --0x5406/0x5407 + 1
	--0x5408 --> 0x5xxx used in Binary
end

function FNX_SYNC_FD()
	local posCapt = ipc.readLvar("I_FCU_EFIS1_FD")
	if posCapt ~= ipc.readLvar("I_FCU_EFIS2_FD") and not FDswitched then
		ipc.writeLvar("S_FCU_EFIS2_FD", 1)
		ipc.sleep(125)
		ipc.writeLvar("S_FCU_EFIS2_FD", 0)
		FDswitched = true
	elseif FDswitched then
		FDswitched = false
	end
end

function FNX_READ_LANDING_LGT()
	local pos = ipc.readLvar("S_OH_EXT_LT_LANDING_L") + ipc.readLvar("S_OH_EXT_LT_LANDING_R")
	ipc.writeUB(0x5400,pos)
end

function FNX_READ_BARO()
	local unitMode = ipc.readLvar("S_FCU_EFIS1_BARO_MODE")
	local pressure = ipc.readUW(0x0330)
	local offset = 0x5401
	local newvalue = "Std"

	if ipc.readLvar("S_OH_IN_LT_ANN_LT") == 2 then
		newvalue = "8888"
	elseif ipc.readLvar("S_FCU_EFIS1_BARO_STD")	~= 1 then
		if unitMode == 0 then
			newvalue = string.format("%.0f", pressure * 0.1845585973546601)
			newvalue = string.sub(newvalue, 0,2) .. "." .. string.sub(newvalue, 3)
		else
			newvalue = string.format("%04.0f", pressure * 0.0625)
		end
	end
	
	ipc.writeSTR(offset, newvalue, 5)
end

function FNX_READ_GEAR()
	local gearUnlk = ipc.readLvar("I_MIP_GEAR_1_U") + ipc.readLvar("I_MIP_GEAR_2_U") + ipc.readLvar("I_MIP_GEAR_3_U")
	local gearDown = ipc.readLvar("I_MIP_GEAR_1_L") + ipc.readLvar("I_MIP_GEAR_2_L") + ipc.readLvar("I_MIP_GEAR_3_L")
	
	ipc.writeUB(0x5406, gearUnlk)
	ipc.writeUB(0x5407, gearDown)
end

event.timer(250, "FNX_SYNC")