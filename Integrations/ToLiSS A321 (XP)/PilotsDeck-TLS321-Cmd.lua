------------------------------------------------------------------
-- Base Functions

local refArray = {}

function RefGet(id)
	if not refArray[id][2] then
		if refArray[id][0] == "int" then
			return XPLMGetDatai(refArray[id][1])
		elseif refArray[id][0] == "float" then
			return XPLMGetDataf(refArray[id][1])
		else
			return nil
		end
	else
		if refArray[id][0] == "int" then
			return XPLMGetDatavi(refArray[id][1], refArray[id][3], 1)[refArray[id][3]]
		elseif refArray[id][0] == "float" then
			return XPLMGetDatavf(refArray[id][1], refArray[id][3], 1)[refArray[id][3]]
		else
			return nil
		end
	end
end

function RefSet(id, value)
	if not refArray[id][2] then
		if refArray[id][0] == "int" then
			XPLMSetDatai(refArray[id][1], value)
		elseif refArray[id][0] == "float" then
			XPLMSetDataf(refArray[id][1], value)
		end
	else
		local outTable = { [refArray[id][3]] = value }
		if refArray[id][0] == "int" then
			XPLMSetDatavi(refArray[id][1], outTable, refArray[id][3], 1)
		elseif refArray[id][0] == "float" then
			XPLMSetDatavf(refArray[id][1], outTable, refArray[id][3], 1)
		end
	end
end

function RemoveArrayString(str)
	local indexFirst = string.find(str, "[", 1, true)
	
	if indexFirst ~= nil then
		str = string.sub(str, 1, indexFirst - 1)
	end
	
	return str
end

function RefAdd(id, typename, isArray, index)
	if isArray == nil then
		isArray = false
	end
	if index == nil then
		index = 0
	end
	
	refArray[id] = { [0] = typename, [1] = XPLMFindDataRef(RemoveArrayString(id)), [2] = isArray, [3] = index }
end

------------------------------------------------------------------
-- Command Queue/Sequence

local commandStack = {}
local stackPos = 1

function commandQueue(cmd)
	table.insert(commandStack, stackPos, cmd)
	stackPos = stackPos + 1
end

function commandExecute()
	if stackPos > 1 then
		local cmd = table.remove(commandStack, 1)
		command_once(cmd)
		stackPos = stackPos - 1
	end
end

do_often("commandExecute()")

------------------------------------------------------------------
-- General Button Functions

function TurnSwitch(id, step, valMin, valMax)
	local refValue = RefGet(id)
	
	refValue = refValue + step
	if refValue < valMin then
		refValue = valMin
	end
	if refValue > valMax then
		refValue = valMax
	end
	
	RefSet(id, refValue)
end

function TurnKnob(id, step)
	local refValue = RefGet(id)
	
	RefSet(id, refValue + step)
end

function SwitchToggler(id, cmdInc, cmdDec, onVal, offVal)
	local pos = RefGet(id)
	if pos >= onVal then
		command_once(cmdDec)
	elseif pos == offVal then
		command_once(cmdInc)
	elseif pos < offVal then
		command_once(cmdInc)
		command_once(cmdInc)
	end
end

function SwitchFullToggle(id, cmdInc, cmdDec, onVal, offVal)
	local pos = RefGet(id)
	if pos >= onVal then
		command_once(cmdDec)
		command_once(cmdDec)
	elseif pos <= offVal then
		command_once(cmdInc)
		command_once(cmdInc)
	else
		command_once(cmdInc)
		command_once(cmdInc)
	end
end

function SwitchSequenceRef(id, step, maxPos, startPos)
	local pos = RefGet(id)
	
	pos = pos + step
	if pos > maxPos then
		pos = startPos
	end
	
	RefSet(id, pos)
end

function RepeatCommand(cmd, count)
	for i=1,count,1 do
		command_once(cmd)
	end
end

