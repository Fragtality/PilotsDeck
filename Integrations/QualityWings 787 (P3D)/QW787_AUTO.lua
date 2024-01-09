---@diagnostic disable: undefined-global

-----------------------------------------
-- $$ CONFIG
local qwSyncP2Axpdr = true		--Sync Transponder state to P2A, requires F20 as Hotkey to be set
local qwInitAItoggle = false	--Toggle AI traffic off/on in Init Function, requires ctrl+x as Hotkey to be set (Traffic Density Toggle)
local qwInitBaroHPA = true		--Set Baro to hPa during in Init Function
local qwInitGSX = true			--Do GSX Intialization (Reposition, Connect/Power, Door L1) - GSX_AUTO and QW787_SYNC Scripts need to be running

-----------------------------------------
-- $$ Base Functions

function QW_NOP()
end

function QW_BTN_PRESS(lvar)
	ipc.writeLvar(lvar, 1)
	ipc.sleep(150)
	ipc.writeLvar(lvar, 0)
end

function QW_REPEAT_MACRO(macro, steps)
	for i=1,steps do
	ipc.macro(macro)
	ipc.sleep(100)
	end
end

function QW_REPEAT_CONTROL(control, steps)
	for i=1,steps do
	ipc.control(control)
	ipc.sleep(75)
	end
end

-----------------------------------------
-- $$ ENGINE MANAGEMENT

function QW_ENG_L_START_SW()
	local lvar = "QW_OH_ENG_L_START_Switch"
	local state = ipc.readLvar(lvar)

	if state == 1 then
		ipc.writeLvar(lvar, 0)
	else
		ipc.writeLvar(lvar, 1)
	end
end

function QW_ENG_R_START_SW()
	local lvar = "QW_OH_ENG_R_START_Switch"
	local state = ipc.readLvar(lvar)

	if state == 1 then
		ipc.writeLvar(lvar, 0)
	else
		ipc.writeLvar(lvar, 1)
	end
end

-----------------------------------------
-- $$ APU

function QW_APU_TOGGLE()
	local lvar = "QW_OH_ELE_APU_Switch"
	local state = ipc.readLvar(lvar)

	if state == 0 then
		ipc.writeLvar(lvar, 1)
		ipc.sleep(300)
		ipc.writeLvar(lvar, 2)
		ipc.sleep(300)
	elseif state == 1 then
		ipc.writeLvar(lvar, 0)
	end
end

-----------------------------------------
-- $$ LIGHTS

function QW_LT_SEAT_CYCLE()
	local lvar = "QW_OH_SEATB_SIGN_Switch"
	local state = ipc.readLvar(lvar)

	if state == 0 then
		ipc.writeLvar(lvar, 1)
	elseif state == 1 then
		ipc.writeLvar(lvar, 2)
	elseif state == 2 then
		ipc.writeLvar(lvar, 1)
		ipc.sleep(250)
		ipc.writeLvar(lvar, 0)
	end
end

function QW_LT_EMER_CYCLE()
	local lvar = "QW_OH_EMER_LTS_Switch"
	local state = ipc.readLvar(lvar)

	if state == -1 then
		ipc.writeLvar(lvar, 0)
		ipc.sleep(200)
		ipc.writeLvar("QW_OH_EMER_LTS_Safety_Switch", 0)
	elseif state == 0 then
		ipc.writeLvar("QW_OH_EMER_LTS_Safety_Switch", 1)
		ipc.sleep(200)
		ipc.writeLvar(lvar, -1)
	end
end

-----------------------------------------
-- $$ AUTOBRAKE

function QW_AUTOBRAKE_CYCLE()
	local lvar = "auto_brake_switch"
	local pos = ipc.readLvar(lvar)
	local ground = ipc.readSW(0x0366)

	if pos == -1 then
		ipc.writeLvar(lvar, 1)
		ipc.sleep(100)
		ipc.writeLvar(lvar, 0)
	elseif pos == 0 and ground == 1 then
		ipc.writeLvar(lvar, -1)
	elseif pos == 0 and ground == 0 then
		ipc.writeLvar(lvar, 2)
	elseif pos == 1 then
		ipc.writeLvar(lvar, 0)
	elseif pos > 1 and pos < 6 then
		ipc.writeLvar(lvar, pos + 1)
	elseif pos == 6 then
		ipc.writeLvar(lvar, 1)
		ipc.sleep(100)
		ipc.writeLvar(lvar, 0)
	end
