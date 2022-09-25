local SYNC_FDLS = true
local FRAME_MOD = 6
local WAIT_TIME = 10

local frameCounter = 0
local startTime = os.clock()

dataref("aircraft", "sim/aircraft/view/acf_livery_path")

local fcuStrSpd = create_dataref_table("FlyWithLua/TLS2PLD/fcuSpd", "Data")
local fcuStrHdg = create_dataref_table("FlyWithLua/TLS2PLD/fcuHdg", "Data")
local fcuStrAlt = create_dataref_table("FlyWithLua/TLS2PLD/fcuAlt", "Data")
local fcuStrVs = create_dataref_table("FlyWithLua/TLS2PLD/fcuVs", "Data")

dataref("fcuSpdDashed", "AirbusFBW/SPDdashed")
dataref("fcuSpdManaged", "AirbusFBW/SPDmanaged")
dataref("fcuSpdValue", "sim/cockpit/autopilot/airspeed")
dataref("fcuSpdMach", "sim/cockpit/autopilot/airspeed_is_mach")

dataref("fcuHdgDashed", "AirbusFBW/HDGdashed")
dataref("fcuHdgManaged", "AirbusFBW/HDGmanaged")
dataref("fcuHdgValue", "sim/cockpit/autopilot/heading_mag")
dataref("fcuTrkMode", "AirbusFBW/HDGTRKmode")

dataref("fcuAltManaged", "AirbusFBW/ALTmanaged")
dataref("fcuAltValue", "sim/cockpit/autopilot/altitude")
dataref("fcuAltIsThousand", "AirbusFBW/ALT100_1000")

dataref("fcuVsDashed", "AirbusFBW/VSdashed")
dataref("fcuVsValue", "sim/cockpit/autopilot/vertical_velocity")


function UpdateFCU()
	--SPEED
	local result = ""
	if fcuSpdMach == 1 then
		result = "MCH\n"
	else
		result = "SPD\n"
	end
	
	if fcuSpdDashed == 0 then
		if fcuSpdMach == 1 then
			result = result .. string.format("%.2f", fcuSpdValue)
		else
			result = result .. string.format("%.3d", math.floor(fcuSpdValue+0.5))
		end
	else
		result = result .. "---"
	end
	
	if fcuSpdManaged == 1 then
		result = result .. "*"
	else
		result = result .. ' '
	end
	if fcuSpdMach == 0 then
		result = result .. ' '
	end
	fcuStrSpd[0] = result
	
	--HEADING
	result = ""
	if fcuTrkMode == 1 then
		result = "TRK\n"
	else
		result = "HDG\n"
	end
	
	if fcuHdgDashed == 0 then
		result = result .. string.format("%03d", fcuHdgValue)
	else
		result = result .. "---"
	end
	
	if fcuHdgManaged == 1 then
		result = result .. "*"
	else
		result = result .. ' '
	end

	fcuStrHdg[0] = result
	
	--ALTITUDE
	if fcuAltIsThousand == 1 then
		result = string.format("%05d", fcuAltValue)
	else
		result = string.format("%06.3f", fcuAltValue/1000.0)
	end
	
	if fcuAltManaged == 1 then
		result = result .. "*"
	else
		result = result .. ' '
	end
	
	if fcuAltIsThousand == 1 then
		result = result .. ' '
	end
	
	fcuStrAlt[0] = result
	
	--VS
	if fcuTrkMode == 1 then --4
		result = "FPA\n"
	else
		result = "V/S\n"
	end
	
	if fcuVsDashed == 0 then
		if fcuTrkMode == 1 then
			result = result .. string.format("%+.1f ", math.floor((fcuVsValue/100)+0.5)/10) --5
		else
			result = result .. string.format("%+05d", math.floor((fcuVsValue/100)+0.5)*100) --5
		end
	else
		result = result .. "-----"
	end
	
	fcuStrVs[0] = result
end



local extLightLandPos = create_dataref_table("FlyWithLua/TLS2PLD/extLightLandPos", "Int")
dataref("extLightLLand", "ckpt/oh/ladningLightLeft/anim")
dataref("extLightRLand", "ckpt/oh/ladningLightRight/anim")

local gearLightUnlk = create_dataref_table("FlyWithLua/TLS2PLD/gearLightUnlk", "Float")
local gearLightDown = create_dataref_table("FlyWithLua/TLS2PLD/gearLightDown", "Float")
dataref("gearLightUnlk1", "ckpt/lamp/207")
dataref("gearLightUnlk2", "ckpt/lamp/208")
dataref("gearLightUnlk3", "ckpt/lamp/209")
dataref("gearLightDown1", "ckpt/lamp/210")
dataref("gearLightDown2", "ckpt/lamp/211")
dataref("gearLightDown3", "ckpt/lamp/212")

function UpdateLights()
	if extLightLLand ~= extLightRLand then
		extLightLandPos[0] = -1
	else
		extLightLandPos[0] = extLightLLand
	end
	
	gearLightUnlk[0] = gearLightUnlk1 + gearLightUnlk2 + gearLightUnlk3
	gearLightDown[0] = gearLightDown1 + gearLightDown2 + gearLightDown3