function SwitchSequence(id, cmdInc, cmdDec, dirUp, maxPos, resetCount)
	local pos = RefGet(id)
	
	if dirUp then
		if pos == maxPos then
			RepeatCommand(cmdDec, resetCount)
		else
			command_once(cmdInc)
		end
	else
		if pos == maxPos then
			RepeatCommand(cmdInc, resetCount)
		else
			command_once(cmdDec)
		end
	end
end

------------------------------------------------------------------
-- FCU

function FcuMngSelToggle(id, pull, push)
	local isManaged = RefGet(id)
	
	if isManaged == 1 then
		command_once(pull)
	else
		command_once(push)
	end
end

-- Speed
RefAdd("AirbusFBW/SPDmanaged", "int")
RefAdd("sim/cockpit/autopilot/airspeed", "float")
RefAdd("sim/cockpit/autopilot/airspeed_is_mach", "int")

function FcuSpdTurn(steps)
	local spd = RefGet("sim/cockpit/autopilot/airspeed")
	local isMach = RefGet("sim/cockpit/autopilot/airspeed_is_mach")
	if isMach == 1 then
		steps = steps * 0.005
	end
	
	RefSet("sim/cockpit/autopilot/airspeed", spd + steps)
end

create_command("FlyWithLua/TLS2PLD/fcuSpdTgl","Toggle Speed between selected and managed Mode","FcuMngSelToggle('AirbusFBW/SPDmanaged', 'AirbusFBW/PullSPDSel', 'AirbusFBW/PushSPDSel')","","")
create_command("FlyWithLua/TLS2PLD/fcuSpdInc","Increase Speed by 10 Knots","FcuSpdTurn(10)","","")
create_command("FlyWithLua/TLS2PLD/fcuSpdDec","Decrease Speed by 10 Knots","FcuSpdTurn(-10)","","")


-- Heading
RefAdd("AirbusFBW/HDGmanaged", "int")
RefAdd("sim/cockpit/autopilot/heading_mag", "float")

create_command("FlyWithLua/TLS2PLD/fcuHdgTgl","Toggle Heading between selected and managed Mode","FcuMngSelToggle('AirbusFBW/HDGmanaged', 'AirbusFBW/PullHDGSel', 'AirbusFBW/PushHDGSel')","","")
create_command("FlyWithLua/TLS2PLD/fcuHdgInc","Increase Heading by 25 Degrees","TurnKnob('sim/cockpit/autopilot/heading_mag', 25)","","")
create_command("FlyWithLua/TLS2PLD/fcuHdgDec","Decrease Heading by 25 Degrees","TurnKnob('sim/cockpit/autopilot/heading_mag', -25)","","")


-- Altitude
RefAdd("AirbusFBW/ALTmanaged", "int")
RefAdd("sim/cockpit/autopilot/altitude", "float")
RefAdd("AirbusFBW/ALT100_1000", "int")

function FcuAltTurn(steps)
	local alt = RefGet("sim/cockpit/autopilot/altitude")
	local isThousand = RefGet("AirbusFBW/ALT100_1000")
	
	if isThousand == 0 then
		steps = steps / 10
	end
	
	RefSet("sim/cockpit/autopilot/altitude", alt + steps)
end

create_command("FlyWithLua/TLS2PLD/fcuAltTgl","Toggle Altitude between selected and managed Mode","FcuMngSelToggle('AirbusFBW/ALTmanaged', 'AirbusFBW/PullAltitude', 'AirbusFBW/PushAltitude')","","")
create_command("FlyWithLua/TLS2PLD/fcuAltInc","Increase Altitude by 500/5000ft","FcuAltTurn(5000)","","")
create_command("FlyWithLua/TLS2PLD/fcuAltDec","Decrease Altitude by 500/5000ft","FcuAltTurn(-5000)","","")


-- Vertical Speed
RefAdd("AirbusFBW/VSdashed", "int")
RefAdd("sim/cockpit/autopilot/vertical_velocity", "float")

