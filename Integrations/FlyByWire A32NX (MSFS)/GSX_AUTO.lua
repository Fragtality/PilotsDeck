---@diagnostic disable: undefined-global
-------------------------------------------------------------------------
-- CONFIGURATION
local GSXAUTO_CFG_PATH = "F:/Prepar3D/Addon Manager" --Path where you have installed GSX/Addon Manager Folder to. MUST use Forward-Slahes ( / ), MUST be as String (enclosed in " ) and MUST NOT end with a Slash

--To set these Automations per Plane, check the GSX_AUTO_LOAD_AC Function
local GSXAUTO_CFG_NOCREW = true 	                        --Disable Boarding and Deboarding of Crew
local GSXAUTO_CFG_AUTODEBOARD = true                        --Automatically call Deboard upon Arrival (Engines stopped and Beacon off) - for Plane-specific use GSX_AUTO_LOAD_AC
local GSXAUTO_CFG_AUTOSERVICES = true                       --Automatically call Refuel + Catering (together) and Boarding later 
local GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER = false      --Start Boarding after Catering is finished (will take Precendence over BOARDAFTERREFUEL)
local GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL = true      --Start Boarding after Refuel is finished (if both are false, Boarding is called as soon as the Fuel Hose is connected)
local GSXAUTO_CFG_REMOVESTAIRS_AFTERBOARD = true            --Remove Stairs (only) after Boarding is completed
local GSXAUTO_CFG_AUTOSERVICES_WAITTURNAROUND = 120         --Wait 60s (120 * 500ms) before calling Refuel on Turn-Around
local GSXAUTO_CFG_AUTOSERVICES_REFRESH_SB = true            --Refresh SimBrief FP in GSX on Turn-Around
local GSXAUTO_CFG_SKIP_FOLLOWME = true                      --Skip the FollowMe Question automatically with 'No'
local GSXAUTO_CFG_SELECT_OPERATOR = true                    --Skip/Answer the Operator Selection by selecting the '[GSX Choice]' in the Menu
local GSXAUTO_CFG_AUTOUNFREEZE = false                      --Automatically unfreezes the Plane after GSX Push-Back if it should still be frozen after it was completed

local GSXAUTO_CFG_OFFSETS = true		--Write Offsets for display on PilotsDeck/StreamDeck
local GSXAUTO_CFG_OFFSETBASE = 0x4300   --First Offset to use (Scripts needs 558 Bytes)
local GSXAUTO_CFG_STRSIZE = 48          --String-Length / Offset-Size for each Menu-Entry

-------------------------------------------------------------------------
local GSXAUTO_CFG_MENUFILE = GSXAUTO_CFG_PATH .. "/MSFS/fsdreamteam-gsx-pro/html_ui/InGamePanels/FSDT_GSX_Panel/menu"
local GSXAUTO_OFFSET_PAX = GSXAUTO_CFG_OFFSETBASE + (11 * GSXAUTO_CFG_STRSIZE) 	--String, Length 5
local GSXAUTO_OFFSET_CARGO = GSXAUTO_OFFSET_PAX + 6 		                    --String, Length 6
local GSXAUTO_OFFSET_STATE = GSXAUTO_OFFSET_CARGO + 7 		                    --String, Length 16
ipc.log("Pax-String at Offset 0x" .. string.upper(string.format("%x", GSXAUTO_OFFSET_PAX)) .. ":5:s")
ipc.log("Cargo-String at Offset 0x" .. string.upper(string.format("%x", GSXAUTO_OFFSET_CARGO)) .. ":6:s")
ipc.log("State-String at Offset 0x" .. string.upper(string.format("%x", GSXAUTO_OFFSET_STATE)) .. ":16:s")

function GSXAUTO_DISABLE_CREW()
    if GSXAUTO_CFG_NOCREW then
        ipc.log("GSX_AUTO - Disabling Crew De/Boarding")
        ipc.writeLvar("FSDT_GSX_CREW_NOT_DEBOARDING", 1)
        ipc.writeLvar("FSDT_GSX_CREW_NOT_BOARDING", 1)
        ipc.writeLvar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1)
        ipc.writeLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1)
        ipc.writeLvar("FSDT_GSX_NUMCREW", 0)
        ipc.writeLvar("FSDT_GSX_NUMPILOTS", 0)
        ipc.writeLvar("FSDT_GSX_CREW_ON_BOARD", 1)
    end
end

----------------------------------
-- AC Handling
local GSXAUTO_AIRCRAFT_ISCARGO = false
local GSXAUTO_AIRCRAFT_CUSTOM = false
local GSXAUTO_AIRCRAFT = ipc.readSTR(0x3C00,256)
ipc.log(GSXAUTO_AIRCRAFT)

function GSXAUTO_AIRCRAFT_CONTAINS(pattern)
    ipc.log("comparing '" .. pattern .. "' with '" .. GSXAUTO_AIRCRAFT .. "'")
    if string.find(GSXAUTO_AIRCRAFT, pattern, 0, true) then
        ipc.log("match")
        return true
    else
        ipc.log("no match")
        return false
    end
end

function GSX_AUTO_LOAD_AC()
    if GSXAUTO_AIRCRAFT_CONTAINS("fnx320") then
        ipc.log("GSX_AUTO: Disabling Auto-Services for Fenix A320")
        GSXAUTO_CFG_NOCREW = false
        GSXAUTO_CFG_AUTODEBOARD = false
        GSXAUTO_CFG_AUTOSERVICES = false
        GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER = false
        GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL = false
        GSXAUTO_CFG_REMOVESTAIRS_AFTERBOARD = false
        GSXAUTO_CFG_SKIP_FOLLOWME = false
        GSXAUTO_CFG_SELECT_OPERATOR = false
        GSXAUTO_AIRCRAFT_ISCARGO = false
        GSXAUTO_CFG_AUTOSERVICES_REFRESH_SB = false
    end

    if GSXAUTO_AIRCRAFT_CONTAINS("787") then
        ipc.log("GSX_AUTO: Enabling Auto-Services for WorkingTitle 787")
        GSXAUTO_CFG_NOCREW = true
        GSXAUTO_CFG_AUTODEBOARD = true
        GSXAUTO_CFG_AUTOSERVICES = true
        GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER = false
        GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL = true
        GSXAUTO_CFG_REMOVESTAIRS_AFTERBOARD = true
        GSXAUTO_CFG_SKIP_FOLLOWME = true
        GSXAUTO_CFG_SELECT_OPERATOR = true
        GSXAUTO_AIRCRAFT_ISCARGO = false
        GSXAUTO_CFG_AUTOSERVICES_REFRESH_SB = true
    end

    if GSXAUTO_AIRCRAFT_CONTAINS("FlyByWire") then
        ipc.log("GSX_AUTO: Enabling Auto-Services for FlyByWire A32NX")
        GSXAUTO_CFG_NOCREW = true
        GSXAUTO_CFG_AUTODEBOARD = true
        GSXAUTO_CFG_AUTOSERVICES = true
        GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER = false
        GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL = true
        GSXAUTO_CFG_REMOVESTAIRS_AFTERBOARD = true
        GSXAUTO_CFG_SKIP_FOLLOWME = true
        GSXAUTO_CFG_SELECT_OPERATOR = true
        GSXAUTO_AIRCRAFT_ISCARGO = false
        GSXAUTO_CFG_AUTOSERVICES_REFRESH_SB = true
    end

	if GSXAUTO_AIRCRAFT_CONTAINS("inibuilds-aircraft-a30") then
		require "GSX_AUTO_INI300"
		GSXAUTO_AIRCRAFT_CUSTOM = true

        GSXAUTO_CFG_NOCREW = true
        GSXAUTO_CFG_AUTODEBOARD = true
        GSXAUTO_CFG_AUTOSERVICES = true
        GSXAUTO_CFG_SKIP_FOLLOWME = true
        GSXAUTO_CFG_SELECT_OPERATOR = true
        GSXAUTO_CFG_REMOVESTAIRS_AFTERBOARD = true
        if GSXAUTO_AIRCRAFT_CONTAINS("a306f") then
            GSXAUTO_AIRCRAFT_ISCARGO = true
            GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER = false
            GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL = false
            ipc.log("GSX_AUTO: Using custom Aircraft Handling for iniBuilds A300F")
        else
            GSXAUTO_AIRCRAFT_ISCARGO = false
            GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER = false
            GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL = true
            ipc.log("GSX_AUTO: Using custom Aircraft Handling for iniBuilds A300R")
        end
	end
