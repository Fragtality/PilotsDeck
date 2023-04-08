---@diagnostic disable: undefined-global

----------------------------------
-- CONFIGURATION
local delayOperator = 0		--Delay for manual Operator Selection before next Action (applied when not connected and in Refuel State)
local writeOffsets = true		--Write Offsets for display on PilotsDeck
local opStairsDeboard = false	--Operate the Stairs after Deboard (not needed anymore, GSX Bug is fixed.)
local noCrewDeBoard = true		--Disable Boarding and Deboarding of Crew
local GSX_OFFSET_PAX = 0x66C0 	--String, Length 5 0x66C0:5:s
local GSX_OFFSET_CARGO = 0x66C5 --String, Length 6 0x66C5:6:s



----------------------------------
-- Variables
local GSX_AUTO_SERVICE_STATE = 0
ipc.createLvar("GSX_AUTO_SERVICE_STATE", GSX_AUTO_SERVICE_STATE)
local GSX_AUTO_CONNECTED = 0
local GSX_AUTO_JETWAY_CONNECTED = false
ipc.createLvar("GSX_AUTO_JETWAY_CONNECTED", GSX_AUTO_JETWAY_CONNECTED)
ipc.createLvar("GSX_AUTO_CONNECTED", GSX_AUTO_CONNECTED)
ipc.createLvar("GSX_AUTO_CONNECT_REQUESTED", 0)
ipc.createLvar("GSX_AUTO_DISCONNECT_REQUESTED", 0)
ipc.createLvar("GSX_AUTO_DEBOARD_REQUESTED", 0)

function GSX_AUTO_SET_CONNECTED(value)
	GSX_AUTO_CONNECTED = value
	ipc.writeLvar("GSX_AUTO_CONNECTED", value)
end

function GSX_DISABLE_CREW()
	ipc.log("GSX_AUTO: Disabling Crew De/Boarding")
	ipc.writeLvar("FSDT_GSX_CREW_ON_BOARD", 1)
	ipc.writeLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1)
	ipc.writeLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1)
	ipc.writeLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1)
	ipc.writeLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1)
	ipc.writeLvar("FSDT_GSX_CREW_NOT_BOARDING", 1)
	ipc.writeLvar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1)
	ipc.writeLvar("FSDT_GSX_CREW_NOT_DEBOARDING", 1)
	ipc.writeLvar("FSDT_GSX_NUMCREW", 0)
	ipc.writeLvar("FSDT_GSX_NUMPILOTS", 0)
end

local debugArrival = false
if debugArrival then
	GSX_AUTO_SERVICE_STATE = 5
	GSX_AUTO_JETWAY_CONNECTED = false
	ipc.writeLvar("GSX_AUTO_SERVICE_STATE", GSX_AUTO_SERVICE_STATE)
	ipc.writeLvar("FSDT_GSX_NUMPASSENGERS", 30)
	GSX_DISABLE_CREW()
end

local debugDeparture = false
if debugDeparture then
	GSX_AUTO_SERVICE_STATE = 3
	ipc.writeLvar("GSX_AUTO_SERVICE_STATE", GSX_AUTO_SERVICE_STATE)
end


----------------------------------
-- AC Handling
local customAcHandling = false
local aircraft = ipc.readSTR(0x3C00,256)
ipc.log(aircraft)

function GSX_AUTO_LOAD_AC()
	if string.find(aircraft, "fnx320") then
		require "GSX_AUTO_FNX"
		customAcHandling = true
		ipc.log("GSX_AUTO: Usinge custom Aircraft Handling for Fenix A320")
	else
		customAcHandling = false
	end
end

GSX_AUTO_LOAD_AC()