create_command("FlyWithLua/TLS2PLD/fcuVsTgl","Toggle Vertical Speed","FcuMngSelToggle('AirbusFBW/VSdashed', 'AirbusFBW/PullVSSel', 'AirbusFBW/PushVSSel')","","")
create_command("FlyWithLua/TLS2PLD/fcuVsInc","Increase Vertical Speed by 500/0.5","TurnKnob('sim/cockpit/autopilot/vertical_velocity', 500)","","")
create_command("FlyWithLua/TLS2PLD/fcuVsDec","Decrease Vertical Speed by 500/0.5","TurnKnob('sim/cockpit/autopilot/vertical_velocity', -500)","","")

------------------------------------------------------------------
-- OVERHEAD

-- ADIRS
RefAdd("AirbusFBW/ADIRUDisplayData", "int")
RefAdd("AirbusFBW/ADIRUDisplaySystem", "int")

create_command("FlyWithLua/TLS2PLD/adirsDataInc", "Increase ADIRS Data by one", "TurnSwitch('AirbusFBW/ADIRUDisplayData', 1, 0, 5)", "", "")
create_command("FlyWithLua/TLS2PLD/adirsDataDec", "Decrease ADIRS Data by one", "TurnSwitch('AirbusFBW/ADIRUDisplayData', -1, 0, 5)", "", "")
create_command("FlyWithLua/TLS2PLD/adirsSysInc", "Increase ADIRS Sys by one", "TurnSwitch('AirbusFBW/ADIRUDisplaySystem', 1, 0, 3)", "", "")
create_command("FlyWithLua/TLS2PLD/adirsSysDec", "Decrease ADIRS Sys by one", "TurnSwitch('AirbusFBW/ADIRUDisplaySystem', -1, 0, 3)", "", "")

--Air Cond
RefAdd("AirbusFBW/CockpitTemp", "int")
RefAdd("AirbusFBW/FwdCabinTemp", "int")
RefAdd("AirbusFBW/AftCabinTemp", "int")
RefAdd("AirbusFBW/FwdCargoTemp", "float")
RefAdd("AirbusFBW/AftCargoTemp", "float")

create_command("FlyWithLua/TLS2PLD/airconTempCockpitInc", "Increase Cockpit Temperature by two Degrees", "TurnSwitch('AirbusFBW/CockpitTemp', 2, 16, 28)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempCockpitDec", "Decrease Cockpit Temperature by two Degrees", "TurnSwitch('AirbusFBW/CockpitTemp', -2, 16, 28)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempFwdCabinInc", "Increase Forward Cabin Temperature by two Degrees", "TurnSwitch('AirbusFBW/FwdCabinTemp', 2, 16, 28)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempFwdCabinDec", "Decrease Forward Cabin Temperature by two Degrees", "TurnSwitch('AirbusFBW/FwdCabinTemp', -2, 16, 28)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempAftCabinInc", "Increase Aft Cabin Temperature by two Degrees", "TurnSwitch('AirbusFBW/AftCabinTemp', 2, 16, 28)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempAftCabinDec", "Decrease Aft Cabin Temperature by two Degrees", "TurnSwitch('AirbusFBW/AftCabinTemp', -2, 16, 28)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempFwdCargoInc", "Increase Forward Cargo Temperature by 3.5 Degrees", "TurnSwitch('AirbusFBW/FwdCargoTemp', 3.5, 5, 26)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempFwdCargoDec", "Decrease Forward Cargo Temperature by 3.5 Degrees", "TurnSwitch('AirbusFBW/FwdCargoTemp', -3.5, 5, 26)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempAftCargoInc", "Increase Aft Cargo Temperature by 3.5 Degrees", "TurnSwitch('AirbusFBW/AftCargoTemp', 3.5, 5, 26)", "", "")
create_command("FlyWithLua/TLS2PLD/airconTempAftCargoDec", "Decrease Aft Cargo Temperature by 3.5 Degrees", "TurnSwitch('AirbusFBW/AftCargoTemp', -3.5, 5, 26)", "", "")

--Cabin Press
RefAdd("AirbusFBW/LandElev", "float")

