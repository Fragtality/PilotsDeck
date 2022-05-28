---@diagnostic disable: undefined-global

-----------------------------------------
-- $$ CONFIG

-----------------------------------------
-- $$ Base Functions

function FNX_BTN_PRESS(lvar, delay, value, offvalue)
	delay = delay or 150
	value = value or 1
	offvalue = offvalue or 0
	ipc.writeLvar(lvar, value)
	ipc.sleep(delay)
	ipc.writeLvar(lvar, offvalue)
end

function FNX_TOGGLER(lvar, poson, posoff)
	local pos = ipc.readLvar(lvar)
	
	if pos == poson then
		pos = posoff
	else
		pos = poson
	end
	
	ipc.writeLvar(lvar, pos)
end

function FNX_SEQUENCE(lvar, posmax, posstart)
	local pos = ipc.readLvar(lvar)
	
	if (pos + 1) > posmax then
		pos = posstart
	else
		pos = pos + 1
	end
		
	ipc.writeLvar(lvar, pos)
end

function FNX_TWIST(lvar, step, lastval)
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

-----------------------------------------
-- $$ Overhead

function FNX_OVH_PACK_FLOW()
	FNX_SEQUENCE("S_OH_PNEUMATIC_PACK_FLOW", 2, 0)
end

function FNX_FIRE_TEST()
	local hold = 4000
	local delay = 1000
	
	ipc.writeLvar("S_OH_FIRE_APU_TEST",1)
	ipc.sleep(hold)
	ipc.writeLvar("S_OH_FIRE_APU_TEST",0)
	ipc.sleep(delay)

	ipc.writeLvar("S_OH_FIRE_ENG1_TEST",1)
	ipc.sleep(hold)
	ipc.writeLvar("S_OH_FIRE_ENG1_TEST",0)
	ipc.sleep(delay)

	ipc.writeLvar("S_OH_FIRE_ENG2_TEST",1)
	ipc.sleep(hold)
	ipc.writeLvar("S_OH_FIRE_ENG2_TEST",0)
end

function FNX_WIPER_CAPT()
	FNX_SEQUENCE("S_MISC_WIPER_CAPT", 2, 0)
end

-----------------------------------------
-- $$ Lights

function FNX_LT_STROBE_ON_TGL()
	FNX_TOGGLER("S_OH_EXT_LT_STROBE", 2, 1)
end

function FNX_LT_STROBE_OFF_TGL()
	FNX_TOGGLER("S_OH_EXT_LT_STROBE", 1, 0)
end

function FNX_LT_LAND_ON_TGL()
	FNX_TOGGLER("S_OH_EXT_LT_LANDING_L", 2, 1)
	FNX_TOGGLER("S_OH_EXT_LT_LANDING_R", 2, 1)
end

function FNX_LT_LAND_OFF_TGL()
	FNX_TOGGLER("S_OH_EXT_LT_LANDING_L", 1, 0)
	FNX_TOGGLER("S_OH_EXT_LT_LANDING_R", 1, 0)
end

function FNX_LT_NOSE_TO_TGL()
	FNX_TOGGLER("S_OH_EXT_LT_NOSE", 2, 1)
end

function FNX_LT_NOSE_ON_TGL()
	FNX_TOGGLER("S_OH_EXT_LT_NOSE", 1, 0)
end

function FNX_LT_DOME_BRT_TGL()
	FNX_TOGGLER("S_OH_INT_LT_DOME", 2, 1)
end

function FNX_LT_DOME_DIM_TGL()
	FNX_TOGGLER("S_OH_INT_LT_DOME", 1, 0)
end

-----------------------------------------
-- $$ Glareshield / EFIS

function FNX_MASTER_CLEAR()
	FNX_BTN_PRESS("S_MIP_MASTER_WARNING_CAPT", 300)
	ipc.sleep(200)
	FNX_BTN_PRESS("S_MIP_MASTER_CAUTION_CAPT", 300)
	ipc.sleep(200)
	FNX_BTN_PRESS("S_ECAM_CLR_LEFT", 300)
	ipc.sleep(200)
	FNX_BTN_PRESS("S_ECAM_CLR_LEFT", 300)
end

function FNX_BARO_INC()
	FNX_TWIST("E_FCU_EFIS1_BARO", 1)
end

function FNX_BARO_DEC()
	FNX_TWIST("E_FCU_EFIS1_BARO", -1)
end

function FNX_BARO_INC_FAST()
	FNX_TWIST("E_FCU_EFIS1_BARO", 5)
end

function FNX_BARO_DEC_FAST()
	FNX_TWIST("E_FCU_EFIS1_BARO", -5)
end

function FNX_NDZOOM_INC()
	FNX_TWIST("S_FCU_EFIS1_ND_ZOOM", 1, 5)
end