end



local baroStrCapt = create_dataref_table("FlyWithLua/TLS2PLD/baroCapt", "Data")
local baroStrISIS = create_dataref_table("FlyWithLua/TLS2PLD/baroISIS", "Data")
local convHgToHpa = 33.863886666667
dataref("baroCaptIsStd", "AirbusFBW/BaroStdCapt")
dataref("baroCaptIsHpa", "AirbusFBW/BaroUnitCapt")
dataref("baroCaptValue", "sim/cockpit/misc/barometer_setting")
dataref("baroISISIsStd", "AirbusFBW/ISIBaroStd")
dataref("baroISISValue", "AirbusFBW/ISIBaroSetting")
dataref("efisFD1Engage", "AirbusFBW/FD1Engage", "writable")
dataref("efisFD2Engage", "AirbusFBW/FD2Engage", "writable")
dataref("efisLS1Engage", "AirbusFBW/ILSonCapt", "writable")
dataref("efisLS2Engage", "AirbusFBW/ILSonFO", "writable")


function UpdateEFIS()
	--Captain Baro Display
	local result = ""
	if baroCaptIsStd == 1 then
		result = "STD  "
	else
		if baroCaptIsHpa == 1 then
			result = string.format("%d ", math.floor((baroCaptValue * convHgToHpa)+0.5))
			if baroCaptValue * convHgToHpa < 1000 then
				result = result .. " "
			end
			
		else
			result = string.format("%.2f", baroCaptValue)
		end
	end
	
	baroStrCapt[0] = result
	
	--ISIS Baro Display
	if baroISISIsStd == 1 then
		result = "STD  "
	else
		result = string.format("%d", baroISISValue)
	end
	
	baroStrISIS[0] = result
	
	--Sync EFIS
	if SYNC_FDLS then
		if efisFD1Engage ~= efisFD2Engage then
			efisFD2Engage = efisFD1Engage
		end
		
		if efisLS1Engage ~= efisLS2Engage then
			efisLS2Engage = efisLS1Engage
		end
	end
end

local rudderTrimStr = create_dataref_table("FlyWithLua/TLS2PLD/rudderTrimStr", "Data")
dataref("rudderTrim", "AirbusFBW/YawTrimPosition")

local pitchTrimStr = create_dataref_table("FlyWithLua/TLS2PLD/pitchTrimStr", "Data")
dataref("pitchTrim", "AirbusFBW/PitchTrimPosition")

function UpdateTrimDisplays()
	local result = ""
	if rudderTrim < 0 then
		result = string.format("L %0.1f", math.abs(rudderTrim))
	elseif rudderTrim > 0 then
		result = string.format("R %0.1f", rudderTrim)
	else
		result = string.format(" %0.1f", math.abs(rudderTrim))
	end
	
	rudderTrimStr[0] = result
	
	
	result = ""
	if pitchTrim < 0 then
		result = string.format("%0.1f DN", math.abs(pitchTrim))
	elseif pitchTrim > 0 then
		result = string.format("%0.1f UP", pitchTrim)
	else
		result = string.format("%0.1f   ", math.abs(pitchTrim))
	end
	
	pitchTrimStr[0] = result
end


local clockStrZuluTime = create_dataref_table("FlyWithLua/TLS2PLD/clockZuluTime", "Data")
dataref("zuluHours", "sim/cockpit2/clock_timer/zulu_time_hours")
dataref("zuluMinutes", "sim/cockpit2/clock_timer/zulu_time_minutes")
dataref("zuluSeconds", "sim/cockpit2/clock_timer/zulu_time_seconds")
local clockStrET = create_dataref_table("FlyWithLua/TLS2PLD/clockET", "Data")
dataref("etHours", "AirbusFBW/ClockETHours")
dataref("etMinutes", "AirbusFBW/ClockETMinutes")
dataref("etRunning", "AirbusFBW/ClockETSwitch")
dataref("etDisplayed", "AirbusFBW/ClockShowsET")

function UpdateClock()
	local result = string.format("%02d:%02d:%02d", zuluHours, zuluMinutes, zuluSeconds)
	
	clockStrZuluTime[0] = result
	
	if etDisplayed == 1 then
		if etRunning == 0 then
			result = string.format("%02d:%02d", etHours, etMinutes)
		else
			result = string.format("%02d %02d", etHours, etMinutes)
		end
	else
		result = "     "
	end
	
	clockStrET[0] = result
end



function UpdateRefs()
	frameCounter = frameCounter + 1
	if frameCounter % FRAME_MOD ~= 0 then
		return
	end
	
	if os.clock() < startTime + WAIT_TIME then
		return
	end	
	
	if not string.match(aircraft, "ToLissA321") then
		logMsg("Toliss not found, exiting!")
		os.exit()
	end
	
	if frameCounter % FRAME_MOD == 0 then
		UpdateFCU()
		UpdateLights()
		UpdateEFIS()
		UpdateTrimDisplays()
	end
end

do_every_frame("UpdateRefs()")
do_often("UpdateClock()")