create_command("FlyWithLua/TLS2PLD/cabPressLdgElevInc", "Increase Landing Elevation by 0.5", "TurnSwitch('AirbusFBW/LandElev', 0.5, -3, 14)", "", "")
create_command("FlyWithLua/TLS2PLD/cabPressLdgElevDec", "Decrease Landing Elevation by 0.5", "TurnSwitch('AirbusFBW/LandElev', -0.5, -3, 14)", "", "")
create_command("FlyWithLua/TLS2PLD/cabPressLdgElevIncFast", "Increase Landing Elevation by 0.5", "TurnSwitch('AirbusFBW/LandElev', 2, -3, 14)", "", "")
create_command("FlyWithLua/TLS2PLD/cabPressLdgElevDecFast", "Decrease Landing Elevation by 0.5", "TurnSwitch('AirbusFBW/LandElev', -2, -3, 14)", "", "")

--Exterior Lights
RefAdd("ckpt/oh/strobeLight/anim", "int")
RefAdd("ckpt/oh/navLight/anim", "int")
RefAdd("ckpt/oh/ladningLightLeft/anim", "int")
RefAdd("ckpt/oh/ladningLightRight/anim", "int")
RefAdd("ckpt/oh/taxiLight/anim", "int")

create_command("FlyWithLua/TLS2PLD/extLtStrobeTglOn", "Toggle Strobe Lights", "SwitchToggler('ckpt/oh/strobeLight/anim', 'toliss_airbus/lightcommands/StrobeLightUp', 'toliss_airbus/lightcommands/StrobeLightDown', 2, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/extLtStrobeTglOff", "Toggle Strobe Lights", "SwitchToggler('ckpt/oh/strobeLight/anim', 'toliss_airbus/lightcommands/StrobeLightUp', 'toliss_airbus/lightcommands/StrobeLightDown', 1, 0)", "", "")
create_command("FlyWithLua/TLS2PLD/extLtNavTgl", "Toggle Nav Lights", "SwitchToggler('ckpt/oh/navLight/anim', 'toliss_airbus/lightcommands/NavLightUp', 'toliss_airbus/lightcommands/NavLightDown', 1, 0)", "", "")
create_command("FlyWithLua/TLS2PLD/extLtLandTglOn", "Toggle Landing Lights", "SwitchToggler('ckpt/oh/ladningLightLeft/anim', 'toliss_airbus/lightcommands/LLandLightUp', 'toliss_airbus/lightcommands/LLandLightDown', 2, 1)", "", "SwitchToggler('ckpt/oh/ladningLightRight/anim', 'toliss_airbus/lightcommands/RLandLightUp', 'toliss_airbus/lightcommands/RLandLightDown', 2, 1)")
create_command("FlyWithLua/TLS2PLD/extLtLandTglOff", "Toggle Landing Lights", "SwitchToggler('ckpt/oh/ladningLightLeft/anim', 'toliss_airbus/lightcommands/LLandLightUp', 'toliss_airbus/lightcommands/LLandLightDown', 1, 0)", "", "SwitchToggler('ckpt/oh/ladningLightRight/anim', 'toliss_airbus/lightcommands/RLandLightUp', 'toliss_airbus/lightcommands/RLandLightDown', 1, 0)")
create_command("FlyWithLua/TLS2PLD/extLtLandTglFull", "Toggle Landing Lights", "SwitchFullToggle('ckpt/oh/ladningLightLeft/anim', 'toliss_airbus/lightcommands/LLandLightUp', 'toliss_airbus/lightcommands/LLandLightDown', 2, 0)", "", "SwitchFullToggle('ckpt/oh/ladningLightRight/anim', 'toliss_airbus/lightcommands/RLandLightUp', 'toliss_airbus/lightcommands/RLandLightDown', 2, 0)")
create_command("FlyWithLua/TLS2PLD/extLtNoseTglOn", "Toggle Nose Lights", "SwitchToggler('ckpt/oh/taxiLight/anim', 'toliss_airbus/lightcommands/NoseLightUp', 'toliss_airbus/lightcommands/NoseLightDown', 2, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/extLtNoseTglOff", "Toggle Nose Lights", "SwitchToggler('ckpt/oh/taxiLight/anim', 'toliss_airbus/lightcommands/NoseLightUp', 'toliss_airbus/lightcommands/NoseLightDown', 1, 0)", "", "")
create_command("FlyWithLua/TLS2PLD/extLtNoseTglFull", "Toggle Nose Lights", "SwitchFullToggle('ckpt/oh/taxiLight/anim', 'toliss_airbus/lightcommands/NoseLightUp', 'toliss_airbus/lightcommands/NoseLightDown', 2, 0)", "", "")