end

-----------------------------------------
-- $$ MPL / GLARE


function QW_MPL_MCP_MINS_MODE()
	local lvar = "QW_MCP_L_RADIO_BARO_Knob"
	local state = ipc.readLvar(lvar)

	if state == 1 then
		ipc.writeLvar(lvar, 0)
	else
		ipc.writeLvar(lvar, 1)
	end
end

function QW_MPL_MCP_MINS_INC_100()
	QW_REPEAT_MACRO("QW787_MAIN: QW_MPL_DH_INC",20)
end

function QW_MPL_MCP_MINS_DEC_100()
	QW_REPEAT_MACRO("QW787_MAIN: QW_MPL_DH_DEC",20)
end

function QW_MPL_BARO_UNIT_MODE()
	local lvar = "QW_MCP_L_BAROSET_Knob"
	local state = ipc.readLvar(lvar)

	if state == 1 then
		ipc.writeLvar(lvar, 0)
		ipc.sleep(150)
		--ipc.macro("QW787_MAIN: QW_MPL_SBARO_MD")
		ipc.writeLvar("QW_MAIN_SPFD_HPIN_Button", 1)
	else
		ipc.writeLvar(lvar, 1)
		ipc.sleep(150)
		ipc.macro("QW787_MAIN: QW_MPL_SBARO_MD")
		--ipc.writeLvar("QW_MAIN_SPFD_HPIN_Button", 1)
	end
end

-----------------------------------------
-- $$ XPDR/TCAS

function QW_XPDR_CYCLE(value)
	local lvar = "QW_AFT_Transponder_Knob"
	local pos = value or ipc.readLvar(lvar)

	if pos == 0 then
		ipc.writeLvar(lvar, 2)
		if qwSyncP2Axpdr then
			ipc.keypress(131) --F20
			ipc.keypress(131) --F20
			ipc.keypress(131) --F20
		end
	elseif pos >= 2 then
		ipc.writeLvar(lvar, 0)
		if qwSyncP2Axpdr then
			ipc.keypress(131) --F20
			ipc.keypress(131) --F20
			ipc.keypress(131) --F20
		end
	end
end

function QW_TCAS_CYCLE()
	local lvar = "QW_AFT_Transponder_Knob"
	local pos = ipc.readLvar(lvar)

	if pos == 0 then
		QW_XPDR_CYCLE(pos)
	elseif pos == 2 or pos == 3 then
		ipc.writeLvar(lvar, 4)
	elseif pos == 4 then
		ipc.writeLvar(lvar, 2)
	end
end

-----------------------------------------
-- $$ MCP/FCU

function QW_MCP_SPD_INC_FAST()
	--QW_REPEAT_MACRO("QW787_MAIN: QW_MCP_SPD_INC",10)
	QW_REPEAT_CONTROL(65896, 20)
end

function QW_MCP_SPD_DEC_FAST()
	--QW_REPEAT_MACRO("QW787_MAIN: QW_MCP_SPD_DEC",10)
	QW_REPEAT_CONTROL(65897, 20)
end

function QW_MCP_HDG_INC_FAST()
	QW_REPEAT_MACRO("QW787_MAIN: QW_MCP_HDG_INC",15)
	-- QW_REPEAT_CONTROL(65879, 20)
end

function QW_MCP_HDG_DEC_FAST()
	QW_REPEAT_MACRO("QW787_MAIN: QW_MCP_HDG_DEC",15)
	-- QW_REPEAT_CONTROL(65880, 20)
end

function QW_MCP_ALT_INC_FAST()
	QW_REPEAT_MACRO("QW787_MAIN: QW_MCP_ALT_INC",6)
	-- if ipc.readLvar("QW_MCP_ALT_AUTO_Knob") == 0 then
	-- 	QW_REPEAT_CONTROL(65892, 5)
	-- else
	-- 	QW_REPEAT_CONTROL(65892, 50)
	-- end
end

