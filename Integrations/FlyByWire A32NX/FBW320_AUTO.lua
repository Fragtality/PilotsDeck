---@diagnostic disable: undefined-global

-----------------------------------------
-- $$ CONFIG

-----------------------------------------
-- $$ Base Functions
function FBW_BTN_PRESS(lvar, delay, value, offvalue)
	delay = delay or 150
	value = value or 1
	offvalue = offvalue or 0
	ipc.writeLvar(lvar, value)
	ipc.sleep(delay)
	ipc.writeLvar(lvar, offvalue)
end

function FBW_TOGGLER(lvar, poson, posoff)
	local pos = ipc.readLvar(lvar)
	
	if pos == poson then
		pos = posoff
	else
		pos = poson
	end
	
	ipc.writeLvar(lvar, pos)
end

function FBW_REPEAT_CONTROL(control, steps, delay)
	for i=1,steps do
		ipc.control(control)
		if delay then 
			ipc.sleep(delay)
		end
	end
end

function FBW_TWIST(lvar, step, lastval)
	local pos = ipc.readLvar(lvar)
	if lastval then
		if step > 0 and pos < lastval then
			ipc.writeLvar(lvar, pos + step)
		elseif step < 0 and pos > lastval then
			ipc.writeLvar(lvar, pos + step)
		end
	elseif not lastval then
		ipc.writeLvar(lvar, pos + step)
	end
end

function FBW_TWIST_OFFSET_UD(offset, step, lastval)
	local pos = ipc.readUD(offset)
	if lastval then
		if step > 0 and pos < lastval then
			ipc.writeUD(offset, (pos + step)/100)
		elseif step < 0 and pos > lastval then
			ipc.writeUD(offset, (pos + step)/100)
		end
	elseif not lastval then
		ipc.writeUD(offset, (pos + step)/100)
	end
end

function FBW_SEQUENCE(lvar, posmax, posstart)
	local pos = ipc.readLvar(lvar)
	
	if (pos + 1) > posmax then
		pos = posstart
	else
		pos = pos + 1
	end
		
	ipc.writeLvar(lvar, pos)
end

-----------------------------------------
-- $$ Lights

function FBW_LT_LAND_ON_TGL()
	FBW_TOGGLER("LIGHTING_LANDING_2", 0, 1)
	FBW_TOGGLER("LIGHTING_LANDING_3", 0, 1)
	ipc.sleep(500)
	if ipc.readLvar("LANDING_2_Retracted") ~= 0 then
		ipc.writeLvar("LANDING_2_Retracted", 0)
		ipc.writeLvar("LANDING_3_Retracted", 0)
	end
	ipc.sleep(500)
	if ipc.readLvar("LIGHTING_LANDING_2") == 0 then
		ipc.control(67072, 18)
		ipc.control(67072, 19)
	end
end

function FBW_LT_LAND_OFF_TGL()
	if ipc.readLvar("LIGHTING_LANDING_2") == 0 then
		ipc.control(67072, 18)
		ipc.control(67072, 19)
	end
	
	FBW_TOGGLER("LIGHTING_LANDING_2", 2, 1)
	FBW_TOGGLER("LIGHTING_LANDING_3", 2, 1)
	ipc.sleep(500)
	if ipc.readLvar("LIGHTING_LANDING_2") == 2 then
		ipc.writeLvar("LANDING_2_Retracted", 1)
		ipc.writeLvar("LANDING_3_Retracted", 1)
	else
		ipc.writeLvar("LANDING_2_Retracted", 0)
		ipc.writeLvar("LANDING_3_Retracted", 0)
	end
end

function FBW_LT_TAXI_TO_TGL()
	local oldPos = ipc.readLvar("LIGHTING_LANDING_1")
	FBW_TOGGLER("LIGHTING_LANDING_1", 0, 1)
	ipc.control(67072, 17)
	if oldPos == 2 then
		ipc.control(67072, 20)
	end
end

function FBW_LT_TAXI_ON_TGL()
	local oldPos = ipc.readLvar("LIGHTING_LANDING_1")
	FBW_TOGGLER("LIGHTING_LANDING_1", 2, 1)
	ipc.control(67072, 20)
	if oldPos == 0 then
		ipc.control(67072, 17)
	end