----------------------------------
-- Service/Cycle State, Request Handling
local firstStart = true
function GSX_AUTO_SYNC_CYCLE()
	local couatlStarted = ipc.readLvar("FSDT_GSX_COUATL_STARTED") == 1

	if couatlStarted and firstStart and noCrewDeBoard then
		GSX_DISABLE_CREW()
		firstStart = false
	elseif not couatlStarted then
		return
	end
	
	if ipc.readSTR(0x3C00,256) ~= aircraft then
		GSX_AUTO_LOAD_AC()
	end

	local fuel_state = ipc.readLvar("FSDT_GSX_REFUELING_STATE")
	local cater_state = ipc.readLvar("FSDT_GSX_CATERING_STATE")
	local board_state = ipc.readLvar("FSDT_GSX_BOARDING_STATE")
	local deboard_state = ipc.readLvar("FSDT_GSX_DEBOARDING_STATE")
	local depart_state = ipc.readLvar("FSDT_GSX_DEPARTURE_STATE")
	local enginesStopped = ipc.readLvar("FSDT_VAR_EnginesStopped") == 1
	local onGnd = ipc.readSW(0x0366)

	-- SERVICE STATE
	-- 0 => Refuel
	-- 1 => Cater
	-- 2 => Board
	-- 3 => Push
	-- 4 => Taxi Out
	-- 5 => Flight
	-- 6 => Taxi In
	-- 7 => Deboard

	if GSX_AUTO_SERVICE_STATE == 0 and (fuel_state >= 4 or board_state >= 4 ) then
		GSX_AUTO_SERVICE_STATE = 1
		ipc.log("GSX_AUTO: Service State switched from Refuel to Cater")
	elseif GSX_AUTO_SERVICE_STATE == 1 and (cater_state >= 5 or board_state >= 4 )then
		GSX_AUTO_SERVICE_STATE = 2
		ipc.log("GSX_AUTO: Service State switched from Cater to Board")
	elseif GSX_AUTO_SERVICE_STATE == 2 and board_state >= 4 and board_state < 6 and not GSX_AUTO_JETWAY_CONNECTED then
		ipc.log("GSX_AUTO: Reconnecting Jetway for Boarding")
		ipc.control(66695)
	elseif GSX_AUTO_SERVICE_STATE == 2 and board_state == 6 then
		GSX_AUTO_SERVICE_STATE = 3
		ipc.log("GSX_AUTO: Service State switched from Bord to Push")
	elseif GSX_AUTO_SERVICE_STATE == 3 and depart_state == 6 then
		GSX_AUTO_SERVICE_STATE = 4
		ipc.log("GSX_AUTO: Service State switched from Push to Taxi Out")
	elseif onGnd ~= 1 and GSX_AUTO_SERVICE_STATE ~= 5 then
		GSX_AUTO_SERVICE_STATE = 5
		GSX_AUTO_JETWAY_CONNECTED = false
		ipc.log("GSX_AUTO: Service State switched to Flight")
	elseif GSX_AUTO_SERVICE_STATE == 5 and onGnd == 1 then
		GSX_AUTO_SERVICE_STATE = 6
		ipc.log("GSX_AUTO: Service State switched from Flight to Taxi In")
		if noCrewDeBoard then
			GSX_DISABLE_CREW()
		end
	elseif GSX_AUTO_SERVICE_STATE == 6 and enginesStopped then
		GSX_AUTO_SERVICE_STATE = 7
		ipc.log("GSX_AUTO: Service State switched from Taxi In to Deboard")
		if customAcHandling and GSX_CUSTOM_IS_AUTO_DEBOARD() then
			ipc.log("GSX_AUTO: Call Connect/Deboard automatically")
			GSX_AUTO_CONNECT()
			GSX_AUTO_DEBOARD()
		end
	elseif GSX_AUTO_SERVICE_STATE == 7 and deboard_state >=4 and deboard_state < 6 and not GSX_AUTO_JETWAY_CONNECTED then
		ipc.log("GSX_AUTO: Reconnecting Jetway for Deboarding")
		ipc.control(66695)
	elseif GSX_AUTO_SERVICE_STATE == 7 and deboard_state == 6 then
		GSX_AUTO_SERVICE_STATE = 0
		ipc.log("GSX_AUTO: Service State switched from Deboard to Refuel")
		if opStairsDeboard and ipc.readLvar("FSDT_GSX_JETWAY") == 2 and ipc.readLvar("FSDT_GSX_STAIRS") == 5 then --Try to remove Stairs for them not blocking the Refuel ...
			ipc.log("GSX_AUTO: Remove Stairs after Deboard")
			GSX_AUTO_MENU(1500)
			GSX_AUTO_KEY(7)
			ipc.sleep(500)
		end
	end

	ipc.writeLvar("GSX_AUTO_SERVICE_STATE", GSX_AUTO_SERVICE_STATE)

	-- REQUEST HANDLING
	if ipc.readLvar("GSX_AUTO_CONNECT_REQUESTED") == 1 and GSX_AUTO_CONNECTED == 0 then
		ipc.writeLvar("GSX_AUTO_CONNECT_REQUESTED", 0)
		ipc.log("GSX_AUTO: Connect Request received")
		GSX_AUTO_CONNECT()
	end

	if ipc.readLvar("GSX_AUTO_DISCONNECT_REQUESTED") == 1 and GSX_AUTO_CONNECTED == 1 then
		ipc.writeLvar("GSX_AUTO_DISCONNECT_REQUESTED", 0)
		ipc.log("GSX_AUTO: Disconnect Request received")
		GSX_AUTO_DISCONNECT()
	end

	if ipc.readLvar("GSX_AUTO_DEBOARD_REQUESTED") == 1 and deboard_state < 6 then
		ipc.writeLvar("GSX_AUTO_DEBOARD_REQUESTED", 0)
		ipc.log("GSX_AUTO: Deboard Request received")
		GSX_AUTO_DEBOARD()
	end

	-- UPDATE OFFSETS
	if writeOffsets then
		GSX_AUTO_UPDATE_OFFSETS(board_state, deboard_state)
	end