function QW_MCP_ALT_DEC_FAST()
	QW_REPEAT_MACRO("QW787_MAIN: QW_MCP_ALT_DEC",6)
	-- if ipc.readLvar("QW_MCP_ALT_AUTO_Knob") == 0 then
	-- 	QW_REPEAT_CONTROL(65893, 5)
	-- else
	-- 	QW_REPEAT_CONTROL(65893, 50)
	-- end
end

function QW_MCP_VS_INC_FAST()
	QW_REPEAT_CONTROL(65894, 5)
end

function QW_MCP_VS_DEC_FAST()
	QW_REPEAT_CONTROL(65895, 58)
end

function QW_AP_DISCONNECT()
	ipc.writeLvar("ap_master_disconnect",1)
	ipc.sleep(200)
	ipc.writeLvar("ap_master_disconnect",0)
end

function QW_GLS_CHRONO()
	QW_BTN_PRESS("QW_MCP_Clock_L_Button")
end

function QW_AP_HDG_KNOB()
	local lat_mode = ipc.readLvar("ap_lat_mode")
	if lat_mode == 3 then
		ipc.control(65725) --hdg hold
	else
		ipc.control(65798) --hdg sel
	end
end

function QW_AP_HDG_SELLNAV_TOGGLE()
	local lat_mode = ipc.readLvar("ap_lat_mode")
	if lat_mode == 6 then
		ipc.control(65798) --hdg sel
	else
		ipc.control(65729) --lnav
	end
end

function QW_AP_ALT_KNOB()
	local vert_mode = ipc.readLvar("ap_vert_mode")
	if vert_mode ~= 6 then
		ipc.control(65722) --flch
	else
		ipc.control(65808) --push knob
	end
end

function QW_AP_ALT_ALTVNAV_TOGGLE()
	local vert_mode = ipc.readLvar("ap_vert_mode")
	if vert_mode == 6 then
		ipc.control(65722) --flch
	else
		ipc.control(65727) --vnav
	end
end

function QW_LT_RWY_TURN_BOTH()
	if ipc.readLvar("QW_OH_LT_L_RWYTF_Switch") == 1 then
		ipc.writeLvar("QW_OH_LT_L_RWYTF_Switch", 0)
		ipc.writeLvar("QW_OH_LT_R_RWYTF_Switch", 0)
	else
		ipc.writeLvar("QW_OH_LT_L_RWYTF_Switch", 1)
		ipc.writeLvar("QW_OH_LT_R_RWYTF_Switch", 1)
	end
end

-----------------------------------------
-- $$ INIT SCRIPT