function FNX_NDZOOM_DEC()
	FNX_TWIST("S_FCU_EFIS1_ND_ZOOM", -1, 0)
end

-----------------------------------------
-- $$ FCU

function FNX_FCU_SPD_MACH()
	FNX_BTN_PRESS("S_FCU_SPD_MACH")
end

function FNX_FCU_SPD_INC()
	FNX_TWIST("E_FCU_SPEED", 1)
end

function FNX_FCU_SPD_DEC()
	FNX_TWIST("E_FCU_SPEED", -1)
end

function FNX_FCU_SPD_INC_FAST()
	FNX_TWIST("E_FCU_SPEED", 10)
end

function FNX_FCU_SPD_DEC_FAST()
	FNX_TWIST("E_FCU_SPEED", -10)
end

function FNX_FCU_HDGTRAK()
	FNX_BTN_PRESS("S_FCU_HDGVS_TRKFPA")
end

function FNX_FCU_HDG_INC()
	FNX_TWIST("E_FCU_HEADING", 1)
end

function FNX_FCU_HDG_DEC()
	FNX_TWIST("E_FCU_HEADING", -1)
end

function FNX_FCU_HDG_INC_FAST()
	FNX_TWIST("E_FCU_HEADING", 12)
end

function FNX_FCU_HDG_DEC_FAST()
	FNX_TWIST("E_FCU_HEADING", -12)
end

function FNX_FCU_ALT_INC()
	FNX_TWIST("E_FCU_ALTITUDE", 1)
end

function FNX_FCU_ALT_DEC()
	FNX_TWIST("E_FCU_ALTITUDE", -1)
end

function FNX_FCU_ALT_INC_FAST()
	FNX_TWIST("E_FCU_ALTITUDE", 5)
end

function FNX_FCU_ALT_DEC_FAST()
	FNX_TWIST("E_FCU_ALTITUDE", -5)
end

function FNX_FCU_VS_INC()
	FNX_TWIST("E_FCU_VS", 1)
end

function FNX_FCU_VS_DEC()
	FNX_TWIST("E_FCU_VS", -1)
end

function FNX_FCU_VS_INC_FAST()
	FNX_TWIST("E_FCU_VS", 5)
end

function FNX_FCU_VS_DEC_FAST()
	FNX_TWIST("E_FCU_VS", -5)
end

function FNX_FCU_VS_PUSH()
	FNX_TWIST("S_FCU_VERTICAL_SPEED", -1)
end

function FNX_FCU_VS_PULL()
	FNX_TWIST("S_FCU_VERTICAL_SPEED", 1)
end

-----------------------------------------
-- $$ MIP

function FNX_ND_WX_BRIGHT_CP_INC()
	FNX_TWIST("A_DISPLAY_BRIGHTNESS_CI_OUTER", 0.1, 1.0)
end

function FNX_ND_WX_BRIGHT_CP_DEC()
	FNX_TWIST("A_DISPLAY_BRIGHTNESS_CI_OUTER", -0.1, 0)
end

function FNX_PFD_BRIGHT_CP_INC()
	FNX_TWIST("A_DISPLAY_BRIGHTNESS_CO", 0.1, 1.0)
end

function FNX_PFD_BRIGHT_CP_DEC()
	FNX_TWIST("A_DISPLAY_BRIGHTNESS_CO", -0.1, 0)
end


function FNX_MIP_CLOCK_RUN_TGL()
	local lvar = "S_MIP_CLOCK_ET"
	local pos = ipc.readLvar(lvar)
	if pos == 1 then
		ipc.writeLvar(lvar, 0)
	else
		ipc.writeLvar(lvar, 1)
	end
end

function FNX_MIP_CLOCK_RST_TGL()
	local lvar = "S_MIP_CLOCK_ET"
	ipc.writeLvar(lvar, 2)
	ipc.sleep(175)
	ipc.writeLvar(lvar, 1)
end

function FNX_ISIS_INC()
	FNX_TWIST("E_MIP_ISFD_BARO", 1)
end

function FNX_ISIS_DEC()
	FNX_TWIST("E_MIP_ISFD_BARO", -1)
end

-----------------------------------------
-- $$ Pedestal

function FNX_RMP1_OUTER_INC()
	FNX_TWIST("E_PED_RMP1_OUTER", 1)
end

function FNX_RMP1_OUTER_DEC()
	FNX_TWIST("E_PED_RMP1_OUTER", -1)
end

function FNX_RMP1_INNER_INC()
	FNX_TWIST("E_PED_RMP1_INNER", 1)
end

function FNX_RMP1_INNER_DEC()
	FNX_TWIST("E_PED_RMP1_INNER", -1)
end

function FNX_RMP1_OUTER_INCFAST()
	FNX_TWIST("E_PED_RMP1_OUTER", 5)
end