--Interior Lights
RefAdd("ckpt/oh/domeLight/anim", "int")
RefAdd("ckpt/oh/nosmoking/anim", "int")
RefAdd("ckpt/oh/emerExitLight/anim", "int")

create_command("FlyWithLua/TLS2PLD/intLtDomeTgl", "Toggle Dome Light", "SwitchSequence('ckpt/oh/domeLight/anim', 'toliss_airbus/lightcommands/DomeLightUp', 'toliss_airbus/lightcommands/DomeLightDown', false, 0, 2)", "", "")
create_command("FlyWithLua/TLS2PLD/intLtNoSmokeTglOn", "Toggle No Smoking Lights", "SwitchToggler('ckpt/oh/nosmoking/anim', 'toliss_airbus/lightcommands/NSSignUp', 'toliss_airbus/lightcommands/NSSignDown', 2, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/intLtNoSmokeTglOff", "Toggle No Smoking Lights", "SwitchToggler('ckpt/oh/nosmoking/anim', 'toliss_airbus/lightcommands/NSSignUp', 'toliss_airbus/lightcommands/NSSignDown', 1, 0)", "", "")
create_command("FlyWithLua/TLS2PLD/intLtEmerExitTglOn", "Toggle Emergency Exit Lights", "SwitchToggler('ckpt/oh/emerExitLight/anim', 'toliss_airbus/lightcommands/EmerExitLightUp', 'toliss_airbus/lightcommands/EmerExitLightDown', 2, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/intLtEmerExitTglOff", "Toggle Emergency Exit Lights", "SwitchToggler('ckpt/oh/emerExitLight/anim', 'toliss_airbus/lightcommands/EmerExitLightUp', 'toliss_airbus/lightcommands/EmerExitLightDown', 1, 0)", "", "")

--Panel/Integ Lights
RefAdd("AirbusFBW/OHPBrightnessLevel", "float")
RefAdd("AirbusFBW/SupplLightLevelRehostats[0]", "float", true, 0)
RefAdd("AirbusFBW/SupplLightLevelRehostats[1]", "float", true, 1)

create_command("FlyWithLua/TLS2PLD/integOvhdInc", "Increase Overhead Integral Light", "TurnSwitch('AirbusFBW/OHPBrightnessLevel', 0.15, 0, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/integOvhdDec", "Decrease Overhead Integral Light", "TurnSwitch('AirbusFBW/OHPBrightnessLevel', -0.15, 0, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/integFcuInc", "Increase FCU Integral Light", "TurnSwitch('AirbusFBW/SupplLightLevelRehostats[0]', 0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/integFcuDec", "Decrease FCU Integral Light", "TurnSwitch('AirbusFBW/SupplLightLevelRehostats[0]', -0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/integFcuDispInc", "Increase FCU Display Light", "TurnSwitch('AirbusFBW/SupplLightLevelRehostats[1]', 0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/integFcuDispDec", "Decrease FCU Display Light", "TurnSwitch('AirbusFBW/SupplLightLevelRehostats[1]', -0.15, 0.01, 1)", "", "")

--Misc
RefAdd("AirbusFBW/LeftWiperSwitch", "int")

create_command("FlyWithLua/TLS2PLD/wiperCptTgl", "Toggle Left/Captain Wiper", "SwitchSequenceRef('AirbusFBW/LeftWiperSwitch', 1, 2, 0)", "", "")