end

GSX_AUTO_LOAD_AC()

--------------------------------------
-- MAIN LOOP
local GSXAUTO_STARTUP = true
local GSXAUTO_STATE = 0
ipc.createLvar("GSXAUTO_STATE", GSXAUTO_STATE)
local GSXAUTO_STATE_STR = "Call Refuel"
local GSXAUTO_STATE_CALLED = false
local GSXAUTO_CONNECTED = 0
local GSXAUTO_DEBOARD_FINISHED = false
local GSXAUTO_DEBOARD_DELAY = 0
local GSXAUTO_REFUEL_DELAY = 0
local GSXAUTO_REFUEL_HOSEWASCONNECTED = false
local GSXAUTO_IS_TURNAROUND = false
local GSXAUTO_MENU_LANDDELAY = 0
local GSXAUTO_MENU_TITLE = ""
local GSXAUTO_MENU_FIRSTLINE = ""
local GSXAUTO_MENU_ISREADY = false
local GSXAUTO_MENU_ISMAIN = true
local GSXAUTO_MENU_DELAYCOUNTER = 0
local GSXAUTO_MENU_NUMPRESSED = false
local GSXAUTO_MENU_QUEUED = false
local GSXAUTO_MENU_NOTREADYCOUNTER = 0
local GSXAUTO_MENU_GSXCHOICE = -1
local GSXAUTO_MENU_CHOICENAME = ""
local GSXAUTO_PUSH_CONFIRMED = false
local GSXAUTO_PUSH_STAND_DEICE = false
local GSXAUTO_PUSH_REQUESTED = false
local GSXAUTO_PUSH_MENUOPENING = false
local GSXAUTO_PUSH_LASTBRAKES = 32767
local GSXAUTO_DEICE_SELECTED = false
local GSXAUTO_CALLED_FUELCATER = false
local GSXAUTO_CALLED_BOARD = false
local GSXAUTO_CALLED_DEBOARD = false
local GSXAUTO_SKIPPED_FOLLOWME = false
local GSXAUTO_SKIPPED_OPERATOR = false
ipc.createLvar("GSXAUTO_CONNECTED", GSXAUTO_CONNECTED)
ipc.createLvar("GSXAUTO_REPO_CONNECT", 0)

function GSXAUTO_MAIN_LOOP()
    local couatlStarted = ipc.readLvar("FSDT_GSX_COUATL_STARTED") == 1
    if not couatlStarted then
		return
	end

    if ipc.readSTR(0x3C00,256) ~= GSXAUTO_AIRCRAFT then
		GSX_AUTO_LOAD_AC()
	end

    GSXAUTO_MENU_READ()
    GSXAUTO_MENU_RUN()
    local board_state = ipc.readLvar("FSDT_GSX_BOARDING_STATE")
	local deboard_state = ipc.readLvar("FSDT_GSX_DEBOARDING_STATE")
    GSXAUTO_STATE_EVAL(board_state, deboard_state)
    GSXAUTO_OFFSET_UPDATE(board_state, deboard_state)

    if GSXAUTO_STARTUP then
        ipc.log("GSX_AUTO - Refreshing Menu (Startup)")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_STARTUP = false
    end

    if (GSXAUTO_MENU_NUMPRESSED and GSXAUTO_MENU_ISREADY) or (GSXAUTO_MENU_NUMPRESSED and GSXAUTO_PUSH_MENUOPENING) then
        GSXAUTO_MENU_NUMPRESSED = false
        GSXAUTO_MENU_NOTREADYCOUNTER = 0
    elseif GSXAUTO_MENU_NUMPRESSED and not GSXAUTO_MENU_ISREADY then
        GSXAUTO_MENU_NOTREADYCOUNTER = GSXAUTO_MENU_NOTREADYCOUNTER + 1
    end

    if GSXAUTO_MENU_NOTREADYCOUNTER >= 5 then
        GSXAUTO_MENU_NOTREADYCOUNTER = 0
        GSXAUTO_MENU_NUMPRESSED = false
        ipc.log("GSX_AUTO - Refreshing Menu (Num pressed)")
        GSXAUTO_MENU_QUEUE_OPEN()
    end
end

--------------------------------------
-- STATE LOGIC
function STARTSWITH(str, start)
    return str:find('^' .. start) ~= nil
end

