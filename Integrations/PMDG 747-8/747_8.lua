PMDGID = 69632

function LIGHTS_TAXI ()
	if ipc.readLvar("switch_228_74X") == 0 then
	ipc.control(PMDGID + 228, 1) else
	ipc.control(PMDGID + 228, 0)
	end
end	

function LIGHTS_Outbd_L ()
	if ipc.readLvar("switch_222_74X") == 0 then
	ipc.control(PMDGID + 222, 1) else
	ipc.control(PMDGID + 222, 0)
	end
end	

function LIGHTS_Outbd_R ()
	if ipc.readLvar("switch_223_74X") == 0 then
	ipc.control(PMDGID + 223, 1) else
	ipc.control(PMDGID + 223, 0)
	end
end	

function LIGHTS_INBD_L ()
	if ipc.readLvar("switch_224_74X") == 0 then
	ipc.control(PMDGID + 224, 1) else
	ipc.control(PMDGID + 224, 0)
	end
end	

function LIGHTS_INBD_R ()
	if ipc.readLvar("switch_225_74X") == 0 then
	ipc.control(PMDGID + 225, 1) else
	ipc.control(PMDGID + 225, 0)
	end
end	

function RWY_L_TURNOFF ()
	if ipc.readLvar("switch_226_74X") == 0 then
	ipc.control(PMDGID + 226, 1) else
	ipc.control(PMDGID + 226, 0)
	end
end	

function RWY_R_TURNOFF ()
	if ipc.readLvar("switch_227_74X") == 0 then
	ipc.control(PMDGID + 227, 1) else
	ipc.control(PMDGID + 227, 0)
	end
end	

function LIGHTS_BEACON ()
	if ipc.readLvar("switch_230_74X") == 50 then
	ipc.control(PMDGID + 230, 2) else
	ipc.control(PMDGID + 230, 1)
	end
end	

function LIGHTS_NAV ()
	if ipc.readLvar("switch_231_74X") == 0 then
	ipc.control(PMDGID + 231, 1) else
	ipc.control(PMDGID + 231, 0)
	end
end	

function LIGHTS_STRB ()
	if ipc.readLvar("switch_232_74X") == 0 then
	ipc.control(PMDGID + 232, 1) else
	ipc.control(PMDGID + 232, 0)
	end
end	

function LIGHTS_WING ()
	if ipc.readLvar("switch_233_74X") == 0 then
	ipc.control(PMDGID + 233, 1) else
	ipc.control(PMDGID + 233, 0)
	end
end	

function LIGHTS_LOGO ()
	if ipc.readLvar("switch_234_74X") == 0 then
	ipc.control(PMDGID + 234, 1) else
	ipc.control(PMDGID + 234, 0)
	end
end	

function FUEL_PUMP_FWD_1 ()
if ipc.readLvar("switch_116_74X") == 0 then
	ipc.control(PMDGID + 116, 1) else
	ipc.control(PMDGID + 116, 0)
	end
end	

function FUEL_PUMP_AFT_1 ()
if ipc.readLvar("switch_122_74X") == 0 then
	ipc.control(PMDGID + 122, 1) else
	ipc.control(PMDGID + 122, 0)
	end
end	

function FUEL_PUMP_FWD_2 ()
if ipc.readLvar("switch_117_74X") == 0 then
	ipc.control(PMDGID + 117, 1) else
	ipc.control(PMDGID + 117, 0)
	end
end	

function FUEL_PUMP_AFT_2 ()
if ipc.readLvar("switch_123_74X") == 0 then
	ipc.control(PMDGID + 123, 1) else
	ipc.control(PMDGID + 123, 0)
	end
end	

function FUEL_PUMP_FWD_3 ()
if ipc.readLvar("switch_118_74X") == 0 then
	ipc.control(PMDGID + 118, 1) else
	ipc.control(PMDGID + 118, 0)
	end
end	

function FUEL_PUMP_AFT_3 ()
if ipc.readLvar("switch_124_74X") == 0 then
	ipc.control(PMDGID + 124, 1) else
	ipc.control(PMDGID + 124, 0)
	end