end

-----------------------------------------
-- $$ Calls

function FBW_CALL_MECH()
	FBW_BTN_PRESS("PUSH_OVHD_CALLS_MECH", 200)
end

function FBW_CALL_ALL()
	FBW_BTN_PRESS("PUSH_OVHD_CALLS_ALL", 200)
end

function FBW_CALL_FWD()
	FBW_BTN_PRESS("PUSH_OVHD_CALLS_FWD", 200)
end

function FBW_CALL_AFT()
	FBW_BTN_PRESS("PUSH_OVHD_CALLS_AFT", 200)
end

-----------------------------------------
-- $$ EFIS / Glareshield

function FBW_BARO_INC()
	if ipc.readLvar("XMLVAR_Baro_Selector_HPA_1") == 1 then
		FBW_REPEAT_CONTROL(65883, 3)
	else
		ipc.control(65883)
	end
end

function FBW_BARO_DEC()
	if ipc.readLvar("XMLVAR_Baro_Selector_HPA_1") == 1 then
		FBW_REPEAT_CONTROL(65884, 3)
	else
		ipc.control(65884)
	end
end

function FBW_BARO_INCFAST()
	if ipc.readLvar("XMLVAR_Baro_Selector_HPA_1") == 1 then
		FBW_REPEAT_CONTROL(65883, 15)
	else
		FBW_REPEAT_CONTROL(65883, 11)
	end
end

function FBW_BARO_DECFAST()
	if ipc.readLvar("XMLVAR_Baro_Selector_HPA_1") == 1 then
		FBW_REPEAT_CONTROL(65884, 15)
	else
		FBW_REPEAT_CONTROL(65884, 11)
	end
end

function FBW_ND_RANGE_INC()
	FBW_TWIST("A32NX_EFIS_L_ND_RANGE", 1, 5)
end

function FBW_ND_RANGE_DEC()
	FBW_TWIST("A32NX_EFIS_L_ND_RANGE", -1, 0)
end

function FBW_MASTER_CLEAR()
	if ipc.readLvar("A32NX_MASTER_WARNING") == 1 then
		ipc.writeLvar("A32NX_MASTER_WARNING", 0)
	end
	if ipc.readLvar("A32NX_MASTER_CAUTION") == 1 then
		ipc.writeLvar("A32NX_MASTER_CAUTION", 0)
	end
	FBW_BTN_PRESS("A32NX_BTN_CLR")
	ipc.sleep(50)
	FBW_BTN_PRESS("A32NX_BTN_CLR")
end

-----------------------------------------
-- $$ FCU

function FBW_FCU_SPD_TOGGLE()
	if ipc.readLvar("A32NX_FCU_SPD_MANAGED_DOT") == 1 then
		ipc.control(66094)
	else
		ipc.control(66093)
	end
end

function FBW_FCU_HDG_TOGGLE()
	if ipc.readLvar("A32NX_FCU_HDG_MANAGED_DOT") == 1 then
		ipc.control(65815)
	else
		ipc.control(65807)
	end
end

function FBW_FCU_ALT_TOGGLE()
	if ipc.readLvar("A32NX_FCU_ALT_MANAGED") == 1 then
		ipc.control(65816)
	else
		ipc.control(65808)
	end
end

function FBW_FCU_ATHR_TOGGLE()
	if ipc.readLvar("A32NX_AUTOTHRUST_STATUS") == 0 then
		ipc.control(65860)
	else
		ipc.control(67279)
	end
end

-----------------------------------------
-- $$ MIP / MainPanel

function FBW_PFD_CP_INC()
	FBW_TWIST_OFFSET_UD(0x5411, 5, 100)
end

function FBW_PFD_CP_DEC()
	FBW_TWIST_OFFSET_UD(0x5411, -5, 0)
end

function FBW_ND_OUTER_CP_INC()
	FBW_TWIST_OFFSET_UD(0x5415, 5, 100)
end

function FBW_ND_OUTER_CP_DEC()
	FBW_TWIST_OFFSET_UD(0x5415, -5, 0)
end

