---@diagnostic disable: undefined-global

local FDswitched = false
local ticks = 0

function FNX_SYNC()
	ticks = ticks + 1
	FNX_READ_LANDING_LGT() --0x5400 + 1
	FNX_READ_BARO()	--0x5401 + 5
	FNX_READ_GEAR() --0x5406/0x5407 + 1
	--0x5408 --> 0x5xxx used in Binary
	FNX_READ_CLOCK() --0x5500 + 9
	FNX_READ_TRIM() --0x5509 + 8


	FNX_SYNC_FD()
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
	elseif ipc.readLvar("I_FCU_TRACK_FPA_MODE") == 0 and ipc.readLvar("I_FCU_HEADING_VS_MODE") == 0 then
		newvalue = ""
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

function FNX_READ_CLOCK()
	if ticks % 2 == 0 then
		local result = string.format("%02d:%02d:%02d", ipc.readUB(0x023B), ipc.readUB(0x023C), ipc.readUB(0x023A))
		ipc.writeSTR(0x5500, result, 9)
	end
end

function FNX_BTN_PRESS(lvar, delay, value, offvalue)
	delay = delay or 150
	value = value or 1
	offvalue = offvalue or 0
	ipc.writeLvar(lvar, value)
	ipc.sleep(delay)
	ipc.writeLvar(lvar, offvalue)
end

function FNX_READ_TRIM()
	local trim = (ipc.readLvar("A_FC_ELEVATOR_TRIM") * 0.01)
	if trim > 0 then
		trim = math.floor(trim + 0.49)
	else
		trim = math.ceil(trim - 0.49)
	end

	local up = trim > 0.5
	local down = trim < -0.51
	trim = math.abs(trim * 0.1)

	local result = string.format("%0.1f", trim)
	if up then
		result = result .. " UP"
	elseif down then
		result = result .. " DN"
	end

	ipc.writeSTR(0x5509, result, 8)
end

event.timer(250, "FNX_SYNC")