function GSXAUTO_STATE_EVAL (board_state, deboard_state)
    local fuel_state = ipc.readLvar("FSDT_GSX_REFUELING_STATE")
	local cater_state = ipc.readLvar("FSDT_GSX_CATERING_STATE")
	local depart_state = ipc.readLvar("FSDT_GSX_DEPARTURE_STATE")
    local deice_state = ipc.readLvar("FSDT_GSX_DEICING_STATE")
	local enginesStopped = ipc.readLvar("FSDT_VAR_EnginesStopped") == 1
	local onGround = ipc.readSW(0x0366) == 1
    local result = ""

	-- SERVICE STATE
	-- 0 => Refuel
	-- 1 => Cater
	-- 2 => Board
	-- 3 => Push
	-- 4 => Taxi Out
    -- 5 => Remote De-Ice
	-- 6 => Flight
	-- 7 => Taxi In
	-- 8 => Deboard

    --Active Service
    if (GSXAUTO_SERVICE_ISRUNNING(deboard_state) or GSXAUTO_SERVICE_ISCALLED(deboard_state)) and not GSXAUTO_DEBOARD_FINISHED then
        GSXAUTO_STATE_CALLED = GSXAUTO_STATE == 8
        if not GSXAUTO_AIRCRAFT_ISCARGO then
            result = "Deboarding\n..."
        else
            result = "Unloading\n..."
        end
    elseif GSXAUTO_SERVICE_ISRUNNING(board_state) or GSXAUTO_SERVICE_ISCALLED(board_state) then
        GSXAUTO_STATE_CALLED = GSXAUTO_STATE == 2
        if not GSXAUTO_AIRCRAFT_ISCARGO then
            result = "Boarding\n..."
        else
            result = "Loading\n..."
        end
    elseif GSXAUTO_SERVICE_ISRUNNING(deice_state) or GSXAUTO_SERVICE_ISCALLED(deice_state) then
        if GSXAUTO_STATE ~= 5 and GSXAUTO_STATE ~= 3 then
            ipc.log("GSX_AUTO - Overriding State to Remote De-Ice (5)!")
            GSXAUTO_STATE = 5
        end
        if GSXAUTO_STATE == 3 then
            GSXAUTO_PUSH_STAND_DEICE = true
        end
        GSXAUTO_STATE_CALLED = true
        result = "De-Icing\n..."
    elseif GSXAUTO_SERVICE_ISCALLED(depart_state) then
        if GSXAUTO_STATE ~= 3 then
            ipc.log("GSX_AUTO - Overriding State to Push-Back (3)!")
            GSXAUTO_STATE = 3
            GSXAUTO_PUSH_STAND_DEICE = false
            GSXAUTO_PUSH_LASTBRAKES = 32767
        end

        GSXAUTO_PUSH_MENUOPENING = false
        GSXAUTO_STATE_CALLED = true
        GSXAUTO_PUSH_CONFIRMED = false
        GSXAUTO_PUSH_REQUESTED = true

        if STARTSWITH(GSXAUTO_MENU_TITLE, "Do you want to request") then
            result = "Start\nPush?"
        elseif STARTSWITH(GSXAUTO_MENU_TITLE, "Ice warning") then
            result = "De-Icing?"
        elseif STARTSWITH(GSXAUTO_MENU_TITLE, "Select pushback direction") then
            result = "Direction?"
        elseif deice_state >= 4 then
            result = "De-Icing\n..."
        elseif GSXAUTO_PUSH_STAND_DEICE and deice_state <= 1 then
            GSXAUTO_PUSH_STAND_DEICE = false
            ipc.log("GSX_AUTO - Refreshing Menu (After Deice)")
            GSXAUTO_MENU_QUEUE_OPEN()
        else
            result = "Connecting\n..."
        end
    elseif GSXAUTO_SERVICE_ISRUNNING(depart_state) then
        GSXAUTO_STATE_CALLED = false
        GSXAUTO_PUSH_MENUOPENING = false
        local brakes = ipc.readUW(0x0BC8)

        if STARTSWITH(GSXAUTO_MENU_TITLE, "Select pushback direction") then
            result = "Direction?"
        elseif not GSXAUTO_PUSH_CONFIRMED then
            result = "Confirm / Stop"
            if brakes ~= GSXAUTO_PUSH_LASTBRAKES then
                ipc.log("GSX_AUTO - Refreshing Menu (Parking Brake changed)")
                GSXAUTO_MENU_QUEUE_OPEN()
            end
        elseif GSXAUTO_PUSH_CONFIRMED then
            result = "Unlocking\n..."
            GSXAUTO_PUSH_REQUESTED = false
        end

        GSXAUTO_PUSH_LASTBRAKES = brakes
    elseif GSXAUTO_PUSH_REQUESTED and depart_state == 1 and result == "" and not GSXAUTO_PUSH_MENUOPENING then
        if STARTSWITH(GSXAUTO_MENU_TITLE, "Activate Services") then
            ipc.log("GSX_AUTO - Reopening Push-Back Menu!")
            GSXAUTO_MENU_QUEUE_OPEN()
            GSXAUTO_MENU_QUEUE_NUM(5)
            GSXAUTO_PUSH_MENUOPENING = true
        elseif not GSXAUTO_MENU_QUEUED then
            ipc.log("GSX_AUTO - Refreshing Menu (Push-Back)")
            GSXAUTO_MENU_QUEUE_OPEN()
        end
    elseif GSXAUTO_SERVICE_ISRUNNING(fuel_state) or GSXAUTO_SERVICE_ISCALLED(fuel_state) then
        if GSXAUTO_STATE == 8 then
            ipc.log("GSX_AUTO - Overriding State to Refuel (0)!")
            GSXAUTO_STATE = 0
        end
        GSXAUTO_STATE_CALLED = GSXAUTO_STATE == 0
        local hose = ipc.readLvar("FSDT_GSX_FUELHOSE_CONNECTED")
        if GSXAUTO_SERVICE_ISCALLED(fuel_state) then
            result = "Truck\nincoming"
        elseif GSXAUTO_SERVICE_ISRUNNING(fuel_state) and hose == 0 then
            result = "Connecting\nHose"
        else
            result = "Refueling\n..."
        end
    elseif GSXAUTO_SERVICE_ISRUNNING(cater_state) or GSXAUTO_SERVICE_ISCALLED(cater_state) then
        GSXAUTO_STATE_CALLED = GSXAUTO_STATE == 1
        result = "Catering\n..."
    end


    --Advance States
    if GSXAUTO_STATE == 0 and GSXAUTO_SERVICE_ISCALLED(fuel_state) then
        GSXAUTO_REFUEL_DELAY = 0
        GSXAUTO_IS_TURNAROUND = false
        GSXAUTO_STATE = 1
        ipc.log("GSX_AUTO - State switch - Refuel is called - now in State Catering (1)")
    elseif GSXAUTO_STATE < 2 and GSXAUTO_SERVICE_ISFINISHED(cater_state) then
        GSXAUTO_STATE = 2
        ipc.log("GSX_AUTO - State switch - Catering is finished - now in State Boarding (2)")
    elseif (GSXAUTO_STATE == 1 or GSXAUTO_STATE == 2) and GSXAUTO_SERVICE_ISFINISHED(board_state) then
        GSXAUTO_STATE = 3
        ipc.log("GSX_AUTO - State switch - Boarding is finished - now in State Push-Back (3)")
        if GSXAUTO_CFG_REMOVESTAIRS_AFTERBOARD then
            GSXAUTO_CALL_STAIRS()
        end
    elseif GSXAUTO_STATE == 2 and GSXAUTO_SERVICE_ISFINISHED(board_state) then
        GSXAUTO_STATE = 3
        ipc.log("GSX_AUTO - State switch - Boarding is finished - now in State Push-Back (3)")
    elseif GSXAUTO_STATE == 3 and GSXAUTO_SERVICE_ISFINISHED(depart_state) then
        GSXAUTO_STATE = 4
        GSXAUTO_STATE_CALLED = true
        result = "Taxi-Out"
        GSXAUTO_PUSH_REQUESTED = false
        ipc.log("GSX_AUTO - State switch - Push-Back is complete - now in State Taxi-Out (4)")
        ipc.log("GSX_AUTO - Refreshing Menu (Taxi-Out)")
        GSXAUTO_MENU_QUEUE_OPEN()
        if ipc.readLvar("FSDT_VAR_Frozen") == 1 and GSXAUTO_CFG_AUTOUNFREEZE then
            ipc.log("GSX_AUTO - Plane still frozen in Taxi-Out State - automatic unfreeze")
            GSXAUTO_DO_UNFREEZE()
        end
    elseif GSXAUTO_STATE == 5 and GSXAUTO_SERVICE_ISFINISHED(deice_state) then
        GSXAUTO_STATE = 4
        GSXAUTO_STATE_CALLED = true
        result = "Taxi-Out"
        GSXAUTO_PUSH_REQUESTED = false
        GSXAUTO_DEICE_SELECTED = false
        ipc.log("GSX_AUTO - State switch - De-Icing is complete - now in State Taxi-Out (4)")
        ipc.log("GSX_AUTO - Refreshing Menu (Taxi-Out)")
        GSXAUTO_MENU_QUEUE_OPEN()
        if ipc.readLvar("FSDT_VAR_Frozen") == 1 and GSXAUTO_CFG_AUTOUNFREEZE then
            ipc.log("GSX_AUTO - Plane still frozen in Taxi-Out State - automatic unfreeze")
            GSXAUTO_DO_UNFREEZE()
        end
    elseif GSXAUTO_STATE <= 4 and not onGround then
        GSXAUTO_STATE = 6
        result = "Flight"
        GSXAUTO_STATE_CALLED = true
        GSXAUTO_PUSH_REQUESTED = false
        GSXAUTO_PUSH_STAND_DEICE = false
        GSXAUTO_PUSH_CONFIRMED = false
        GSXAUTO_PUSH_MENUOPENING = false
        GSXAUTO_CALLED_FUELCATER = false
        GSXAUTO_CALLED_BOARD = false
        GSXAUTO_CALLED_DEBOARD = false
        GSXAUTO_SKIPPED_FOLLOWME = false
        GSXAUTO_SKIPPED_OPERATOR = false
        ipc.log("GSX_AUTO - State switch - Take-Off - now in State Flight (6)")
    elseif GSXAUTO_STATE == 6 and onGround then
        GSXAUTO_STATE = 7
        result = "Taxi-In"
        GSXAUTO_STATE_CALLED = true
        GSXAUTO_MENU_LANDDELAY = 60
        ipc.log("GSX_AUTO - State switch - Touch-Down - now in State Taxi-In (7)")
    elseif GSXAUTO_STATE == 7 and not enginesStopped then
        if GSXAUTO_CFG_SKIP_FOLLOWME and STARTSWITH(GSXAUTO_MENU_TITLE, "Request FollowMe") and not GSXAUTO_SKIPPED_FOLLOWME then
            ipc.log("GSX_AUTO - Skipping FollowMe")
            GSXAUTO_MENU_QUEUE_NUM(2)
            GSXAUTO_SKIPPED_FOLLOWME = true
        elseif STARTSWITH(GSXAUTO_MENU_TITLE, "Select handling operator") and not GSXAUTO_SKIPPED_OPERATOR then
            GSXAUTO_MENU_QUEUE_OPERATOR()
            GSXAUTO_SKIPPED_OPERATOR = true
        end
    elseif GSXAUTO_STATE == 7 and enginesStopped then
        GSXAUTO_STATE = 8
        GSXAUTO_DEBOARD_FINISHED = false
        GSXAUTO_DEBOARD_DELAY = 0
        GSXAUTO_STATE_CALLED = false
        GSXAUTO_SKIPPED_OPERATOR = true
        GSXAUTO_SKIPPED_FOLLOWME = true
        ipc.log("GSX_AUTO - State switch - Engines are stopped - now in State Deboarding (8)")
        GSXAUTO_CHECK_AUTODEBOARD()
    elseif GSXAUTO_STATE == 8 and GSXAUTO_CFG_AUTODEBOARD and not GSXAUTO_CALLED_DEBOARD then
        GSXAUTO_CHECK_AUTODEBOARD()
    elseif GSXAUTO_STATE == 8 and GSXAUTO_DEBOARD_DELAY > 30 and (GSXAUTO_DEBOARD_FINISHED or GSXAUTO_SERVICE_ISFINISHED(deboard_state)) then
        GSXAUTO_IS_TURNAROUND = true
        GSXAUTO_DEBOARD_FINISHED = false
        GSXAUTO_REFUEL_HOSEWASCONNECTED = false
        GSXAUTO_CALLED_BOARD = false
        GSXAUTO_CALLED_DEBOARD = false
        GSXAUTO_CALLED_FUELCATER = false
        GSXAUTO_STATE = 0
        result = "Call Refuel"
        GSXAUTO_REFUEL_DELAY = GSXAUTO_CFG_AUTOSERVICES_WAITTURNAROUND
        ipc.log("GSX_AUTO - State switch - Deboarding is finished - now in State Refueling (0)")
    end

    if GSXAUTO_STATE == 8 then
        GSXAUTO_DEBOARD_DELAY = GSXAUTO_DEBOARD_DELAY + 1
    end

    --Determine Call String and State
    if result == "" then
        if GSXAUTO_STATE == 0 then
            result = "Call Refuel"
            GSXAUTO_STATE_CALLED = false
        elseif GSXAUTO_STATE == 1 then
            result = "Call Catering"
            GSXAUTO_STATE_CALLED = false
        elseif GSXAUTO_STATE == 2 then
            if not GSXAUTO_AIRCRAFT_ISCARGO then
                result = "Start Boarding"
            else
                result = "Start Loading"
            end
            GSXAUTO_STATE_CALLED = false
        elseif GSXAUTO_STATE == 3 then
            result = "Start\nPush-Back"
            GSXAUTO_STATE_CALLED = false
        elseif GSXAUTO_STATE == 4 then
            result = "Taxi-Out"
            if STARTSWITH(GSXAUTO_MENU_TITLE, "Change parking or") then
                local match = string.match(GSXAUTO_MENU_FIRSTLINE,"%[(.*)%]")
                if match ~= nil then
                    result = string.sub(match, 1, GSXAUTO_CFG_STRSIZE)
                    if not GSXAUTO_DEICE_SELECTED then
                        ipc.log("GSX_AUTO - Remote De-Ice was selected")
                        GSXAUTO_DEICE_SELECTED = true
                        GSXAUTO_STATE_CALLED = false
                    end
                end
            elseif not GSXAUTO_DEICE_SELECTED then
                GSXAUTO_STATE_CALLED = true
            end
        elseif GSXAUTO_STATE == 5 then
            result = "De-Ice"
        elseif GSXAUTO_STATE == 6 then
            result = "Flight"
            GSXAUTO_STATE_CALLED = true
        elseif GSXAUTO_STATE == 7 then
            result = "Taxi-In"
            if STARTSWITH(GSXAUTO_MENU_TITLE, "Change parking or") then
                local match = string.match(GSXAUTO_MENU_FIRSTLINE,"%[.*%s+(%a+%s*%d+).*%]")
                if match == nil then
                    match = string.match(GSXAUTO_MENU_FIRSTLINE,"%[%a* (%a*%s*%d+).*%]")
                end
                if match ~= nil then
                    match = string.gsub(match, "Gate ", "")
                    match = string.gsub(match, "Stand ", "")
                    result = result .. "\n" .. match
                end
            end
            GSXAUTO_STATE_CALLED = true
        elseif GSXAUTO_STATE == 8 then
            if not GSXAUTO_AIRCRAFT_ISCARGO then
                result = "Start Deboard"
            else
                result = "Start Unload"
            end
            GSXAUTO_STATE_CALLED = false
        end
    end

    if GSXAUTO_MENU_LANDDELAY > 0 then
        GSXAUTO_MENU_LANDDELAY = GSXAUTO_MENU_LANDDELAY - 1
        if GSXAUTO_MENU_LANDDELAY == 0 then
            ipc.log("GSX_AUTO - Refreshing Menu (Landed)")
            GSXAUTO_MENU_QUEUE_OPEN()
        end
    end

    GSXAUTO_STATE_STR = result

    if ipc.readLvar("GSXAUTO_REPO_CONNECT") == 1 then
        ipc.log("GSX_AUTO - Request for Reposition and Connect received")
        ipc.writeLvar("GSXAUTO_REPO_CONNECT", 0)
        GSXAUTO_CALL_REPOSITION()
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_CALL_CONNECT()
    end

    if GSXAUTO_CFG_AUTOSERVICES then
        GSXAUTO_SERVICE_AUTOMATIONS(fuel_state, cater_state, board_state)
    end

    if GSXAUTO_AIRCRAFT_CUSTOM then
        GSXAUTO_CUSTOM_DOORSYNC()
    end