end	

function FUEL_PUMP_FWD_4 ()
if ipc.readLvar("switch_119_74X") == 0 then
	ipc.control(PMDGID + 119, 1) else
	ipc.control(PMDGID + 119, 0)
	end
end	

function FUEL_PUMP_AFT_4 ()
if ipc.readLvar("switch_125_74X") == 0 then
	ipc.control(PMDGID + 125, 1) else
	ipc.control(PMDGID + 125, 0)
	end
end	

function Battery ()
if ipc.readLvar("switch_018_74X") == 0 then
	ipc.control(PMDGID + 018, 1) else
	ipc.control(PMDGID + 018, 0)
	end
end	

function OH_ELEC_BATTERY_GUARD_open ()
    ipc.control(PMDGID + 10018, 1)
end


function HYDRAULIC_1 ()
hyd1=ipc.readLvar("switch_048_74X")
if  hyd1 == 10 then
	ipc.control(PMDGID + 48, 2) 
	elseif
	hyd1 == 20  
	then
	ipc.control(PMDGID + 48, 0) 
	else
	ipc.control(PMDGID + 48, 1)
	end
end
function HYDRAULIC_2 ()
hyd1=ipc.readLvar("switch_049_74X")
if  hyd1 == 10 then
	ipc.control(PMDGID + 49, 2) 
	elseif
	hyd1 == 20  
	then
	ipc.control(PMDGID + 49, 0) 
	else
	ipc.control(PMDGID + 49, 1)
	end
end

function HYDRAULIC_3 ()
hyd1=ipc.readLvar("switch_050_74X")
if  hyd1 == 10 then
	ipc.control(PMDGID + 50, 2) 
	elseif
	hyd1 == 20  
	then
	ipc.control(PMDGID + 50, 0) 
	else
	ipc.control(PMDGID + 50, 1)
	end
end

function HYDRAULIC_4 ()
hyd1=ipc.readLvar("switch_051_74X")
if  hyd1 == 10 then
	ipc.control(PMDGID + 51, 2) 
	elseif
	hyd1 == 20  
	then
	ipc.control(PMDGID + 51, 0) 
	else
	ipc.control(PMDGID + 51, 1)
	end
end

function EXT_PWR1 ()
ExtPwr1 = ipc.readUB(0x6465)
if  ExtPwr1 == 0 then
	ipc.control(PMDGID + 014, 1) else
	ipc.control(PMDGID + 014, 1)
	end
end	

function EXT_PWR2 ()
ExtPwr2 = ipc.readUB(0x6466)
if  ExtPwr1 == 0 then
	ipc.control(PMDGID + 015, 1) else
	ipc.control(PMDGID + 015, 1)
	end
end	

function APU_PWR1 ()
ApuPwr1 = ipc.readUB(0x646b)
if  ApuPwr1 == 0 then
	ipc.control(PMDGID + 016, 1) else
	ipc.control(PMDGID + 016, 1)
	end
end	

function APU_PWR2 ()
ApuPwr2 = ipc.readUB(0x646c)
if  ApuPwr2 == 0 then
	ipc.control(PMDGID + 017, 1) else
	ipc.control(PMDGID + 017, 1)
	end
end	

function Apu_on_off ()
if ipc.readLvar("switch_013_74X") == 0 then
	ipc.control(PMDGID + 13, 1) else
	ipc.control(PMDGID + 13, 0)
	end
end	

function Apu_start()
ipc.control(PMDGID + 13, 2) 
ipc.sleep(6000)
ipc.control(PMDGID + 13, 1)

end

function ENGINE_1_START ()
if ipc.readLvar("switch_091_74X") == 0 then
	ipc.control(PMDGID + 91, 1) else
	ipc.control(PMDGID + 91, 0)
	end
end	

function ENGINE_2_START ()
if ipc.readLvar("switch_092_74X") == 0 then
	ipc.control(PMDGID + 92, 1) else
	ipc.control(PMDGID + 92, 0)
	end
end	

