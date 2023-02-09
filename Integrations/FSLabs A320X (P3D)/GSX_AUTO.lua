---@diagnostic disable: undefined-global

ipc.sleep(15000) --Wait some time before really starting, to avoid "Load Order Problems"

----------------------------------
-- CONFIGURATION
local delayOperator = 7500		--Delay for manual Operator Selection before next Action (applied when not connected and in Refuel State)
local writeOffsets = true		--Write Offsets for display on PilotsDeck
local opStairsDeboard = false	--Operate the Stairs after Deboard (not needed anymore, GSX Bug is fixed.)
local GSX_OFFSET_PAX = 0x66C0 	--String, Length 5
local GSX_OFFSET_CARGO = 0x66C5 --String, Length 6

----------------------------------
-- Variables


local PLDFSL = nil
local aircraft = ipc.readSTR(0x3C00,256)
if string.find(aircraft, "FSLabs") and (string.find(aircraft, "A320") or string.find(aircraft, "A321") or string.find(aircraft, "A319")) then
	PLDFSL = require "PLDFSL"
elseif string.find(aircraft, "QualityWings 787") then
	ipc.sleep(15000) --Give the QW787 init process some time to avoid "Load Order Problems"
end

local GSX_AUTO_SERVICE_STATE = 0
ipc.createLvar("GSX_AUTO_SERVICE_STATE", GSX_AUTO_SERVICE_STATE)
local GSX_AUTO_CONNECTED = 0
ipc.createLvar("GSX_AUTO_CONNECTED", GSX_AUTO_CONNECTED)
ipc.createLvar("GSX_AUTO_CONNECT_REQUESTED", 0)
ipc.createLvar("GSX_AUTO_DEBOARD_REQUESTED", 0)
local GSX_AUTO_NUM_DOORS = 1
ipc.createLvar("GSX_AUTO_NUM_DOORS", 1)

local debugArrival = false
if debugArrival then
	GSX_AUTO_SERVICE_STATE = 5
	ipc.writeLvar("GSX_AUTO_SERVICE_STATE", GSX_AUTO_SERVICE_STATE)
	ipc.writeLvar("FSDT_GSX_NUMPASSENGERS", 30);
	ipc.writeLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1);
	ipc.writeLvar("FSDT_GSX_CREW_NOT_BOARDING", 1);
	ipc.writeLvar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1);
	ipc.writeLvar("FSDT_GSX_CREW_NOT_DEBOARDING", 1);
	ipc.writeLvar("FSDT_GSX_NUMCREW", 0);
	ipc.writeLvar("FSDT_GSX_NUMPILOTS", 0);
end

local debugDeparture = false
if debugDeparture then
	GSX_AUTO_SERVICE_STATE = 3
	ipc.writeLvar("GSX_AUTO_SERVICE_STATE", GSX_AUTO_SERVICE_STATE)
end


----------------------------------
-- Service/Cycle State

function GSX_AUTO_SYNC_CYCLE()
	local fuel_state = ipc.readLvar("FSDT_GSX_REFUELING_STATE")
	local cater_state = ipc.readLvar("FSDT_GSX_CATERING_STATE")
	local board_state = ipc.readLvar("FSDT_GSX_BOARDING_STATE")
	local deboard_state = ipc.readLvar("FSDT_GSX_DEBOARDING_STATE")
	local depart_state = ipc.readLvar("FSDT_GSX_DEPARTURE_STATE")
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

	if GSX_AUTO_SERVICE_STATE == 0 and (fuel_state >= 5 or board_state >= 4 ) then
		GSX_AUTO_SERVICE_STATE = 1
		ipc.log("GSX_AUTO: Service State switched from Refuel to Cater")
	elseif GSX_AUTO_SERVICE_STATE == 1 and (cater_state >= 5 or board_state >= 4 )then
		GSX_AUTO_SERVICE_STATE = 2
		ipc.log("GSX_AUTO: Service State switched from Cater to Board")
	elseif GSX_AUTO_SERVICE_STATE == 2 and board_state == 6 then
		GSX_AUTO_SERVICE_STATE = 3
		ipc.log("GSX_AUTO: Service State switched from Bord to Push")
	elseif GSX_AUTO_SERVICE_STATE == 3 and depart_state == 6 then
		GSX_AUTO_SERVICE_STATE = 4
		ipc.log("GSX_AUTO: Service State switched from Push to Taxi Out")
	elseif onGnd ~= 1 and GSX_AUTO_SERVICE_STATE ~= 5 then
		GSX_AUTO_SERVICE_STATE = 5
		ipc.log("GSX_AUTO: Service State switched to Flight")
	elseif GSX_AUTO_SERVICE_STATE == 5 and onGnd == 1 then
		GSX_AUTO_SERVICE_STATE = 6
		ipc.log("GSX_AUTO: Service State switched from Flight to Taxi In")
	elseif GSX_AUTO_SERVICE_STATE == 6 and ipc.readLvar("FSDT_VAR_EnginesStopped") == 1 then
		GSX_AUTO_SERVICE_STATE = 7
		ipc.log("GSX_AUTO: Service State switched from Taxi In to Deboard")
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

	-- REQUESTS / CONNECTED STATE
	if ipc.readLvar("GSX_AUTO_CONNECT_REQUESTED") ~= 0 then
		if GSX_AUTO_CONNECTED == 0 then
			ipc.log("GSX_AUTO: Connect Request received")
			GSX_AUTO_CONNECT(true)
		else
			ipc.log("GSX_AUTO: Disconnect Request received")
			GSX_AUTO_CONNECT()
		end
		ipc.writeLvar("GSX_AUTO_CONNECT_REQUESTED", 0)
		if ipc.readLvar("GSX_AUTO_DEBOARD_REQUESTED") ~= 0 then
			ipc.sleep(1000)
		end
	end

	if ipc.readLvar("GSX_AUTO_DEBOARD_REQUESTED") ~= 0 then
		ipc.log("GSX_AUTO: Deboard Request received")
		GSX_AUTO_DEBOARD()
		ipc.writeLvar("GSX_AUTO_DEBOARD_REQUESTED", 0)
	end

	if ipc.readLvar("FSDT_GSX_JETWAY") == 5 or ipc.readLvar("FSDT_GSX_GPU_STATE") == 5 then
		GSX_AUTO_CONNECTED = 1
		ipc.writeLvar("GSX_AUTO_CONNECTED", GSX_AUTO_CONNECTED)
	else
		GSX_AUTO_CONNECTED = 0
		ipc.writeLvar("GSX_AUTO_CONNECTED", GSX_AUTO_CONNECTED)
	end

	-- OFFSETS
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
-- Call/Flag Functions