end

function GSXAUTO_SERVICE_ISCALLED(gsxstate)
    return gsxstate == 4
end

function GSXAUTO_SERVICE_ISRUNNING(gsxstate)
    return gsxstate == 5
end

function GSXAUTO_SERVICE_ISACTIVE(gsxstate)
    return gsxstate == 4 or gsxstate == 5
end

function GSXAUTO_SERVICE_ISFINISHED(gsxstate)
    return gsxstate == 6
end

function GSXAUTO_SERVICE_ISCALLABLE(gsxstate)
    return gsxstate == 1
end

--------------------------------------
-- AUTOMATION
function GSXAUTO_CHECK_AUTODEBOARD()
    if not GSXAUTO_CALLED_DEBOARD and ((GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_IS_AUTO_DEBOARD() and not GSXAUTO_ISLIGHT_ON("beacon")) or (not GSXAUTO_ISLIGHT_ON("beacon") and GSXAUTO_CFG_AUTODEBOARD)) then
        ipc.log("GSX_AUTO - Call Connect/Deboard automatically")
        GSXAUTO_STATE_CALLED = true
        GSXAUTO_CALL_DEBOARD()
        GSXAUTO_CALLED_DEBOARD = true
    end
end

function GSXAUTO_ISLIGHT_ON(light)
    local value = ipc.readUW(0x0D0C)
    if light == "nav" then
        value = logic.And(value, 1)
    elseif light == "beacon" then
        value = logic.And(value, 2)
    else
        value = 0
    end

    return value ~= 0