function FNX_RMP1_OUTER_DECFAST()
	FNX_TWIST("E_PED_RMP1_OUTER", -5)
end

function FNX_RMP1_INNER_INCFAST()
	FNX_TWIST("E_PED_RMP1_INNER", 10)
end

function FNX_RMP1_INNER_DECFAST()
	FNX_TWIST("E_PED_RMP1_INNER", -10)
end

function FNX_ENG_MODE_CRANK()
	ipc.writeLvar("S_ENG_MODE", 0)
end

function FNX_ENG_MODE_NORM()
	ipc.writeLvar("S_ENG_MODE", 1)
end

function FNX_ENG_MODE_IGN()
	ipc.writeLvar("S_ENG_MODE", 2)
end

function FNX_ENG_ATHR_DISC1()
	FNX_BTN_PRESS("S_FC_THR_INST_DISCONNECT1")
end

function FNX_ENG_MASTER1_ON()
	local lvar = "S_ENG_MASTER_1"
	if ipc.readLvar(lvar) ~= 1 then
		ipc.writeLvar(lvar, 1)
	end
end

function FNX_ENG_MASTER1_OFF()
	local lvar = "S_ENG_MASTER_1"
	if ipc.readLvar(lvar) ~= 0 then
		ipc.writeLvar(lvar, 0)
	end
end

function FNX_ENG_MASTER2_ON()
	local lvar = "S_ENG_MASTER_2"
	if ipc.readLvar(lvar) ~= 1 then
		ipc.writeLvar(lvar, 1)
	end
end

function FNX_ENG_MASTER2_OFF()
	local lvar = "S_ENG_MASTER_2"
	if ipc.readLvar(lvar) ~= 0 then
		ipc.writeLvar(lvar, 0)
	end
end

function FNX_SPEEDBRK_TGL()
	local lvar = "A_FC_SPEEDBRAKE"
	local pos = ipc.readLvar(lvar)
	
	if pos ~= 0 then
		ipc.writeLvar(lvar, 0)
	else
		ipc.writeLvar(lvar, 1)
	end
end

function FNX_RUDDER_RST()
	FNX_BTN_PRESS("S_FC_RUDDER_TRIM_RESET")
end

function FNX_RUDDER_INC()
	FNX_BTN_PRESS("S_FC_RUDDER_TRIM", 150, 2, 1)
end

function FNX_RUDDER_DEC()
	FNX_BTN_PRESS("S_FC_RUDDER_TRIM", 150, 0, 1)
end

function FNX_WX_GAIN_INC()
	FNX_TWIST("A_WR_GAIN", 1, 4)
end

function FNX_WX_GAIN_DEC()
	FNX_TWIST("A_WR_GAIN", -1, -5)
end

function FNX_WX_TILT_INC()
	FNX_TWIST("A_WR_TILT", 1)
end

function FNX_WX_TILT_DEC()
	FNX_TWIST("A_WR_TILT", -1)
end

function FNX_WX_MODE_SEQ()
	FNX_SEQUENCE("S_WR_MODE", 3, 0)
end

function FNX_XPDR_THRT_SEQ()
	FNX_SEQUENCE("S_TCAS_RANGE", 3, 0)
end

function FNX_TRIM_WHEEL_INC()
	FNX_TWIST("A_FC_ELEVATOR_TRIM", 5)
end

function FNX_TRIM_WHEEL_DEC()
	FNX_TWIST("A_FC_ELEVATOR_TRIM", -5)
end

function FNX_SIDE_CAPT_DISC()
	FNX_BTN_PRESS("S_FC_CAPT_INST_DISCONNECT")
end

-----------------------------------------
-- $$ MCDU

function FNX_MCDU_AOC1()
	FNX_BTN_PRESS("S_CDU1_KEY_MENU")
	ipc.sleep(100)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK2L")
	ipc.sleep(1250)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK1R")
end

function FNX_MCDU_AOC2()
	FNX_BTN_PRESS("S_CDU2_KEY_MENU")
	ipc.sleep(100)
	FNX_BTN_PRESS("S_CDU2_KEY_LSK2L")
	ipc.sleep(1250)
	FNX_BTN_PRESS("S_CDU2_KEY_LSK1R")
end

function FNX_MCDU_WXREQ()
	FNX_MCDU_AOC1()
	ipc.sleep(100)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK1R")
	ipc.sleep(100)
	FNX_BTN_PRESS("S_CDU1_KEY_LSK1L")
end

-----------------------------------------
-----------------------------------------
-- $$ EVENT FLAGS