end

function GSX_AUTO_UPDATE_OFFSETS(board_state, deboard_state)
	local resultPax = ""
	local resultCargo = ""

	if GSX_AUTO_SERVICE_STATE == 2 or GSX_AUTO_SERVICE_STATE == 7 then
		local plnPax = ipc.readLvar("FSDT_GSX_NUMPASSENGERS")
		local brdPax = ipc.readLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL")
		local debrdPax = ipc.readLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL")
		local ldCargo = ipc.readLvar("FSDT_GSX_BOARDING_CARGO_PERCENT")
		local unldCargo = ipc.readLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT")

		if GSX_AUTO_SERVICE_STATE == 2 and board_state >= 4 then
			resultPax = tostring(brdPax)
			if plnPax ~= brdPax then
				resultPax = resultPax .. " >"
			end

			resultCargo = tostring(ldCargo) .. "%"
			if ldCargo ~= 100 then
				resultCargo = "< " .. resultCargo
			end
		end
		if GSX_AUTO_SERVICE_STATE == 7 and deboard_state >= 4 then
			resultPax = tostring(plnPax - debrdPax)
			if plnPax ~= debrdPax then
				resultPax = "< " .. resultPax
			end

			resultCargo = tostring(100 - unldCargo) .. "%"
			if unldCargo ~= 100 then
				resultCargo = resultCargo .. " >"
			end
		end
	end
	
	ipc.writeSTR(GSX_OFFSET_PAX, resultPax, 5)
	ipc.writeSTR(GSX_OFFSET_CARGO, resultCargo, 6)
end

----------------------------------
-- Call Service Functions

function GSX_AUTO_MENU(sleep)
	if ipc.readLvar("FSDT_GSX_STATE") ~=5 then
		ipc.keypress(123,11)
	else
		ipc.writeLvar("FSDT_GSX_MENU_OPEN", 1)
	end
	ipc.sleep(sleep or 1000)
end

function GSX_AUTO_KEY(key, sleep)
	ipc.keypress(48 + key, 4)
	if sleep ~= nil then
		ipc.sleep(sleep)
	end
end

function GSX_AUTO_CONNECT_TGL()
	if GSX_AUTO_CONNECTED == 1 then
		GSX_AUTO_DISCONNECT()
	else
		GSX_AUTO_CONNECT()
	end
end