------------------------------------------------------------------
-- GLARESHIELD

-- EFIS
local convHgToHpa = 0.029529983071445
RefAdd("AirbusFBW/BaroStdCapt", "int")
RefAdd("AirbusFBW/BaroUnitCapt", "int")
RefAdd("sim/cockpit/misc/barometer_setting", "float")

function EfisBaroTurn(steps)
	if RefGet("AirbusFBW/BaroUnitCapt") == 1 then
		steps = steps * convHgToHpa
	else
		steps = steps / 100
	end
	
	RefSet("sim/cockpit/misc/barometer_setting", RefGet("sim/cockpit/misc/barometer_setting") + steps)
end

create_command("FlyWithLua/TLS2PLD/efisBaroCaptTgl", "Toggle Captain Barometer", "FcuMngSelToggle('AirbusFBW/BaroStdCapt', 'toliss_airbus/capt_baro_push', 'toliss_airbus/capt_baro_pull')", "", "")
create_command("FlyWithLua/TLS2PLD/efisBaroCaptInc", "Increase Captain Baro by One", "EfisBaroTurn(1)", "", "")
create_command("FlyWithLua/TLS2PLD/efisBaroCaptDec", "Decrease Captain Baro by One", "EfisBaroTurn(-1)", "", "")
create_command("FlyWithLua/TLS2PLD/efisBaroCaptIncFast", "Increase Captain Baro by One", "EfisBaroTurn(7.5)", "", "")
create_command("FlyWithLua/TLS2PLD/efisBaroCaptDecFast", "Decrease Captain Baro by One", "EfisBaroTurn(-7.5)", "", "")

RefAdd("AirbusFBW/NDrangeCapt", "int")
create_command("FlyWithLua/TLS2PLD/efisNDCaptInc", "Increase Captain ND Range", "TurnSwitch('AirbusFBW/NDrangeCapt', 1, 0, 5)", "", "")
create_command("FlyWithLua/TLS2PLD/efisNDCaptDec", "Decrease Captain ND Range", "TurnSwitch('AirbusFBW/NDrangeCapt', -1, 0, 5)", "", "")

function MasterAllPush()
	command_once("AirbusFBW/CaptWarnPush")
	command_once("AirbusFBW/CaptCautPush")
end
create_command("FlyWithLua/TLS2PLD/efisMasterAllPush", "Push both Master Warn and Caution", "MasterAllPush()", "", "")


------------------------------------------------------------------
-- MAIN Panel

-- Display Brightness
RefAdd("AirbusFBW/DUBrightness[0]", "float", true, 0)
RefAdd("AirbusFBW/DUBrightness[1]", "float", true, 1)
RefAdd("AirbusFBW/DUBrightness[4]", "float", true, 4)
RefAdd("AirbusFBW/DUBrightness[5]", "float", true, 5)
RefAdd("AirbusFBW/WXAlphaND1", "float")

create_command("FlyWithLua/TLS2PLD/dispCaptPfdInc", "Increase Captain PFD Brightness", "TurnSwitch('AirbusFBW/DUBrightness[0]', 0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispCaptPfdDec", "Decrease Captain PFD Brightness", "TurnSwitch('AirbusFBW/DUBrightness[0]', -0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispCaptNdInc", "Increase Captain ND Brightness", "TurnSwitch('AirbusFBW/DUBrightness[1]', 0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispCaptNdDec", "Decrease Captain ND Brightness", "TurnSwitch('AirbusFBW/DUBrightness[1]', -0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispCaptWxInc", "Increase Captain WX Brightness", "TurnSwitch('AirbusFBW/WXAlphaND1', 0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispCaptWxDec", "Decrease Captain WX Brightness", "TurnSwitch('AirbusFBW/WXAlphaND1', -0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispEcamTopInc", "Increase Upper ECAM Brightness", "TurnSwitch('AirbusFBW/DUBrightness[4]', 0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispEcamTopDec", "Decrease Upper ECAM Brightness", "TurnSwitch('AirbusFBW/DUBrightness[4]', -0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispEcamBotInc", "Increase Lower ECAM Brightness", "TurnSwitch('AirbusFBW/DUBrightness[5]', 0.15, 0.01, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/dispEcamBotDec", "Decrease Lower ECAM Brightness", "TurnSwitch('AirbusFBW/DUBrightness[5]', -0.15, 0.01, 1)", "", "")