function QW_INIT()
	if logic.And(ipc.readUB(0x0D0C), 1) ~= 0 then --NAV lights
		return
	end

	--Test Light Fix
	ipc.writeLvar("QW_annun_ovhd_test", 0)
	ipc.sleep(250)

	--Fuel
	ipc.writeUD(0x0B74, 0) -- center tank 0%
	ipc.writeUD(0x0B7C, 125830) --left wing tank ==> 1.5% => 0,015 * 128 * 65536
	ipc.writeUD(0x0B94, 125830)  --right wing tank 1.5%
	ipc.sleep(1000)

	--Power / Chocks
	if qwInitGSX then
		ipc.writeLvar("QW_OH_TOWER_PWR_Button", 1) --disable Chock/Ext Pwr Sync
	end
	ipc.sleep(250)
	ipc.writeLvar("QW_OH_ELE_BAT_Button", 1)
	ipc.sleep(1000)

	ipc.writeLvar("QW_WheelChocks",1)
	ipc.sleep(1000)

	--reset position at current gate
	if qwInitGSX then
		ipc.keypress(123,11) --gsx menu
		ipc.sleep(500)
		ipc.control(67145)	--0
		ipc.sleep(500)
		ipc.control(67136)	--1
		ipc.sleep(4000)
	end

	--request Ground Services (Jetways or GPU)
	if qwInitGSX then
		ipc.writeLvar("GSX_AUTO_CONNECT_REQUESTED", 1)
		ipc.sleep(5000)
	end

	--Interior Lights
	ipc.writeLvar("QW_OH_DOME_Knob", 1)
	ipc.sleep(300)
	ipc.writeLvar("QW_OH_TEXT", 1)
	ipc.sleep(300)
	ipc.writeLvar("QW_MCP_TEXT", 1)
	ipc.sleep(300)
	ipc.writeLvar("QW_MAIN_TEXT", 1)
	ipc.sleep(300)
	ipc.writeLvar("QW_AFT_TEXT", 1)
	ipc.sleep(300)
	ipc.writeLvar("QW_MCPFlood", 3)
	ipc.sleep(300)
	ipc.writeLvar("QW_MAINFlood", 2)
	ipc.sleep(300)
	ipc.writeLvar("QW_AftFlood", 2)
	ipc.sleep(300)

	--Baro to hPA
	if qwInitBaroHPA then
		ipc.writeLvar("QW_MCP_L_BAROSET_Knob", 1)
		ipc.sleep(150)
		ipc.macro("QW787_MAIN: QW_MPL_SBARO_MD")
		ipc.sleep(150)
	end

	--Alt Selection to 1000's
	ipc.writeLvar("QW_MCP_ALT_AUTO_Knob", 1)
	ipc.sleep(150)

	--Wait for Jetway/GPU for "Power" and Power ON
	while qwInitGSX and ipc.readLvar("GSX_AUTO_CONNECTED") ~= 1 do
		ipc.sleep(1000)
	end
	if qwInitGSX then
		ipc.writeLvar("QW_OH_TOWER_PWR_Button", 0) --enable Chock/Ext Pwr Sync - will be set by QW787_Sync
		ipc.sleep(500)
	end

	ipc.writeLvar("QW_OH_FWDEXTPWR_LEFT_Button", 1)
	ipc.sleep(1000)

	--Transponder 2000
	ipc.writeUW(0x0354, 0x2000)
	ipc.sleep(1000)

	--AI Toggle off/on
	if qwInitAItoggle then
		ipc.keypress(88,10)	--ctrl+x | fsuipc density toggle
		ipc.sleep(2000)
		ipc.keypress(88,10) --ctrl+x | fsuipc density toggle
		ipc.sleep(500)
	end

	--Open Main Door (only with Jetway because of GSX ...)
	if qwInitGSX then
		if ipc.readLvar("FSDT_GSX_JETWAY") ~= 2 then
			ipc.control(66389, 1) -- L1
			ipc.writeLvar("DoorL1", 1)
			ipc.sleep(500)
		end
	end

	--NAV Lights
	ipc.control(66379)
end

-----------------------------------------
-----------------------------------------
-- $$ EVENT FLAGS

event.flag(1, "QW_NOP")
event.flag(2, "QW_ENG_L_START_SW")
event.flag(3, "QW_ENG_R_START_SW")
event.flag(4, "QW_APU_TOGGLE")
event.flag(5, "QW_LT_SEAT_CYCLE")
event.flag(6, "QW_LT_EMER_CYCLE")
event.flag(7, "QW_AUTOBRAKE_CYCLE")
event.flag(8, "QW_MPL_MCP_MINS_MODE")
event.flag(9, "QW_MPL_MCP_MINS_INC_100")
event.flag(10, "QW_MPL_MCP_MINS_DEC_100")
event.flag(11, "QW_MPL_BARO_UNIT_MODE")
event.flag(12, "QW_XPDR_CYCLE")
event.flag(13, "QW_TCAS_CYCLE")
event.flag(14, "QW_MCP_SPD_INC_FAST")
event.flag(15, "QW_MCP_SPD_DEC_FAST")
event.flag(16, "QW_MCP_HDG_INC_FAST")
event.flag(17, "QW_MCP_HDG_DEC_FAST")
event.flag(18, "QW_MCP_ALT_INC_FAST")
event.flag(19, "QW_MCP_ALT_DEC_FAST")
event.flag(20, "QW_MCP_VS_INC_FAST")
event.flag(21, "QW_MCP_VS_DEC_FAST")
event.flag(22, "QW_AP_DISCONNECT")
event.flag(23, "QW_GLS_CHRONO")
event.flag(24, "QW_AP_HDG_KNOB")
event.flag(25, "QW_AP_HDG_SELLNAV_TOGGLE")
event.flag(26, "QW_AP_ALT_KNOB")
event.flag(27, "QW_AP_ALT_ALTVNAV_TOGGLE")
event.flag(28, "QW_LT_RWY_TURN_BOTH")

event.flag(254, "QW_INIT")