function FBW_AUTOBRAKE_TGL(pos)
	local selBrakes = ipc.readLvar("A32NX_AUTOBRAKES_ARMED_MODE")
	if selBrakes ~= pos then
		ipc.writeLvar("A32NX_AUTOBRAKES_ARMED_MODE_SET", pos)
	else
		ipc.writeLvar("A32NX_AUTOBRAKES_ARMED_MODE_SET", 0)
	end
end

function FBW_AUTOBRAKE_TGL_LO()
	FBW_AUTOBRAKE_TGL(1)
end

function FBW_AUTOBRAKE_TGL_MED()
	FBW_AUTOBRAKE_TGL(2)
end

function FBW_AUTOBRAKE_TGL_MAX()
	FBW_AUTOBRAKE_TGL(3)
end

function FBW_CHRONO_TGL()
	FBW_TOGGLER("A32NX_CHRONO_ET_SWITCH_POS", 0, 1)
end

-----------------------------------------
-- $$ MIP / MainPanel

function FBW_WX_MODE()
	FBW_SEQUENCE("XMLVAR_A320_WEATHERRADAR_MODE", 3, 0)
end

function FBW_XPDR_TRAFFIC_SEQ()
	FBW_SEQUENCE("A32NX_SWITCH_TCAS_TRAFFIC_POSITION", 3, 0)
end

-----------------------------------------
-- $$ MCDU

function FBW_MCDU1_AOC()
	ipc.execCalcCode("(>H:A320_Neo_CDU_1_BTN_MENU)")
	ipc.sleep(250)
	ipc.execCalcCode("(>H:A320_Neo_CDU_1_BTN_L2)")
	ipc.sleep(1500)
	ipc.execCalcCode("(>H:A320_Neo_CDU_1_BTN_R2)")
end

-----------------------------------------
-- $$ Pedestal

function FBW_ENG_MODE_IGN()
	ipc.control(67017, 2)
	ipc.control(67018, 2)
end

function FBW_ENG_MODE_NORM()
	ipc.control(67017, 1)
	ipc.control(67018, 1)
end

function FBW_ENG_MODE_CRANK()
	ipc.control(67017, 0)
	ipc.control(67018, 0)
end

-----------------------------------------
-----------------------------------------
-- $$ EVENT FLAGS


event.flag(1, "FBW_LT_LAND_ON_TGL")
event.flag(2, "FBW_LT_LAND_OFF_TGL")
event.flag(3, "FBW_CALL_MECH")
event.flag(4, "FBW_CALL_ALL")
event.flag(5, "FBW_CALL_FWD")
event.flag(6, "FBW_CALL_AFT")
event.flag(7, "FBW_BARO_INC")
event.flag(8, "FBW_BARO_DEC")
event.flag(9, "FBW_BARO_INCFAST")
event.flag(10, "FBW_BARO_DECFAST")
event.flag(11, "FBW_ND_RANGE_INC")
event.flag(12, "FBW_ND_RANGE_DEC")
event.flag(13, "FBW_MASTER_CLEAR")
event.flag(14, "FBW_FCU_SPD_TOGGLE")
event.flag(15, "FBW_FCU_HDG_TOGGLE")
event.flag(16, "FBW_FCU_ALT_TOGGLE")
event.flag(17, "FBW_FCU_ATHR_TOGGLE")
event.flag(18, "FBW_PFD_CP_INC")
event.flag(19, "FBW_PFD_CP_DEC")
event.flag(20, "FBW_ND_OUTER_CP_INC")
event.flag(21, "FBW_ND_OUTER_CP_DEC")
event.flag(22, "FBW_AUTOBRAKE_TGL_LO")
event.flag(23, "FBW_AUTOBRAKE_TGL_MED")
event.flag(24, "FBW_AUTOBRAKE_TGL_MAX")
event.flag(25, "FBW_WX_MODE")
event.flag(26, "FBW_XPDR_TRAFFIC_SEQ")
event.flag(27, "FBW_MCDU1_AOC")
event.flag(28, "FBW_LT_TAXI_TO_TGL")
event.flag(29, "FBW_LT_TAXI_ON_TGL")
event.flag(30, "FBW_CHRONO_TGL")
event.flag(31, "FBW_ENG_MODE_IGN")
event.flag(32, "FBW_ENG_MODE_NORM")
event.flag(33, "FBW_ENG_MODE_CRANK")