-- ISIS
RefAdd("AirbusFBW/ISIBaroSetting", "float")

create_command("FlyWithLua/TLS2PLD/isisBaroInc", "Increase ISIS Barometer", "TurnKnob('AirbusFBW/ISIBaroSetting', 1)", "", "")
create_command("FlyWithLua/TLS2PLD/isisBaroDec", "Increase ISIS Barometer", "TurnKnob('AirbusFBW/ISIBaroSetting', -1)", "", "")


------------------------------------------------------------------
-- Pedestal

-- RMP
RefAdd("sim/cockpit2/radios/actuators/com1_standby_frequency_hz_833", "int")

create_command("FlyWithLua/TLS2PLD/rmp1FreqOutInc", "Increase Outer Frequency by 5", "TurnKnob('sim/cockpit2/radios/actuators/com1_standby_frequency_hz_833', 5000)", "", "")
create_command("FlyWithLua/TLS2PLD/rmp1FreqOutDec", "Decrease Outer Frequency by 5", "TurnKnob('sim/cockpit2/radios/actuators/com1_standby_frequency_hz_833', -5000)", "", "")
create_command("FlyWithLua/TLS2PLD/rmp1FreqInnInc", "Increase Inner Frequency by 100", "TurnKnob('sim/cockpit2/radios/actuators/com1_standby_frequency_hz_833', 100)", "", "")
create_command("FlyWithLua/TLS2PLD/rmp1FreqInnDec", "Decrease Inner Frequency by 100", "TurnKnob('sim/cockpit2/radios/actuators/com1_standby_frequency_hz_833', -100)", "", "")


-- WXR
RefAdd("ckpt/fped/radar/gain/anim", "float")

create_command("FlyWithLua/TLS2PLD/wxrGainInc", "Increase Weather Radar Gain", "TurnSwitch('ckpt/fped/radar/gain/anim', 10, -150, 120)", "", "")
create_command("FlyWithLua/TLS2PLD/wxrGainDec", "Decrease Weather Radar Gain", "TurnSwitch('ckpt/fped/radar/gain/anim', -10, -150, 120)", "", "")

RefAdd("ckpt/fped/radar/tilt/anim", "float")

create_command("FlyWithLua/TLS2PLD/wxrTiltInc", "Increase Weather Radar Tilt", "TurnSwitch('ckpt/fped/radar/tilt/anim', 10, -120, 120)", "", "")
create_command("FlyWithLua/TLS2PLD/wxrTiltDec", "Decrease Weather Radar Tilt", "TurnSwitch('ckpt/fped/radar/tilt/anim', -10, -120, 120)", "", "")

RefAdd("ckpt/fped/radar/mode/anim", "int")

create_command("FlyWithLua/TLS2PLD/wxrModeTgl", "Toggle Weather Radar Mode", "SwitchSequenceRef('ckpt/fped/radar/mode/anim', 1, 3, 0)", "", "")

RefAdd("ckpt/radar/sys/anim", "int")
RefAdd("ckpt/ped/radar/manAuto/anim", "int")
RefAdd("ckpt/ped/radar/gcs/anim", "int")
RefAdd("ckpt/ped/radar/pwr/anim", "int")
function wxToggle()
	if RefGet("ckpt/radar/sys/anim") ~= 0 then
		RefSet("ckpt/radar/sys/anim", 0)
		RefSet("ckpt/ped/radar/manAuto/anim", 1)
		RefSet("ckpt/ped/radar/gcs/anim", 1)
		RefSet("ckpt/fped/radar/mode/anim", 1)
		RefSet("ckpt/ped/radar/pwr/anim", 2)
	else
		RefSet("ckpt/radar/sys/anim", 1)
		RefSet("ckpt/ped/radar/pwr/anim", 0)
	end