function GSX_AUTO_CONNECT()
	if customAcHandling and GSX_CUSTOM_CONNECT() then

	else
		GSX_AUTO_MENU(4500)
		local gsxJetway = ipc.readLvar("FSDT_GSX_JETWAY")
		if gsxJetway ~= 2 then
			if not GSX_AUTO_JETWAY_CONNECTED then
				ipc.log("GSX_AUTO_CONNECT: Connect Jetway")
				GSX_AUTO_KEY(6)
			end
		elseif gsxJetway == 2 then
			ipc.log("GSX_AUTO_CONNECT: Connect GPU")
			GSX_AUTO_GPU(true)
			ipc.sleep(750)
			ipc.log("GSX_AUTO_CONNECT: Connect Stairs")
			GSX_AUTO_STAIRS_TOGGLE()
		end
	end

	GSX_AUTO_SET_CONNECTED(1)
end

function GSX_AUTO_DISCONNECT()
	if customAcHandling and GSX_CUSTOM_DISCONNECT() then

	else
		GSX_AUTO_MENU(1750)
		local gsxJetway = ipc.readLvar("FSDT_GSX_JETWAY")
		if gsxJetway ~= 2 then
			if GSX_AUTO_JETWAY_CONNECTED then
				ipc.log("GSX_AUTO_DISCONNECT: Disconnect Jetway")
				GSX_AUTO_JETWAY_TOGGLE(true)
			end
		else
			ipc.log("GSX_AUTO_DISCONNECT: Disconnect GPU")
			GSX_AUTO_GPU(true)
			if ipc.readLvar("FSDT_GSX_DEBOARDING_STATE") ~= 6 then
				ipc.log("GSX_AUTO_DISCONNECT: Disconnect Stairs")
				GSX_AUTO_STAIRS_TOGGLE()
			end
		end
	end

	GSX_AUTO_SET_CONNECTED(0)
end

function GSX_AUTO_JETWAY_TOGGLE(menuIsOpen)
	if customAcHandling and GSX_CUSTOM_JETWAY(menuIsOpen) then

	else
		menuIsOpen = noMenu or false
		if not menuIsOpen then
			GSX_AUTO_MENU(1750)
		end

		if (GSX_AUTO_CONNECTED == 0 and GSX_AUTO_SERVICE_STATE == 0) then
			ipc.sleep(delayOperator)
		end

		GSX_AUTO_KEY(6, 250)
	end
end

function GSX_AUTO_STAIRS_TOGGLE(menuIsOpen)
	if customAcHandling and GSX_CUSTOM_STAIRS(menuIsOpen) then

	else
		menuIsOpen = noMenu or false
		if not menuIsOpen then
			GSX_AUTO_MENU(1750)
		end

		if (GSX_AUTO_CONNECTED == 0 and GSX_AUTO_SERVICE_STATE == 0) then
			ipc.sleep(delayOperator)
		end

		GSX_AUTO_KEY(7, 250)
	end
end

function GSX_AUTO_GPU(menuIsOpen)
	if customAcHandling and GSX_CUSTOM_GPU(menuIsOpen) then

	else
		menuIsOpen = noMenu or false
		if not menuIsOpen then
			GSX_AUTO_MENU(1750)
		end

		GSX_AUTO_KEY(8, 750)
		GSX_AUTO_KEY(1, 250)
	end
end

function GSX_AUTO_REFUEL()
	if customAcHandling and GSX_CUSTOM_REFUEL() then

	else
		GSX_AUTO_MENU(1750)
		GSX_AUTO_KEY(3, 250)
	end
end

function GSX_AUTO_CATER()
	if customAcHandling and GSX_CUSTOM_CATER() then

	else
		GSX_AUTO_MENU(1750)
		GSX_AUTO_KEY(2, 250)
	end
end

function GSX_AUTO_BOARD()
	if customAcHandling and GSX_CUSTOM_BOARD() then

	else
		GSX_AUTO_MENU(1750)
		GSX_AUTO_KEY(4, 250)
	end
end

function GSX_AUTO_PUSH()
	local depature_state = ipc.readLvar("FSDT_GSX_DEPARTURE_STATE")
	if customAcHandling and GSX_CUSTOM_PUSH(depature_state) then

	else
		if depature_state <= 1 then
			ipc.log("GSX_AUTO_PUSH: Request Push")
			GSX_AUTO_MENU(1750)
			GSX_AUTO_KEY(5, 500)
			if GSX_AUTO_JETWAY_CONNECTED then
				ipc.log("GSX_AUTO: Disconecting Jetway for Push")
				ipc.control(66695)
			end
			GSX_AUTO_SET_CONNECTED(0)
		elseif depature_state == 5 then
			ipc.log("GSX_AUTO_PUSH: Confirm Good-Start")
			GSX_AUTO_PUSH_CONFIRM()
		end
	end