function ENGINE_3_START ()
if ipc.readLvar("switch_093_74X") == 0 then
	ipc.control(PMDGID + 93, 1) else
	ipc.control(PMDGID + 93, 0)
	end
end

function ENGINE_4_START ()
if ipc.readLvar("switch_094_74X") == 0 then
	ipc.control(PMDGID + 94, 1) else
	ipc.control(PMDGID + 94, 0)
	end
end	

function EVT_OH_ICE_WINDOW_HEAT_L ()
if ipc.readLvar("switch_141_74X") == 0 then
	ipc.control(PMDGID + 141, 1) else
	ipc.control(PMDGID + 141, 0)
	end
end	

function EVT_OH_ICE_WINDOW_HEAT_R ()
if ipc.readLvar("switch_142_74X") == 0 then
	ipc.control(PMDGID + 142, 1) else
	ipc.control(PMDGID + 142, 0)
	end
end	

function AIRCOND_PACK_SWITCH_L ()
if ipc.readLvar("switch_190_74X") == 0 then
	ipc.control(PMDGID + 190, 1) else
	ipc.control(PMDGID + 190, 0)
	end
end	

function AIRCOND_PACK_SWITCH_C ()
if ipc.readLvar("switch_192_74X") == 0 then
	ipc.control(PMDGID + 192, 1) else
	ipc.control(PMDGID + 192, 0)
	end
end	

function AIRCOND_PACK_SWITCH_R ()
if ipc.readLvar("switch_191_74X") == 0 then
	ipc.control(PMDGID + 191, 1) else
	ipc.control(PMDGID + 191, 0)
	end
end	

function FUEL_XFEED_1 ()
if ipc.readLvar("switch_110_74X") == 0 then
	ipc.control(PMDGID + 110, 1) else
	ipc.control(PMDGID + 110, 0)
	end
end	

function FUEL_XFEED_4 ()
if ipc.readLvar("switch_113_74X") == 0 then
	ipc.control(PMDGID + 113, 1) else
	ipc.control(PMDGID + 113, 0)
	end
end	

function DOOR_1L ()
if ipc.readUB(0x6C30) == 0 then
	ipc.control(PMDGID + 14011, 1) else
	ipc.control(PMDGID + 14011, 1)
	end
end	

function CARGO_SIDE ()
if ipc.readUB(0x6C3F) == 0 then
	ipc.control(PMDGID + 14026, 1) else
	ipc.control(PMDGID + 14026, 1)
	end
end	

function CARGO_FWD ()
if ipc.readUB(0x6C3C) == 0 then
	ipc.control(PMDGID + 14023, 1) else
	ipc.control(PMDGID + 14023, 1)
	end
end	

function CARGO_AFT ()
if ipc.readUB(0x6C3D) == 0 then
	ipc.control(PMDGID + 14024, 1) else
	ipc.control(PMDGID + 14024, 1)
	end
end	

function CARGO_BULK ()
if ipc.readUB(0x6C3E) == 0 then
	ipc.control(PMDGID + 14025, 1) else
	ipc.control(PMDGID + 14025, 1)
	end
end	

function CARGO_NOSE ()
if ipc.readUB(0x6C40) == 0 then
	ipc.control(PMDGID + 14027, 1) else
	ipc.control(PMDGID + 14027, 1)
	end
end	

function DSP_DOOR ()
	ipc.control(PMDGID + 656, 1) 
end

function DSP_FCTL ()
	ipc.control(PMDGID + 663, 1) 
end

function DSP_CHKL ()
    ipc.control(PMDGID  + 665,1)
end

function DSP_NAV ()
    ipc.control(PMDGID  + 667,1)
end

function DSP_RCL ()
    ipc.control(PMDGID  + 659,1)
end

function DSP_CANC ()
    ipc.control(PMDGID  + 658,1)
end

function DSP_GEAR ()
    ipc.control(PMDGID  + 657,1)
end

function DSP_HYD()
    ipc.control(PMDGID  + 655,1)
end

function DSP_ECS ()
    ipc.control(PMDGID  + 654,1)
end

function DSP_FUEL ()
    ipc.control(PMDGID  + 653,1)