function GSX_AUTO_MENU(sleep)
	ipc.keypress(123,11)
	ipc.sleep(sleep or 1000)
end

function GSX_AUTO_KEY(key)
	if (key ~= 0) then
		ipc.control(67136 + (key - 1))
		ipc.sleep(500)
	else
		ipc.control(67145)
		ipc.sleep(500)
	end
end

function GSX_AUTO_CONNECT(connect)
	if connect then	--Open/Close GSX Menu so that it sets the Variables correctly ...
		GSX_AUTO_MENU(1000)
		GSX_AUTO_KEY(8)
		GSX_AUTO_MENU(500)
	end
	local gsxJetway = ipc.readLvar("FSDT_GSX_JETWAY")
	if gsxJetway ~= 2 then
		ipc.log("GSX_AUTO_CONNECT: Dis/Connect Jetway")
		if ipc.readLvar("GSX_AUTO_NUM_DOORS") ~= 1 then
			GSX_AUTO_JETWAY_TOGGLE2()
		else
			GSX_AUTO_JETWAY_TOGGLE1()
		end
	else
		ipc.log("GSX_AUTO_CONNECT: Dis/Connect GPU")
		GSX_AUTO_GPU()
	end
	ipc.sleep(500)
end

function GSX_AUTO_JETWAY_TOGGLE2()
	GSX_AUTO_MENU(1500)
	GSX_AUTO_KEY(6)
	if (GSX_AUTO_CONNECTED == 0 and GSX_AUTO_SERVICE_STATE == 0) then
		ipc.sleep(delayOperator)
	else
		ipc.sleep(500)
	end
	GSX_AUTO_KEY(1)
	GSX_AUTO_KEY(2)
	GSX_AUTO_KEY(3)
end

function GSX_AUTO_JETWAY_TOGGLE1()
	GSX_AUTO_MENU(1500)
	GSX_AUTO_KEY(6)
	if (GSX_AUTO_CONNECTED == 0 and GSX_AUTO_SERVICE_STATE == 0) then
		ipc.sleep(delayOperator)
	else
		ipc.sleep(500)
	end
	GSX_AUTO_KEY(1)
	GSX_AUTO_KEY(3)
	GSX_AUTO_KEY(2)
end

function GSX_AUTO_GPU()
	GSX_AUTO_MENU()
	GSX_AUTO_KEY(8)
	ipc.sleep(500)
	GSX_AUTO_KEY(1)
	if (GSX_AUTO_CONNECTED == 0 and GSX_AUTO_SERVICE_STATE == 0) then
		ipc.sleep(delayOperator)
	end
end