end

function GSXAUTO_SERVICE_AUTOMATIONS(fuel_state, cater_state, board_state)
    if GSXAUTO_STATE == 0 and GSXAUTO_SERVICE_ISCALLABLE(fuel_state) and GSXAUTO_SERVICE_ISCALLABLE(cater_state) and GSXAUTO_CONNECTED == 1 and GSXAUTO_ISLIGHT_ON("nav") and not GSXAUTO_CALLED_FUELCATER then
        if GSXAUTO_REFUEL_DELAY > 0 then
            GSXAUTO_REFUEL_DELAY = GSXAUTO_REFUEL_DELAY - 1
        elseif GSXAUTO_REFUEL_DELAY == 0 then
            ipc.log("GSX_AUTO - Automatic Service Calls: Refuel and Catering")
            if fuel_state < 4 then
                GSXAUTO_CALL_REFUEL()
            else
                ipc.log("GSX_AUTO - Automatic Service Calls: Skipping Refuel - already running")
            end
            if cater_state < 4 then
                GSXAUTO_CALL_CATER()
            else
                ipc.log("GSX_AUTO - Automatic Service Calls: Skipping Cater - already running")
            end
            if GSXAUTO_IS_TURNAROUND and GSXAUTO_CFG_AUTOSERVICES_REFRESH_SB then
                GSXAUTO_MENU_QUEUE_OPERATOR()
                GSXAUTO_CALL_REFRESH_SB()
                GSXAUTO_MENU_QUEUE_OPEN()
            end
            GSXAUTO_CALLED_FUELCATER = true
        end
    end

    if fuel_state >= 4 and cater_state >= 4 and not GSXAUTO_CALLED_FUELCATER then
        ipc.log("GSX_AUTO - Automatic Service Calls: Skipping Refuel & Cater - already running")
        GSXAUTO_CALLED_FUELCATER = true
    end

    local hoseConnected = ipc.readLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1
    if fuel_state == 5 and not GSXAUTO_REFUEL_HOSEWASCONNECTED and hoseConnected then
        ipc.log("GSX_AUTO - Flag 'HOSEWASCONNECTED' is set")
        GSXAUTO_REFUEL_HOSEWASCONNECTED = true
    end

    if GSXAUTO_STATE >= 1 and GSXAUTO_STATE < 3 and GSXAUTO_SERVICE_ISCALLABLE(board_state) and not GSXAUTO_CALLED_BOARD and
       ((not GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER and not GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL and hoseConnected) or
       (GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER and GSXAUTO_SERVICE_ISFINISHED(cater_state)) or
       (GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL and GSXAUTO_REFUEL_HOSEWASCONNECTED and not hoseConnected)) then

        if not GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER and not GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL and hoseConnected then
            ipc.log("GSX_AUTO - Automatic Service Calls: Boarding (After Fuel-Hose connected)")
        end
        if GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERCATER and GSXAUTO_SERVICE_ISFINISHED(cater_state) then
            ipc.log("GSX_AUTO - Automatic Service Calls: Boarding (After Cater)")
        end
        if GSXAUTO_CFG_AUTOSERVICES_BOARDAFTERREFUEL and GSXAUTO_REFUEL_HOSEWASCONNECTED and not hoseConnected then
            ipc.log("GSX_AUTO - Automatic Service Calls: Boarding (After Refuel)")
        end

        GSXAUTO_CALL_BOARD()
        GSXAUTO_CALLED_BOARD = true
        GSXAUTO_REFUEL_HOSEWASCONNECTED = false
    end

    if board_state >= 4 and not GSXAUTO_CALLED_BOARD then
        ipc.log("GSX_AUTO - Automatic Service Calls: Skipping Boarding - already running")
        GSXAUTO_CALLED_BOARD = true
    end