event.flag(1, "FNX_OVH_PACK_FLOW")
event.flag(2, "FNX_FIRE_TEST")
event.flag(3, "FNX_LT_STROBE_ON_TGL")
event.flag(4, "FNX_LT_STROBE_OFF_TGL")
event.flag(5, "FNX_LT_LAND_ON_TGL")
event.flag(6, "FNX_LT_LAND_OFF_TGL")
event.flag(7, "FNX_LT_NOSE_TO_TGL")
event.flag(8, "FNX_LT_NOSE_ON_TGL")
event.flag(9, "FNX_LT_DOME_BRT_TGL")
event.flag(10, "FNX_LT_DOME_DIM_TGL")
event.flag(11, "FNX_WIPER_CAPT")
event.flag(12, "FNX_MASTER_CLEAR")
event.flag(13, "FNX_BARO_INC")
event.flag(14, "FNX_BARO_DEC")
event.flag(15, "FNX_BARO_INC_FAST")
event.flag(16, "FNX_BARO_DEC_FAST")
event.flag(17, "FNX_NDZOOM_INC")
event.flag(18, "FNX_NDZOOM_DEC")
event.flag(19, "FNX_FCU_SPD_MACH")
event.flag(21, "FNX_FCU_SPD_INC")
event.flag(22, "FNX_FCU_SPD_DEC")
event.flag(23, "FNX_FCU_SPD_INC_FAST")
event.flag(24, "FNX_FCU_SPD_DEC_FAST")
event.flag(25, "FNX_FCU_HDGTRAK")
event.flag(26, "FNX_FCU_HDG_INC")
event.flag(27, "FNX_FCU_HDG_DEC")
event.flag(28, "FNX_FCU_HDG_INC_FAST")
event.flag(29, "FNX_FCU_HDG_DEC_FAST")
-- event.flag(30, "FNX_FCU_ALTSCALE")
event.flag(31, "FNX_FCU_ALT_INC")
event.flag(32, "FNX_FCU_ALT_DEC")
event.flag(33, "FNX_FCU_ALT_INC_FAST")
event.flag(34, "FNX_FCU_ALT_DEC_FAST")
event.flag(35, "FNX_FCU_VS_INC")
event.flag(36, "FNX_FCU_VS_DEC")
event.flag(37, "FNX_FCU_VS_INC_FAST")
event.flag(38, "FNX_FCU_VS_DEC_FAST")
event.flag(39, "FNX_ND_WX_BRIGHT_CP_INC")
event.flag(40, "FNX_ND_WX_BRIGHT_CP_DEC")
event.flag(41, "FNX_MIP_CLOCK_RUN_TGL")
event.flag(42, "FNX_MIP_CLOCK_RST_TGL")
event.flag(43, "FNX_ISIS_INC")
event.flag(44, "FNX_ISIS_DEC")
event.flag(45, "FNX_RMP1_OUTER_INC")
event.flag(46, "FNX_RMP1_OUTER_DEC")
event.flag(47, "FNX_RMP1_INNER_INC")
event.flag(48, "FNX_RMP1_INNER_DEC")
event.flag(49, "FNX_RMP1_OUTER_INCFAST")
event.flag(50, "FNX_RMP1_OUTER_DECFAST")
event.flag(51, "FNX_RMP1_INNER_INCFAST")
event.flag(52, "FNX_RMP1_INNER_DECFAST")
event.flag(53, "FNX_ENG_MODE_CRANK")
event.flag(54, "FNX_ENG_MODE_NORM")
event.flag(55, "FNX_ENG_MODE_IGN")
event.flag(56, "FNX_ENG_ATHR_DISC1")
event.flag(57, "FNX_ENG_MASTER1_ON")
event.flag(58, "FNX_ENG_MASTER1_OFF")
event.flag(59, "FNX_ENG_MASTER2_ON")
event.flag(60, "FNX_ENG_MASTER2_OFF")
event.flag(61, "FNX_SPEEDBRK_TGL")
event.flag(63, "FNX_RUDDER_RST")
event.flag(64, "FNX_RUDDER_INC")
event.flag(65, "FNX_RUDDER_DEC")
event.flag(66, "FNX_WX_GAIN_INC")
event.flag(67, "FNX_WX_GAIN_DEC")
event.flag(68, "FNX_WX_TILT_INC")
event.flag(69, "FNX_WX_TILT_DEC")
event.flag(70, "FNX_WX_MODE_SEQ")
event.flag(71, "FNX_XPDR_THRT_SEQ")
event.flag(72, "FNX_FCU_VS_PUSH")
event.flag(73, "FNX_FCU_VS_PULL")
event.flag(74, "FNX_MCDU_AOC1")
event.flag(75, "FNX_MCDU_AOC2")
event.flag(76, "FNX_MCDU_WXREQ")
event.flag(77, "FNX_TRIM_WHEEL_INC")
event.flag(78, "FNX_TRIM_WHEEL_DEC")
event.flag(79, "FNX_SIDE_CAPT_DISC")
event.flag(80, "FNX_PFD_BRIGHT_CP_INC")
event.flag(81, "FNX_PFD_BRIGHT_CP_DEC")