---@diagnostic disable: undefined-global

function DSA_GSX_MENU()
	ipc.keypress(123,11)
end

function DSA_PROATC_MENU()
	ipc.keypress(86)
end


function DSA_GSX_JETW_TOGGLE2()
	local DSA_GSX_JETWAYS_DOCKED = ipc.readLvar("DSA_GSX_JETWAYS_DOCKED")

	ipc.keypress(123,11) --gsx menu
	ipc.sleep(1000)
	ipc.control(67141)	--6
	ipc.sleep(10000)
	ipc.control(67136)	--1
	ipc.sleep(1000)
	ipc.control(67137)	--2
	ipc.sleep(1000)
	ipc.control(67138)	--3
	ipc.sleep(5000)
	if DSA_GSX_JETWAYS_DOCKED == 0 then
		DSA_GSX_JETWAYS_DOCKED = 2
	else
		DSA_GSX_JETWAYS_DOCKED = 0
	end
	ipc.createLvar("DSA_GSX_JETWAYS_DOCKED", DSA_GSX_JETWAYS_DOCKED)
end

function DSA_GSX_JETW_TOGGLE1()
	local DSA_GSX_JETWAYS_DOCKED = ipc.readLvar("DSA_GSX_JETWAYS_DOCKED")

	ipc.keypress(123,11) --gsx menu
	ipc.sleep(1000)
	ipc.control(67141)	--6
	ipc.sleep(10000)
	ipc.control(67136)	--1
	ipc.sleep(1000)
	ipc.control(67137)	--2
	ipc.sleep(5000)
	if DSA_GSX_JETWAYS_DOCKED == 0 then
		DSA_GSX_JETWAYS_DOCKED = 1
	else
		DSA_GSX_JETWAYS_DOCKED = 0
	end
	ipc.createLvar("DSA_GSX_JETWAYS_DOCKED", DSA_GSX_JETWAYS_DOCKED)
end

function DSA_GSX_PUSH()
	local DSA_GSX_JETWAYS_DOCKED = ipc.readLvar("DSA_GSX_JETWAYS_DOCKED")
	if DSA_GSX_JETWAYS_DOCKED == 1 then
		DSA_GSX_JETW_TOGGLE1()
	elseif DSA_GSX_JETWAYS_DOCKED == 2 then
		DSA_GSX_JETW_TOGGLE2()
	end

	local depature_state = ipc.readLvar("FSDT_GSX_DEPARTURE_STATE")
	if depature_state == 1 then
		ipc.keypress(123,11) --gsx menu
		ipc.sleep(1000)
		ipc.control(67140)	--5 request
		ipc.sleep(1000)
	elseif depature_state == 5 then
		ipc.keypress(123,11) --gsx menu
		ipc.sleep(1000)
		ipc.control(67136)	--1 confirm good
		ipc.sleep(1000)
	end
end

function DSA_GSX_DEBOARD()
		ipc.keypress(123,11) --gsx menu
		ipc.sleep(1000)
		ipc.control(67136)	--1
		ipc.sleep(1000)
end

function DSA_GSX_CATER()
		ipc.keypress(123,11) --gsx menu
		ipc.sleep(1000)
		ipc.control(67137)	--2
		ipc.sleep(1000)
end

function DSA_GSX_REFUEL()
		ipc.keypress(123,11) --gsx menu
		ipc.sleep(1000)
		ipc.control(67138)	--3
		ipc.sleep(1000)
end

function DSA_GSX_BOARD()
		ipc.keypress(123,11) --gsx menu
		ipc.sleep(1000)
		ipc.control(67139)	--4
		ipc.sleep(1000)
end

function DSA_GSX_SERVICE_TOGGLE()
	local fuel_state = ipc.readLvar("FSDT_GSX_REFUELING_STATE")
	local cater_state = ipc.readLvar("FSDT_GSX_CATERING_STATE")

	if fuel_state <= 4 then
		DSA_GSX_REFUEL()
	elseif cater_state <= 4 then
		DSA_GSX_CATER()
	elseif ipc.readLvar("FSDT_GSX_BOARDING_STATE") <= 4 then
		DSA_GSX_BOARD()
	end
end

function DSA_GSX_SERVICE_CYCLE()
	local cycle_state = ipc.readLvar("DSA_GSX_SERVICE_STATE")
	local fuel_state = ipc.readLvar("FSDT_GSX_REFUELING_STATE")
	local cater_state = ipc.readLvar("FSDT_GSX_CATERING_STATE")
	local board_state = ipc.readLvar("FSDT_GSX_BOARDING_STATE")
	local deboard_state = ipc.readLvar("FSDT_GSX_DEBOARDING_STATE")

	if cycle_state == 0 and fuel_state < 4 then
		DSA_GSX_REFUEL()
		cycle_state = 1
	elseif cycle_state == 0 and fuel_state >= 4 then
		cycle_state = 1
	elseif cycle_state == 1 and cater_state < 4 then
		DSA_GSX_CATER()
		cycle_state = 2
	elseif cycle_state == 1 and cater_state >= 4 then
		cycle_state = 2
	elseif cycle_state == 2 and board_state < 4 then
		DSA_GSX_BOARD()
		cycle_state = 3
	elseif cycle_state == 2 and board_state >= 4 then
		cycle_state = 3
	elseif cycle_state == 3 and deboard_state < 4 then
		DSA_GSX_DEBOARD()
		cycle_state = 0
	elseif cycle_state == 3 and deboard_state >= 4 then
		cycle_state = 0
	end

	ipc.createLvar("DSA_GSX_SERVICE_STATE", cycle_state)

end




-----------------------------------------
-----------------------------------------
-- $$ EVENT FLAGS

event.flag(1, "DSA_GSX_MENU")
event.flag(2, "DSA_GSX_JETW_TOGGLE1")
event.flag(3, "DSA_GSX_JETW_TOGGLE2")
event.flag(4, "DSA_GSX_PUSH")
event.flag(5, "DSA_GSX_DEBOARD")
event.flag(6, "DSA_GSX_CATER")
event.flag(7, "DSA_GSX_REFUEL")
event.flag(8, "DSA_GSX_BOARD")
event.flag(9, "DSA_GSX_SERVICE_TOGGLE")
event.flag(10, "DSA_GSX_SERVICE_CYCLE")
event.flag(11, "DSA_PROATC_MENU")