end

--------------------------------------
-- OFFSETS

function GSXAUTO_OFFSET_UPDATE(board_state, deboard_state)
	local resultPax = ""
	local resultCargo = ""
    local jetway_state = ipc.readLvar("FSDT_GSX_JETWAY")
    local stairs_state = ipc.readLvar("FSDT_GSX_STAIRS")

	if GSXAUTO_SERVICE_ISACTIVE(board_state) or GSXAUTO_SERVICE_ISACTIVE(deboard_state) then
		local plnPax = ipc.readLvar("FSDT_GSX_NUMPASSENGERS")
		local brdPax = ipc.readLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL")
		local debrdPax = ipc.readLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL")
		local ldCargo = ipc.readLvar("FSDT_GSX_BOARDING_CARGO_PERCENT")
		local unldCargo = ipc.readLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT")

		if GSXAUTO_SERVICE_ISACTIVE(board_state) then
			resultPax = tostring(brdPax)
            if GSXAUTO_AIRCRAFT_ISCARGO then
                resultPax = ""
			elseif plnPax ~= brdPax and brdPax >= 0 then
				resultPax = resultPax .. " >"
            elseif brdPax < 0 then
                resultPax = ""
			end

			resultCargo = tostring(ldCargo) .. "%"
			if ldCargo ~= 100 then
				resultCargo = "< " .. resultCargo
			end
		end

		if GSXAUTO_SERVICE_ISACTIVE(deboard_state) then
			resultPax = tostring(plnPax - debrdPax)
			if GSXAUTO_AIRCRAFT_ISCARGO then
                resultPax = ""
            elseif plnPax ~= debrdPax and (plnPax - debrdPax) >= 0  then
				resultPax = "< " .. resultPax
            elseif (plnPax - debrdPax) < 0 then
                resultPax = ""
			end

			resultCargo = tostring(100 - unldCargo) .. "%"
			if unldCargo ~= 100 then
				resultCargo = resultCargo .. " >"
			end

            GSXAUTO_DEBOARD_FINISHED = (plnPax - debrdPax == 0) and (100 - unldCargo == 0)
		end
	end

    if GSXAUTO_CFG_OFFSETS then
        ipc.writeSTR(GSXAUTO_OFFSET_PAX, resultPax, 5)
        ipc.writeSTR(GSXAUTO_OFFSET_CARGO, resultCargo, 6)
        ipc.writeSTR(GSXAUTO_OFFSET_STATE, GSXAUTO_STATE_STR, 16)
    end
    ipc.writeLvar("GSXAUTO_STATE", GSXAUTO_STATE)

    if jetway_state == 5 or stairs_state == 5 then
        GSXAUTO_CONNECTED = 1
    else
        GSXAUTO_CONNECTED = 0
    end
    ipc.writeLvar("GSXAUTO_CONNECTED", GSXAUTO_CONNECTED)
end

--------------------------------------
-- MENU LOGIC
function GSXAUTO_MENU_QUEUE_OPEN()
    GSXAUTO_MENU_QUEUE_PUSH(-1)
end

function GSXAUTO_MENU_QUEUE_NUM(param)
    if param ~= 99 then
        ipc.log("GSX_AUTO - Input-Request for Menu Item #" .. param)
        GSXAUTO_MENU_QUEUE_PUSH(param - 1)
    end
end

function GSXAUTO_MENU_QUEUE_OPERATOR()
    if GSXAUTO_CFG_SELECT_OPERATOR then
        ipc.log("GSX_AUTO - Queue Operator Selection")
        GSXAUTO_MENU_QUEUE_PUSH(-2)
    end
end

function GSXAUTO_MENU_RUN()
    local selection = GSXAUTO_MENU_QUEUE_PEEK()
    if selection == nil then
        return
    end

    if selection == -1 then
        GSXAUTO_MENU_ISREADY = false
        ipc.log("GSX_AUTO - Opening GSX Menu")
        ipc.writeLvar("FSDT_GSX_MENU_OPEN", 1)
        GSXAUTO_MENU_QUEUED = true
        GSXAUTO_MENU_QUEUE_POP()
    elseif selection == -2 and GSXAUTO_MENU_DELAYCOUNTER >= 3 then
        if not GSXAUTO_MENU_ISMAIN then
            if GSXAUTO_MENU_GSXCHOICE >= 1 and GSXAUTO_MENU_GSXCHOICE <= 10 then
                ipc.log("GSX_AUTO - Selecting Operator '" .. GSXAUTO_MENU_CHOICENAME .. "'")
                ipc.log("GSX_AUTO - Selecting [" .. GSXAUTO_MENU_GSXCHOICE .. "] in GSX Menu")
                ipc.writeLvar("FSDT_GSX_MENU_CHOICE", GSXAUTO_MENU_GSXCHOICE - 1)
            else
                ipc.log("GSX_AUTO - Selecting first Operator (no GSX choice found)")
                ipc.log("GSX_AUTO - Selecting [1] in GSX Menu")
                ipc.writeLvar("FSDT_GSX_MENU_CHOICE", 0)
            end

            GSXAUTO_MENU_ISREADY = false
            GSXAUTO_MENU_QUEUE_OPEN()
        end
        GSXAUTO_MENU_DELAYCOUNTER = 0
        GSXAUTO_MENU_QUEUE_POP()
    elseif selection == -2 and GSXAUTO_MENU_DELAYCOUNTER < 3 then
        GSXAUTO_MENU_DELAYCOUNTER = GSXAUTO_MENU_DELAYCOUNTER + 1
    elseif selection >= 0 and GSXAUTO_MENU_ISREADY then
        ipc.log("GSX_AUTO - Selecting [" .. (selection + 1) .. "] in GSX Menu")
        ipc.writeLvar("FSDT_GSX_MENU_CHOICE", selection)
        GSXAUTO_MENU_NUMPRESSED = true
        GSXAUTO_MENU_ISREADY = false
        GSXAUTO_MENU_QUEUE_POP()
    end
end