end

function GSX_AUTO_PUSH_CONFIRM()
	if customAcHandling and GSX_CUSTOM_PUSHCONFIRM() then

	else
		GSX_AUTO_MENU(1750)
		GSX_AUTO_KEY(1, 500)
	end
end

function GSX_AUTO_DEBOARD()
	if customAcHandling and GSX_CUSTOM_DEBOARD() then

	else
		if GSX_AUTO_CONNECTED == 0 then
			GSX_AUTO_MENU(4500)

			local gsxJetway = ipc.readLvar("FSDT_GSX_JETWAY")
			if gsxJetway == 2 then
				ipc.log("GSX_AUTO_DEBOARD: Call GPU")
				GSX_AUTO_GPU(true)
				ipc.sleep(500)
			end

			GSX_AUTO_MENU(2500)
			GSX_AUTO_KEY(1, 500)

			GSX_AUTO_SET_CONNECTED(1)
		else
			GSX_AUTO_MENU(2500)
			GSX_AUTO_KEY(1, 500)
		end
	end
end

function GSX_AUTO_REMOTE_DEICE()
	if GSX_AUTO_SERVICE_STATE ~= 4 then
		return
	end
	
	GSX_AUTO_MENU(4500)
	GSX_AUTO_KEY(1, 500)
end

function GSX_AUTO_SERVICE_CYCLE()
	if GSX_AUTO_SERVICE_STATE == 0 then
		GSX_AUTO_REFUEL()
	elseif GSX_AUTO_SERVICE_STATE == 1 then
		GSX_AUTO_CATER()
	elseif GSX_AUTO_SERVICE_STATE == 2 then
		GSX_AUTO_BOARD()
	elseif GSX_AUTO_SERVICE_STATE == 3 then
		GSX_AUTO_PUSH()
	elseif GSX_AUTO_SERVICE_STATE == 4 then
		GSX_AUTO_REMOTE_DEICE()
	elseif GSX_AUTO_SERVICE_STATE == 7 then
		GSX_AUTO_DEBOARD()
	end
end


-----------------------------------------
-----------------------------------------
-- $$ EVENT FLAGS

event.flag(1, "GSX_AUTO_MENU")
event.flag(2, "GSX_AUTO_CONNECT")
event.flag(3, "GSX_AUTO_DISCONNECT")
event.flag(4, "GSX_AUTO_JETWAY_TOGGLE")
event.flag(5, "GSX_AUTO_STAIRS_TOGGLE")
event.flag(6, "GSX_AUTO_GPU")
event.flag(7, "GSX_AUTO_REFUEL")
event.flag(8, "GSX_AUTO_CATER")
event.flag(9, "GSX_AUTO_BOARD")
event.flag(10, "GSX_AUTO_PUSH")
event.flag(11, "GSX_AUTO_DEBOARD")
event.flag(12, "GSX_AUTO_SERVICE_CYCLE")
event.flag(13, "GSX_AUTO_CONNECT_TGL")

event.timer(3000, "GSX_AUTO_SYNC_CYCLE")
ipc.log("GSX_AUTO: GSX Sync active - starting in State " .. GSX_AUTO_SERVICE_STATE)

function GSX_AUTO_CHECK_JETWAY()
	if not GSX_AUTO_JETWAY_CONNECTED then
		GSX_AUTO_JETWAY_CONNECTED = true
		ipc.writeLvar("GSX_AUTO_JETWAY_CONNECTED", 1)
		ipc.log("GSX_AUTO: Jetway Event captured - connected")
	else
		GSX_AUTO_JETWAY_CONNECTED = false
		ipc.writeLvar("GSX_AUTO_JETWAY_CONNECTED", 0)
		ipc.log("GSX_AUTO: Jetway Event captured - disconnected")
	end
end
event.control(66695, "GSX_AUTO_CHECK_JETWAY")