end

function DSP_ELEC()
    ipc.control(PMDGID  + 652,1)
end

function DSP_STAT ()
    ipc.control(PMDGID  + 651,1)
end

function DSP_ENG ()
    ipc.control(PMDGID  + 650,1)
end

function EMER_LIGHT_SWITCH ()
if ipc.readLvar("switch_061_74X") == 0 then
	ipc.control(PMDGID + 61, 1) else
	ipc.control(PMDGID + 61, 0)
	end
end	

function EMER_LIGHT_SWITCH_GUARD ()
if ipc.readLvar("switch_10061_74X") == 0 then
	ipc.control(PMDGID + 10061, 1) else
	ipc.control(PMDGID + 10061, 0)
	end
end	

function IRU_SELECTOR_L ()
if ipc.readUB(0x6446) == 0 then
	ipc.control(PMDGID + 5, 2) else
	ipc.control(PMDGID + 5, 0) 
	end
end

function IRU_SELECTOR_C ()
if ipc.readUB(0x6447) == 0 then
	ipc.control(PMDGID + 6, 2) else
	ipc.control(PMDGID + 6, 0) 
	end
end
	
function IRU_SELECTOR_R ()
if ipc.readUB(0x6448) == 0 then
	ipc.control(PMDGID + 7, 2) else
	ipc.control(PMDGID + 7, 0) 
	end
end

function FD_L ()
if ipc.readLvar("switch_550_74X") == 0 then
	ipc.control(PMDGID + 550, 1) else
	ipc.control(PMDGID + 550, 0)
	end
end	

function FD_R ()
if ipc.readLvar("switch_589_74X") == 0 then
	ipc.control(PMDGID + 589, 1) else
	ipc.control(PMDGID + 589, 0)
	end
end	

function THR ()
ipc.control(PMDGID + 552, 1)
end

function SPD ()
ipc.control(PMDGID + 553, 1)
end	

function LNAV ()
ipc.control(PMDGID + 559, 1)
end	

function VNAV ()
ipc.control(PMDGID + 560, 1)
end	

function LVL_CHG ()
ipc.control(PMDGID + 561, 1)
end	

function APP()
ipc.control(PMDGID + 583, 1)
end	

function LOC ()
ipc.control(PMDGID + 584, 1)
end	

function VS_SWT ()
ipc.control(PMDGID + 575, 1)
end	

function MCP_SPEED_SELECTOR_inc ()
    ipc.control(PMDGID + 554, 256)
end

function MCP_SPEED_SELECTOR_incfast ()
    local i
    for i = 1, 20 do
    ipc.control(PMDGID + 554, 256)
    end
end

function MCP_SPEED_SELECTOR_dec ()
    ipc.control(PMDGID + 554, 128)
end

function MCP_SPEED_SELECTOR_decfast ()
    local i
    for i = 1, 20 do
    ipc.control(PMDGID + 554, 128)
    end
end

function MCP_HDG_SELECTOR_inc ()
    ipc.control(PMDGID + 566, 256)
end

function MCP_HDG_SELECTOR_incfast ()
    local i
    for i = 1, 20 do
    ipc.control(PMDGID + 566, 256)
    end
end

function MCP_HDG_SELECTOR_dec ()
    ipc.control(PMDGID + 566, 128)
end

function MCP_HDG_SELECTOR_decfast ()
    local i
    for i = 1, 20 do
    ipc.control(PMDGID + 566, 128)
    end
end

function MCP_HEADING_PUSH_SWITCH ()
    ipc.control(PMDGID + 10566, 1)
end

function MCP_ALT_SELECTOR_inc ()
    ipc.control(PMDGID + 581, 256)
end

function MCP_ALT_SELECTOR_incfast ()
    local i
    for i = 1, 50 do
    ipc.control(PMDGID + 581, 256)
    end
end

function MCP_ALT_SELECTOR_dec ()
    ipc.control(PMDGID + 581, 128)
end

function MCP_ALT_SELECTOR_decfast ()
    local i
    for i = 1, 50 do
    ipc.control(PMDGID + 581, 128)
    end