function GSXAUTO_MENU_READ()
    local f = io.open(GSXAUTO_CFG_MENUFILE, "rb")
    if f then f:close() end
    if f == nil then return end

	local offset = GSXAUTO_CFG_OFFSETBASE
    local choicefound = false
	local numlines = 0
	for line in io.lines(GSXAUTO_CFG_MENUFILE) do
        if numlines == 0 then
            GSXAUTO_MENU_TITLE = line
            if STARTSWITH(line, "Activate Services at") then
                GSXAUTO_MENU_ISMAIN = true
            else
                GSXAUTO_MENU_ISMAIN = false
            end
        end
        if numlines == 1 then
            GSXAUTO_MENU_FIRSTLINE = line
        end

        local find, _ = string.find(line, "%[GSX choice%]")
        local lastchoice = GSXAUTO_MENU_GSXCHOICE
        if find ~= nil then
            GSXAUTO_MENU_GSXCHOICE = numlines
            GSXAUTO_MENU_CHOICENAME = string.sub(line, 0, find - 2)
            if lastchoice ~= GSXAUTO_MENU_GSXCHOICE then
                ipc.log("GSX_AUTO - Operator Choice from GSX: [" .. GSXAUTO_MENU_GSXCHOICE .. "] '" .. GSXAUTO_MENU_CHOICENAME .. "'")
            end
            choicefound = true
        end

    	local str = string.gsub(line, "[^%a%w%d -/()%[%]]", "")
		str = string.gsub(str, "[%(]", " (")
		ipc.writeSTR(offset, string.sub(str, 1, GSXAUTO_CFG_STRSIZE), GSXAUTO_CFG_STRSIZE)
		if GSXAUTO_STARTUP then
			ipc.log("GSX_AUTO - Menu-Line " .. numlines .. " at Offset 0x" .. string.upper(string.format("%x", offset)) .. ":48:s")
		end
		offset = offset + GSXAUTO_CFG_STRSIZE
		numlines = numlines + 1
	end

	while numlines <= 10 do
		ipc.writeSTR(offset, "", GSXAUTO_CFG_STRSIZE)
        if GSXAUTO_STARTUP then
			ipc.log("GSX_AUTO - Menu-Line " .. numlines .. " at Offset 0x" .. string.upper(string.format("%x", offset)) .. ":48:s")
		end
		offset = offset + GSXAUTO_CFG_STRSIZE
		numlines = numlines + 1
	end

    if not choicefound then
        GSXAUTO_MENU_GSXCHOICE = -1
        GSXAUTO_MENU_CHOICENAME = ""
    end
end

function GSXAUTO_MENU_EVENT()
    GSXAUTO_MENU_ISREADY = true
    GSXAUTO_MENU_QUEUED = false
    ipc.log("GSX_AUTO - Menu-Ready Event received")
end

--------------------------------------
-- MENU QUEUE
local GSXAUTO_MENU_QUEUE = {first = 0, last = -1}

function GSXAUTO_MENU_QUEUE_PUSH(value)
    local last = GSXAUTO_MENU_QUEUE.last + 1
    GSXAUTO_MENU_QUEUE.last = last
    GSXAUTO_MENU_QUEUE[last] = value
end

function GSXAUTO_MENU_QUEUE_PEEK()
    local first = GSXAUTO_MENU_QUEUE.first
    if first > GSXAUTO_MENU_QUEUE.last then return nil end
    return GSXAUTO_MENU_QUEUE[first]
end

function GSXAUTO_MENU_QUEUE_POP()
    local first = GSXAUTO_MENU_QUEUE.first
    if first > GSXAUTO_MENU_QUEUE.last then return nil end
    local value = GSXAUTO_MENU_QUEUE[first]
    GSXAUTO_MENU_QUEUE[first] = nil
    GSXAUTO_MENU_QUEUE.first = first + 1
    return value
end

--------------------------------------
-- SERVICE CALLS
function GSXAUTO_CALL_CONNECT_TGL()
    if GSXAUTO_CONNECTED == 1 then
        GSXAUTO_CALL_DISCONNECT()
    else
        GSXAUTO_CALL_CONNECT()
    end
end

function GSXAUTO_CALL_CONNECT()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_CONNECT() then

	else
        GSXAUTO_MENU_QUEUE_OPEN()
		local gsxJetway = ipc.readLvar("FSDT_GSX_JETWAY")
        local gsxJetwayOp = ipc.readLvar("FSDT_GSX_OPERATEJETWAYS_STATE")
		if gsxJetway == 1 and gsxJetwayOp < 3 then
            ipc.log("GSX_AUTO - Connect Jetway")
			GSXAUTO_MENU_QUEUE_NUM(6)
            GSXAUTO_MENU_QUEUE_OPERATOR()
        end

        if ipc.readLvar("FSDT_GSX_STAIRS") == 1 and ipc.readLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3 then
            if ipc.readLvar("FSDT_GSX_GPU_STATE") == 1 and gsxJetway == 2 then
                ipc.log("GSX_AUTO - Connect GPU")
                GSXAUTO_CALL_GPU()
            end
			ipc.log("GSX_AUTO - Connect Stairs")
            GSXAUTO_MENU_QUEUE_OPEN()
			GSXAUTO_MENU_QUEUE_NUM(7)
            GSXAUTO_MENU_QUEUE_OPERATOR()
		end
	end
    GSXAUTO_CONNECTED = 1
end

function GSXAUTO_CALL_DISCONNECT()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_DISCONNECT() then

	else
		local gsxJetway = ipc.readLvar("FSDT_GSX_JETWAY")
        local gsxJetwayOp = ipc.readLvar("FSDT_GSX_OPERATEJETWAYS_STATE")
		if gsxJetway >= 4 and gsxJetwayOp < 3 then
            ipc.log("GSX_AUTO - Disconnect Jetway")
            GSXAUTO_MENU_QUEUE_OPEN()
			GSXAUTO_MENU_QUEUE_NUM(6)
        end

		if ipc.readLvar("FSDT_GSX_STAIRS") >= 4 and ipc.readLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3 then
            if ipc.readLvar("FSDT_GSX_GPU_STATE") >=4 then
			    ipc.log("GSX_AUTO - Disconnect GPU")
                GSXAUTO_CALL_GPU()
            end
			ipc.log("GSX_AUTO - Disconnect Stairs")
            GSXAUTO_MENU_QUEUE_OPEN()
			GSXAUTO_MENU_QUEUE_NUM(7)
		end
	end
    GSXAUTO_CONNECTED = 0
end

function GSXAUTO_CALL_REFRESH_SB()
    ipc.log("GSX_AUTO - Refresh SimBrief")
    GSXAUTO_MENU_QUEUE_OPEN()
    GSXAUTO_MENU_QUEUE_NUM(15)
end

function GSXAUTO_CALL_REFUEL()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_REFUEL() then

	else
        ipc.log("GSX_AUTO - Requesting Refuel")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(3)
        GSXAUTO_MENU_QUEUE_OPERATOR()
	end
end

function GSXAUTO_CALL_CATER()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_CATER() then

	else
        ipc.log("GSX_AUTO - Requesting Cater")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(2)
        GSXAUTO_MENU_QUEUE_OPERATOR()
	end
end

function GSXAUTO_CALL_BOARD()
    GSXAUTO_DISABLE_CREW()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_BOARD() then

	else
        ipc.log("GSX_AUTO - Requesting Board")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(4)
	end
end