end

create_command("FlyWithLua/TLS2PLD/wxrTgl", "Toggle Weather Radar", "wxToggle()", "", "")

--XPDR
RefAdd("AirbusFBW/XPDRPower", "int")

create_command("FlyWithLua/TLS2PLD/xpdrModeTgl", "Toggle XPDR Mode between XPNDR, TA, TARA", "SwitchSequenceRef('AirbusFBW/XPDRPower', 1, 4, 2)", "", "")

-- Lights
RefAdd("AirbusFBW/PanelFloodBrightnessLevel", "float")

create_command("FlyWithLua/TLS2PLD/floodMainInc", "Increase Main Flood Light", "TurnSwitch('AirbusFBW/PanelFloodBrightnessLevel', 0.15, 0, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/floodMainDec", "Decrease Main Flood Light", "TurnSwitch('AirbusFBW/PanelFloodBrightnessLevel', -0.15, 0, 1)", "", "")

RefAdd("AirbusFBW/PedestalFloodBrightnessLevel", "float")

create_command("FlyWithLua/TLS2PLD/floodPedInc", "Increase Pedestal Flood Light", "TurnSwitch('AirbusFBW/PedestalFloodBrightnessLevel', 0.15, 0, 1)", "", "")
create_command("FlyWithLua/TLS2PLD/floodPedDec", "Decrease Pedestal Flood Light", "TurnSwitch('AirbusFBW/PedestalFloodBrightnessLevel', -0.15, 0, 1)", "", "")

--MCDU
RefAdd("sim/flightmodel2/gear/on_ground[0]", "int", true, 0)
function McduAocMenu(mcduNum)
	commandQueue("AirbusFBW/MCDU" .. mcduNum .. "Menu")
	commandQueue(McduLskKey(mcduNum, "2", "L"))
	commandQueue(McduLskKey(mcduNum, "1", "R"))
end

function McduFmgc(mcduNum)
	commandQueue("AirbusFBW/MCDU" .. mcduNum .. "Menu")
	commandQueue(McduLskKey(mcduNum, "1", "L"))
	if RefGet("sim/flightmodel2/gear/on_ground[0]") == 1 then
		commandQueue("AirbusFBW/MCDU" .. mcduNum .. "Init")
	else
		commandQueue("AirbusFBW/MCDU" .. mcduNum .. "Fpln")
	end
end

function McduLskKey(mcduNum, num, side)
	return "AirbusFBW/MCDU" .. mcduNum .. "LSK" .. num .. side
end

function McduKey(mcduNum, keyName)
	return "AirbusFBW/MCDU" .. mcduNum .. "Key" .. keyName
end

create_command("FlyWithLua/TLS2PLD/mcdu1AocMenu", "Open AOC Menu on MCDU1", "McduAocMenu('1')", "", "")
create_command("FlyWithLua/TLS2PLD/mcdu2AocMenu", "Open AOC Menu on MCDU1", "McduAocMenu('2')", "", "")
create_command("FlyWithLua/TLS2PLD/mcdu1Fmgc", "Open FMGC on MCDU1", "McduFmgc('1')", "", "")
create_command("FlyWithLua/TLS2PLD/mcdu2Fmgc", "Open FMGC on MCDU1", "McduFmgc('2')", "", "")

------------------------------------------------------------------
-- Misc

RefAdd("AirbusFBW/SlideArmedArray[0]", "int", true, 0)
function SlidesToggle()
	if RefGet("AirbusFBW/SlideArmedArray[0]") == 1 then
		command_once("toliss_airbus/door_commands/disarm_slides")
	else
		command_once("toliss_airbus/door_commands/arm_slides")
	end
end

create_command("FlyWithLua/TLS2PLD/slidesToggle", "Toggle the Slides on/off", "SlidesToggle()", "", "")