end

function MCP_ALT_PUSH_SWITCH ()
    ipc.control(PMDGID + 10581, 1)
end

function MCP_VS_SELECTOR_inc ()
    ipc.control(PMDGID + 574, 128)
end
	
function MCP_VS_SELECTOR_dec ()
    ipc.control(PMDGID + 574, 256)
end

function EFIS_CPT_RANGE_inc ()					--TCA
	ipc.control(PMDGID  + 526,256)
end

function EFIS_CPT_RANGE_dec ()					--TCA
	ipc.control(PMDGID  + 526,128)
end

function Autobrake_rto ()					--TCA
    ipc.control(PMDGID + 1102, 0)
end

function Autobrake_off ()					--TCA
    ipc.control(PMDGID + 1102, 1)
end

function Autobrake_1 ()						--TCA
    ipc.control(PMDGID + 1102, 3)
	ipc.sleep(500)
	ipc.control(PMDGID + 1102, 3)
	
end

function Autobrake_2 ()						--TCA
    ipc.control(PMDGID + 1102, 4)
end

function Autobrake_3 ()						--TCA
    ipc.control(PMDGID + 1102, 5)
end

function Autobrake_4 ()						--TCA
    ipc.control(PMDGID + 1102, 6)
end

function LIGHT_STORM ()
if ipc.readLvar("switch_210_74X") == 0 then
	ipc.control(PMDGID + 210, 1) else
	ipc.control(PMDGID + 210, 0)
	end
end	



function CDU_C_Wheel_Chocks()
CDURstartVar = 1140
	ipc.control(PMDGID +CDURstartVar+ 23, 1)
    ipc.sleep(2500)
    ipc.control(PMDGID +CDURstartVar+ 11, 1)
    ipc.sleep(100)
    ipc.control(PMDGID +CDURstartVar+ 6, 1)
	ipc.sleep(1000)
	ipc.control(PMDGID +CDURstartVar+ 11, 1)
	ipc.sleep(1000)
	ipc.control(PMDGID +CDURstartVar+ 23, 1)

    
end

function CDU_C_GPU()
CDURstartVar = 1140
	ipc.control(PMDGID +CDURstartVar+ 23, 1)
    ipc.sleep(2500)
    ipc.control(PMDGID +CDURstartVar+ 11, 1)
    ipc.sleep(100)
    ipc.control(PMDGID +CDURstartVar+ 6, 1)
	ipc.sleep(1000)
	ipc.control(PMDGID +CDURstartVar+ 1, 1)
	ipc.sleep(1000)
	ipc.control(PMDGID +CDURstartVar+ 23, 1)

    
end

function NOSE_CARGO()
CDURstartVar = 1140
	ipc.control(PMDGID +CDURstartVar+ 23, 1)
    ipc.sleep(2500)
    ipc.control(PMDGID +CDURstartVar+ 11, 1)
    ipc.sleep(100)
    ipc.control(PMDGID +CDURstartVar+ 7, 1)
	ipc.sleep(1000)
	ipc.control(PMDGID +CDURstartVar+ 1, 1)
	ipc.sleep(1000)
	ipc.control(PMDGID +CDURstartVar+ 23, 1)

end

function TCAS_MODE_stby ()
if ipc.readLvar("switch_1296_74X") == 0 then
	ipc.control(PMDGID +1296,3) else
	ipc.control(PMDGID +1296,0)
    
end
end

function MCP_AP_L_SWITCH ()
    ipc.control(PMDGID + 585, 1)
end

function MCP_AP_C_SWITCH ()
    ipc.control(PMDGID + 586, 1)
end

function MCP_AP_R_SWITCH ()
    ipc.control(PMDGID + 587, 1)
end

function EVT_MCP_AT_ARM_SWITCH_toggle ()
if ipc.readLvar("switch_551_74X") == 100 then
 ipc.control(PMDGID + 551, 0) else
 ipc.control(PMDGID + 551, 1)
end
end

function TOGA1_SWITCH ()					--TCA
    ipc.control(PMDGID + 962, 1)
    
