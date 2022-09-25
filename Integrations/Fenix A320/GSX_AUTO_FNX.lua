---@diagnostic disable: undefined-global

local fnxIsColdAndDark = true

function GSX_CUSTOM_IS_AUTO_DEBOARD()
	return true
end

function FNX_BTN_PRESS(lvar, delay, value, offvalue)
	delay = delay or 150
	value = value or 1
	offvalue = offvalue or 0
	ipc.writeLvar(lvar, value)
	ipc.sleep(delay)
	ipc.writeLvar(lvar, offvalue)
end

function GSX_CUSTOM_CONNECT()
	if fnxIsColdAndDark then
		fnxIsColdAndDark = false
		GSX_AUTO_MENU(4500)

		local gsxJetway = ipc.readLvar("FSDT_GSX_JETWAY")
		if gsxJetway == 2 then
			ipc.log("GSX_CUSTOM_CONNECT: Connect Stairs")
			GSX_AUTO_STAIRS_TOGGLE()
		end

		if ipc.readLvar("B_CONFIG_GPU") ~= 1 then
			ipc.log("GSX_CUSTOM_CONNECT: Connect GPU & AirCon")
			GSX_CUSTOM_GPU(false)
		else
			ipc.log("GSX_CUSTOM_CONNECT: Connect AirCon")
			FNX_GROUND_AIR()
		end

		return true
	else
		return false
	end
end

function GSX_CUSTOM_DISCONNECT()
	local board_state = ipc.readLvar("FSDT_GSX_BOARDING_STATE")
	if board_state == 6 then
		if ipc.readLvar("B_CONFIG_GPU") == 1 then
			ipc.log("GSX_CUSTOM_DISCONNECT: Disconnect GPU & AirCon")
			GSX_CUSTOM_GPU(false)
		end

		return true
	else
		return false
	end
end

function GSX_CUSTOM_JETWAY(menuIsOpen)
	GSX_CUSTOM_GPU(false)
	return false
end

function GSX_CUSTOM_STAIRS(menuIsOpen)
	return false
end

function GSX_CUSTOM_GPU(menuIsOpen)
	FNX_BTN_PRESS("S_CDU1_KEY_MENU")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK6R")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK3L")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK2L")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK3L")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_INIT")
end

function FNX_GROUND_AIR()
	FNX_BTN_PRESS("S_CDU1_KEY_MENU")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK6R")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK3L")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK3L")
	ipc.sleep(125)
	FNX_BTN_PRESS("S_CDU1_KEY_INIT")
end

function GSX_CUSTOM_REFUEL()
	return false
end

function GSX_CUSTOM_CATER()
	return false
end

function GSX_CUSTOM_BOARD()
	return false
end

function GSX_CUSTOM_PUSH(depature_state)
	if ipc.readLvar("B_CONFIG_GPU") == 1 then
		ipc.log("GSX_CUSTOM_DISCONNECT: Disconnect GPU & AirCon")
		GSX_CUSTOM_GPU(false)
	end
	GSX_AUTO_SET_CONNECTED(0)
	return false
end

function GSX_CUSTOM_PUSHCONFIRM()
	return false
end

function GSX_CUSTOM_DEBOARD()
	return false
end