function GSXAUTO_CALL_PUSH()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_PUSH() then

	else
        local depature_state = ipc.readLvar("FSDT_GSX_DEPARTURE_STATE")
        if depature_state <= 1 then
			ipc.log("GSX_AUTO - Requesting Push-Back")
			GSXAUTO_MENU_QUEUE_OPEN()
			GSXAUTO_MENU_QUEUE_NUM(5)
            GSXAUTO_PUSH_REQUESTED = true
            GSXAUTO_PUSH_MENUOPENING = true
		elseif depature_state == 5 then
			ipc.log("GSX_AUTO - Confirm Good / Stop Now")
			GSXAUTO_MENU_QUEUE_OPEN()
			GSXAUTO_MENU_QUEUE_NUM(1)
            GSXAUTO_PUSH_CONFIRMED = true
		end
	end
end

function GSXAUTO_CALL_JETWAY()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_JETWAY() then

	else
        ipc.log("GSX_AUTO - Requesting Jetway")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(6)
        GSXAUTO_MENU_QUEUE_OPERATOR()
	end
end

function GSXAUTO_CALL_STAIRS()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_STAIRS() then

	else
        ipc.log("GSX_AUTO - Requesting Stairs")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(7)
        GSXAUTO_MENU_QUEUE_OPERATOR()
	end
end

function GSXAUTO_CALL_GPU()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_GPU() then

	else
        ipc.log("GSX_AUTO - Requesting GPU")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(8)
        GSXAUTO_MENU_QUEUE_NUM(1)
        GSXAUTO_MENU_QUEUE_OPERATOR()
	end
end

function GSXAUTO_CALL_DEICE()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_DEICE() then

	else
        ipc.log("GSX_AUTO - Requesting De-Ice on Stand")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(8)
        GSXAUTO_MENU_QUEUE_NUM(2)
	end
end

function GSXAUTO_START_DEICE()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_DEICE() then

	else
        ipc.log("GSX_AUTO - Starting Remote De-Ice")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(1)
	end
end

function GSXAUTO_CALL_DEBOARD()
    GSXAUTO_DISABLE_CREW()
    if GSXAUTO_AIRCRAFT_CUSTOM and GSXAUTO_CUSTOM_DEBOARD() then

	else
        ipc.log("GSX_AUTO - Requesting Deboard")
        GSXAUTO_MENU_QUEUE_OPEN()
        GSXAUTO_MENU_QUEUE_NUM(1)
        GSXAUTO_MENU_QUEUE_OPERATOR()
	end
end

function GSXAUTO_CALL_REPOSITION()
    ipc.log("GSX_AUTO - Call Reposition at Gate")
    GSXAUTO_MENU_QUEUE_OPEN()
    GSXAUTO_MENU_QUEUE_NUM(10)
    GSXAUTO_MENU_QUEUE_NUM(1)
end

function GSXAUTO_DO_UNFREEZE()
    ipc.log("GSX_AUTO - Unfreeze - Set Parking Brake")
    ipc.control(67271)
    ipc.sleep(300)
    ipc.log("GSX_AUTO - Unfreeze - Set Freeze SimVars to 0")
    ipc.execCalcCode("0 (>K:FREEZE_LATITUDE_LONGITUDE_SET) 0 (>K:FREEZE_ALTITUDE_SET) 0 (>K:FREEZE_ATTITUDE_SET)")
    ipc.log("GSX_AUTO - Unfreeze - Set GSX Freeze L-Var to 0")
    ipc.writeLvar("FSDT_VAR_Frozen", 0)
end

function GSXAUTO_CALL_NEXT()
    if GSXAUTO_STATE_CALLED then
        ipc.log("GSX_AUTO - Nothing to do for Call Next")
        return
    end

	if GSXAUTO_STATE == 0 then --Refuel
		GSXAUTO_CALL_REFUEL()
	elseif GSXAUTO_STATE == 1 then --Cater
		GSXAUTO_CALL_CATER()
	elseif GSXAUTO_STATE == 2 then --Board
		GSXAUTO_CALL_BOARD()
	elseif GSXAUTO_STATE == 3 then --Push
		GSXAUTO_CALL_PUSH()
    elseif GSXAUTO_STATE == 4 and GSXAUTO_DEICE_SELECTED then --Deice during Taxi-Out
		GSXAUTO_START_DEICE()
        GSXAUTO_STATE_CALLED = true
	elseif GSXAUTO_STATE == 8 then --Deboard
		GSXAUTO_CALL_DEBOARD()
	end
end

function QNUM1()
    GSXAUTO_MENU_QUEUE_NUM(1)
end

function QNUM2()
    GSXAUTO_MENU_QUEUE_NUM(2)
end

function QNUM3()
    GSXAUTO_MENU_QUEUE_NUM(3)
end

function QNUM4()
    GSXAUTO_MENU_QUEUE_NUM(4)
end

function QNUM5()
    GSXAUTO_MENU_QUEUE_NUM(5)
end

function QNUM6()
    GSXAUTO_MENU_QUEUE_NUM(6)
end

function QNUM7()
    GSXAUTO_MENU_QUEUE_NUM(7)
end

function QNUM8()
    GSXAUTO_MENU_QUEUE_NUM(8)
end

function QNUM9()
    GSXAUTO_MENU_QUEUE_NUM(9)
end

function QNUM10()
    GSXAUTO_MENU_QUEUE_NUM(10)
end

function QMENU()
    GSXAUTO_MENU_QUEUE_OPEN()
end

event.timer(500, "GSXAUTO_MAIN_LOOP")
event.control(66703, "GSXAUTO_MENU_EVENT")
event.param("GSXAUTO_MENU_QUEUE_NUM")

event.flag(1, "QNUM1")
event.flag(2, "QNUM2")
event.flag(3, "QNUM3")
event.flag(4, "QNUM4")
event.flag(5, "QNUM5")
event.flag(6, "QNUM6")
event.flag(7, "QNUM7")
event.flag(8, "QNUM8")
event.flag(9, "QNUM9")
event.flag(10, "QNUM10")
event.flag(11, "QMENU")

event.flag(12, "GSXAUTO_CALL_DEBOARD")
event.flag(13, "GSXAUTO_CALL_CATER")
event.flag(14, "GSXAUTO_CALL_REFUEL")
event.flag(15, "GSXAUTO_CALL_BOARD")
event.flag(16, "GSXAUTO_CALL_PUSH")
event.flag(17, "GSXAUTO_CALL_JETWAY")
event.flag(18, "GSXAUTO_CALL_STAIRS")
event.flag(19, "GSXAUTO_CALL_GPU")
event.flag(20, "GSXAUTO_CALL_DEICE")
event.flag(21, "GSXAUTO_CALL_NEXT")
event.flag(22, "GSXAUTO_CALL_CONNECT_TGL")
event.flag(23, "GSXAUTO_CALL_REPOSITION")
event.flag(24, "GSXAUTO_DO_UNFREEZE")


ipc.log("GSX_AUTO - Script active")