end

function WARN_Reset ()
    ipc.control(PMDGID + 509, 1)
  
end

function EFIS_CPT_WXR ()
    ipc.control(PMDGID  + 534,1)
  
end

function EFIS_CPT_STA ()
    ipc.control(PMDGID  + 535,1)
    
end

function EFIS_CPT_WPT ()
    ipc.control(PMDGID  + 536,1)
 
end

function EFIS_CPT_ARPT ()
    ipc.control(PMDGID  + 537,1)
    
end

function EFIS_CPT_DATA ()
    ipc.control(PMDGID  + 538,1)
  
end

function EFIS_CPT_POS ()
    ipc.control(PMDGID  + 539,1)
    
end

function EFIS_CPT_TERR ()
    ipc.control(PMDGID  + 540,1)
    
end

function EVT_YOKE_L_AP_DISC_SWITCH ()			--Map to Yoke Button
    ipc.control(PMDGID + 1540, 1)
end

function EFIS_CPT_BARO_inc ()
    ipc.control(PMDGID  + 530,256)
end

function EFIS_CPT_BARO_dec ()
    ipc.control(PMDGID  + 530,128)
   
end

function EFIS_CPT_BARO_STD ()
    ipc.control(PMDGID  + 531,1)
    
end

function EFIS_CPT_RANGE_TFC ()
    ipc.control(PMDGID  + 527,1)
 
end

function TCAS_k_1  ()
	ipc.control(PMDGID  + 1301,1)
	ipc.control(PMDGID  + 1301,128)
	
end	

function TCAS_k_2  ()
	ipc.control(PMDGID  + 1302,1)
	ipc.control(PMDGID  + 1302,128)
end	

function TCAS_k_3  ()
	ipc.control(PMDGID  + 1303,1)
	ipc.control(PMDGID  + 1303,128)
end	

function TCAS_k_4  ()
	ipc.control(PMDGID  + 1304,1)
	ipc.control(PMDGID  + 1304,128)
end	

function TCAS_k_5  ()
	ipc.control(PMDGID  + 1305,1)
	ipc.control(PMDGID  + 1305,128)
end	

function TCAS_k_6  ()
	ipc.control(PMDGID  + 1306,1)
	ipc.control(PMDGID  + 1306,128)
end	

function TCAS_k_7  ()
	ipc.control(PMDGID  + 1307,1)
	ipc.control(PMDGID  + 1307,128)
end	

function TCAS_k_clr  ()
	ipc.control(PMDGID  + 1308,1)
	ipc.control(PMDGID  + 1308,128)
end	

function TCAS_k_0  ()
	ipc.control(PMDGID  + 1309,1)
	ipc.control(PMDGID  + 1309,128)
end	

function E1_E2_fuel_on ()               -- Tca 
	ipc.control(69632 + 968, 0) 
	ipc.sleep(1500)
	ipc.control(69632 + 969, 0)
end

function E1_E2_fuel_off ()               -- Tca 
	ipc.control(69632 + 968, 1) 
	ipc.sleep(1700)
	ipc.control(69632 + 969, 1)
end

function E3_E4_fuel_on ()               -- Tca 
	ipc.control(69632 + 970, 0) 
	ipc.sleep(1200)
	ipc.control(69632 + 971, 0)
end

function E3_E4_fuel_off ()               -- Tca 
	ipc.control(69632 + 970, 1) 
	ipc.sleep(1500)
	ipc.control(69632 + 971, 1)
end

function FUEL_PUMP_OVRD_FWD_2 ()
if ipc.readLvar("switch_120_74X") == 0 then
	ipc.control(PMDGID + 120, 1) else
	ipc.control(PMDGID + 120, 0)
	end
end	

function FUEL_PUMP_OVRD_FWD_3 ()
if ipc.readLvar("switch_121_74X") == 0 then
	ipc.control(PMDGID + 121, 1) else
	ipc.control(PMDGID + 121, 0)
	end
end	