function GSX_AUTO_PUSH()
	if ipc.readLvar("FSDT_GSX_JETWAY") == 5 or ipc.readLvar("FSDT_GSX_GPU_STATE") == 5 then
		ipc.log("GSX_AUTO_PUSH: Jetway/GPU in Place, request Disconnect")
		GSX_AUTO_CONNECT()
	end

	local depature_state = ipc.readLvar("FSDT_GSX_DEPARTURE_STATE")
	if depature_state == 1 then
		if PLDFSL == nil then
			GSX_AUTO_MENU()
			GSX_AUTO_KEY(5)
		else
			PLDFSL.PED_COMM_1_INT_RAD_Switch("INT")
			ipc.sleep(1000)
			PLDFSL.PED_COMM_1_INT_RAD_Switch("OFF")
		end
		ipc.log("GSX_AUTO_PUSH: Push requested")
	elseif depature_state == 5 then
		if PLDFSL == nil then
			GSX_AUTO_PUSH_CONFIRM()
		else
			PLDFSL.PED_COMM_1_INT_RAD_Switch("INT")
			ipc.sleep(1000)
			PLDFSL.PED_COMM_1_INT_RAD_Switch("OFF")
		end
		ipc.log("GSX_AUTO_PUSH: Good-Start confirmed")
	end
end

function GSX_AUTO_PUSH_CONFIRM()
	GSX_AUTO_MENU()
	GSX_AUTO_KEY(1)
end

function GSX_AUTO_DEBOARD()
	if GSX_AUTO_CONNECTED == 0 then
		GSX_AUTO_DEBOARDCONNECT()
	else
		GSX_AUTO_MENU()
		ipc.sleep(500)
		GSX_AUTO_KEY(1)
		ipc.sleep(500)
	end
end

function GSX_AUTO_DEBOARDCONNECT()
	--let GSX set the Variables
	GSX_AUTO_MENU(1000)
	GSX_AUTO_KEY(8)
	GSX_AUTO_MENU(500)
	
	--Call GPU when no Jetway avail
	local gsxJetway = ipc.readLvar("FSDT_GSX_JETWAY")
	if gsxJetway == 2 then
		ipc.log("GSX_AUTO_DEBOARDCONNECT: Call GPU")
		GSX_AUTO_GPU()
	end
	ipc.sleep(500)
	
	--Call Deboard and select Jetways if avail
	GSX_AUTO_MENU()
	ipc.sleep(500)
	GSX_AUTO_KEY(1)
	ipc.sleep(1000)
	if gsxJetway ~=2 then
		GSX_AUTO_KEY(1)
		GSX_AUTO_KEY(3)
		GSX_AUTO_KEY(2)
	end
end

function GSX_AUTO_CATER()
	if PLDFSL == nil then
		GSX_AUTO_MENU()
		GSX_AUTO_KEY(2)
	else
		PLDFSL:MCDU_AOC_MENU("R")
		PLDFSL:MCDU_ClickLSK("R", "R4")
		ipc.sleep(250)
		PLDFSL:MCDU_ClickLSK("R", "R6")
		ipc.sleep(250)
		PLDFSL:MCDU_ClickLSK("R", "R3")
		ipc.sleep(250)
		PLDFSL:MCDU_ClickLSK("R", "R6")
		ipc.sleep(250)
		PLDFSL:MCDU_ClickLSK("R", "L6")
		ipc.sleep(250)
	end
end

function GSX_AUTO_REFUEL()
		GSX_AUTO_MENU()
		GSX_AUTO_KEY(3)
end

function GSX_AUTO_BOARD()
	if PLDFSL == nil then
		GSX_AUTO_MENU()
		GSX_AUTO_KEY(4)
	else
		PLDFSL:MCDU_AOC_MENU("R")
		PLDFSL:MCDU_ClickLSK("R", "R4")
		ipc.sleep(250)
		PLDFSL:MCDU_ClickLSK("R", "R6")
		ipc.sleep(250)
		PLDFSL:MCDU_ClickLSK("R", "L3")
		ipc.sleep(250)
		PLDFSL:MCDU_ClickLSK("R", "R6")
		ipc.sleep(250)
		PLDFSL:MCDU_ClickLSK("R", "L6")
		ipc.sleep(250)
	end
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
	elseif GSX_AUTO_SERVICE_STATE == 7 then
		GSX_AUTO_DEBOARD()
	end
end


-----------------------------------------
-----------------------------------------
-- $$ EVENT FLAGS

event.flag(1, "GSX_AUTO_MENU")
event.flag(2, "GSX_AUTO_JETWAY_TOGGLE1")
event.flag(3, "GSX_AUTO_JETWAY_TOGGLE2")
event.flag(4, "GSX_AUTO_PUSH")
event.flag(5, "GSX_AUTO_DEBOARD")
event.flag(6, "GSX_AUTO_CATER")
event.flag(7, "GSX_AUTO_REFUEL")
event.flag(8, "GSX_AUTO_BOARD")
event.flag(9, "GSX_AUTO_CONNECT")
event.flag(10, "GSX_AUTO_SERVICE_CYCLE")

event.timer(3000, "GSX_AUTO_SYNC_CYCLE")
ipc.log("GSX Sync active - starting in State " .. GSX_AUTO_SERVICE_STATE)