function FUEL_PUMP_OVRD_AFT_2 ()
if ipc.readLvar("switch_126_74X") == 0 then
	ipc.control(PMDGID + 126, 1) else
	ipc.control(PMDGID + 126, 0)
	end
end	

function FUEL_PUMP_OVRD_AFT_3 ()
if ipc.readLvar("switch_127_74X") == 0 then
	ipc.control(PMDGID + 127, 1) else
	ipc.control(PMDGID + 127, 0)
	end
end	






-----------------------------------------
-----------------------------------------
-- $$ EVENT FLAGS
event.flag(1, "LIGHTS_TAXI")
event.flag(2, "LIGHTS_Outbd_L")
event.flag(3, "LIGHTS_Outbd_R")
event.flag(4, "LIGHTS_INBD_L")
event.flag(5, "LIGHTS_INBD_R")
event.flag(6, "RWY_L_TURNOFF")
event.flag(7, "RWY_R_TURNOFF")
event.flag(8, "LIGHTS_BEACON")
event.flag(9, "LIGHTS_NAV")
event.flag(10, "LIGHTS_STRB")
event.flag(11, "LIGHTS_WING")
event.flag(12, "LIGHTS_LOGO")
event.flag(13, "FUEL_PUMP_FWD_1")
event.flag(14, "FUEL_PUMP_AFT_1")
event.flag(15, "FUEL_PUMP_FWD_2")
event.flag(16, "FUEL_PUMP_AFT_2")
event.flag(17, "FUEL_PUMP_FWD_3")
event.flag(18, "FUEL_PUMP_AFT_3")
event.flag(19, "FUEL_PUMP_FWD_4")
event.flag(20, "FUEL_PUMP_AFT_4")
event.flag(21, "Battery")
event.flag(22, "OH_ELEC_BATTERY_GUARD_open")
event.flag(23, "HYDRAULIC_1")
event.flag(24, "HYDRAULIC_2")
event.flag(25, "HYDRAULIC_3")
event.flag(26, "HYDRAULIC_4")
event.flag(27, "EXT_PWR1")
event.flag(28, "EXT_PWR2")
event.flag(29, "APU_PWR1")
event.flag(30, "APU_PWR2")
event.flag(31, "Apu_on_off")
event.flag(32, "Apu_start")
event.flag(33, "ENGINE_1_START")
event.flag(34, "ENGINE_2_START")
event.flag(35, "ENGINE_3_START")
event.flag(36, "ENGINE_4_START")
event.flag(37, "EVT_OH_ICE_WINDOW_HEAT_L")
event.flag(38, "EVT_OH_ICE_WINDOW_HEAT_R")
event.flag(39, "AIRCOND_PACK_SWITCH_L")
event.flag(40, "AIRCOND_PACK_SWITCH_C")
event.flag(41, "AIRCOND_PACK_SWITCH_R")
event.flag(42, "FUEL_XFEED_1")
event.flag(43, "FUEL_XFEED_4")
event.flag(44, "DOOR_1L")
event.flag(45, "CARGO_SIDE")
event.flag(46, "CARGO_FWD")
event.flag(47, "CARGO_AFT")
event.flag(48, "CARGO_BULK")
event.flag(49, "CARGO_NOSE")
event.flag(50, "DSP_DOOR")
event.flag(51, "DSP_FCTL")
event.flag(52, "DSP_CHKL")
event.flag(53, "DSP_NAV")
event.flag(54, "DSP_RCL")
event.flag(55, "DSP_CANC")
event.flag(56, "DSP_GEAR")
event.flag(57, "DSP_HYD")
event.flag(58, "DSP_ECS")
event.flag(59, "DSP_FUEL")
event.flag(60, "DSP_ELEC")
event.flag(61, "DSP_STAT")
event.flag(62, "DSP_ENG")
event.flag(63, "EMER_LIGHT_SWITCH")
event.flag(64, "EMER_LIGHT_SWITCH_GUARD")
event.flag(65, "IRU_SELECTOR_L")
event.flag(66, "IRU_SELECTOR_C")
event.flag(67, "IRU_SELECTOR_R")
event.flag(68, "FD_L")
event.flag(69, "FD_R")
event.flag(70, "THR")
event.flag(71, "SPD")
event.flag(72, "LNAV")
event.flag(73, "VNAV")
event.flag(74, "LVL_CHG")
event.flag(75, "APP")
event.flag(76, "LOC")
event.flag(77, "VS_SWT")
event.flag(78, "MCP_SPEED_SELECTOR_inc")
event.flag(79, "MCP_SPEED_SELECTOR_incfast")
event.flag(80, "MCP_SPEED_SELECTOR_dec")
event.flag(81, "MCP_SPEED_SELECTOR_decfast")
event.flag(82, "MCP_HDG_SELECTOR_inc")
event.flag(83, "MCP_HDG_SELECTOR_incfast")
event.flag(84, "MCP_HDG_SELECTOR_dec")
event.flag(85, "MCP_HDG_SELECTOR_decfast")
event.flag(86, "MCP_HEADING_PUSH_SWITCH")
event.flag(87, "MCP_ALT_SELECTOR_inc")
event.flag(88, "MCP_ALT_SELECTOR_incfast")
event.flag(89, "MCP_ALT_SELECTOR_dec")
event.flag(90, "MCP_ALT_SELECTOR_decfast")
event.flag(91, "MCP_ALT_PUSH_SWITCH")
event.flag(92, "MCP_VS_SELECTOR_inc")
event.flag(93, "MCP_VS_SELECTOR_dec")
event.flag(94, "EFIS_CPT_RANGE_inc")
event.flag(95, "EFIS_CPT_RANGE_dec")
event.flag(96, "Autobrake_rto")
event.flag(97, "Autobrake_off")
event.flag(98, "Autobrake_1")
event.flag(99, "Autobrake_2")
event.flag(100, "Autobrake_3")
event.flag(101, "Autobrake_4")
event.flag(102, "LIGHT_STORM")
event.flag(103, "CDU_C_Wheel_Chocks")
event.flag(104, "CDU_C_GPU")
event.flag(105, "NOSE_CARGO")
event.flag(106, "TCAS_MODE_stby")
event.flag(107, "MCP_AP_L_SWITCH")
event.flag(108, "MCP_AP_C_SWITCH")
event.flag(109, "MCP_AP_R_SWITCH")
event.flag(110, "EVT_MCP_AT_ARM_SWITCH_toggle")
event.flag(111, "TOGA1_SWITCH")
event.flag(112, "WARN_Reset")
event.flag(113, "EFIS_CPT_WXR")
event.flag(114, "EFIS_CPT_STA")
event.flag(115, "EFIS_CPT_WPT")
event.flag(116, "EFIS_CPT_ARPT")
event.flag(117, "EFIS_CPT_DATA")
event.flag(118, "EFIS_CPT_POS")
event.flag(119, "EFIS_CPT_TERR")
event.flag(120, "EVT_YOKE_L_AP_DISC_SWITCH")
event.flag(121, "EFIS_CPT_BARO_inc")
event.flag(122, "EFIS_CPT_BARO_dec")
event.flag(123, "EFIS_CPT_BARO_STD")
event.flag(124, "EFIS_CPT_RANGE_TFC")
event.flag(125, "TCAS_k_1")
event.flag(126, "TCAS_k_2")
event.flag(127, "TCAS_k_3")
event.flag(128, "TCAS_k_4")
event.flag(129, "TCAS_k_5")
event.flag(130, "TCAS_k_6")
event.flag(131, "TCAS_k_7")
event.flag(132, "TCAS_k_clr")
event.flag(133, "TCAS_k_0")
event.flag(134, "E1_E2_fuel_on")
event.flag(135, "E1_E2_fuel_off")
event.flag(136, "E3_E4_fuel_on")
event.flag(137, "E3_E4_fuel_off")
event.flag(138, "FUEL_PUMP_OVRD_FWD_2")
event.flag(139, "FUEL_PUMP_OVRD_FWD_3")
event.flag(140, "FUEL_PUMP_OVRD_AFT_2")
event.flag(141, "FUEL_PUMP_OVRD_AFT_3")