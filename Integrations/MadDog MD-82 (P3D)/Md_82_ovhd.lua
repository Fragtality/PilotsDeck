MDGID= 16777200

function sign_smoke ()
	
	smoke=ipc.readLvar("ovhd_no_smoke_switch1")
	if smoke==0 then
	 ipc.control(69852, MDGID + 17) elseif
	 smoke==1 then
	 ipc.control(69852, MDGID + 18) else
	 ipc.control(69852, MDGID + 16)
	 end
end
event.flag(1,"sign_smoke")

function sign_seat ()
	
	seat=ipc.readLvar("ovhd_seat_belt_switch1")
	if seat==0 then
	 ipc.control(69853, MDGID + 17) elseif
	 seat==1 then
	 ipc.control(69853, MDGID + 18) else
	 ipc.control(69853, MDGID + 16)
	 end
end
event.flag(2,"sign_seat")

function Iphone()
	
	iphone=ipc.readLvar("ovhd_interphone_switch1")
	if iphone==0 then
    ipc.control(69795, MDGID + 17) else
	ipc.control(69795, MDGID + 16)
	end
    
end
event.flag(3,"Iphone")

function Ext_pwr()
	
	ext=ipc.readLvar("ovhd_gnd_ext_switch1")
	if ext==0 then
    ipc.control(69794, MDGID + 17) else
	ipc.control(69794, MDGID + 16)
	end
    
end
event.flag(4,"Ext_pwr")


function Ext_Bus_R()
	
	busr=ipc.readLvar("ovhd_R_ext_switch1")
	if busr==0 then
    ipc.control(69823, MDGID + 17) else
	ipc.control(69823, MDGID + 16)
	end
    
end
event.flag(6,"Ext_Bus_R")

function Ext_Bus_L()
	
	busl=ipc.readLvar("ovhd_L_ext_switch1")
	if busl==0 then
    ipc.control(69822, MDGID + 17) else
	ipc.control(69822, MDGID + 16)
	end
    
end
event.flag(7,"Ext_Bus_L")

function Apu_Bus_L()
	
	apu_busl=ipc.readLvar("ovhd_L_apu_switch1")
	if apu_busl==0 then
    ipc.control(69820, MDGID + 17) else
	ipc.control(69820, MDGID + 16)
	end
    
end
event.flag(8,"Apu_Bus_L")

function Apu_Bus_R()
	
	apu_busr=ipc.readLvar("ovhd_R_apu_switch1")
	if apu_busr==0 then
    ipc.control(69821, MDGID + 17) else
	ipc.control(69821, MDGID + 16)
	end
    
end
event.flag(9,"Apu_Bus_R")

function galley()
	
	gal=ipc.readLvar("ovhd_galley_switch1")
	if gal==0 then
    ipc.control(69824, MDGID + 17) else
	ipc.control(69824, MDGID + 16)
	end
    
end
event.flag(5,"galley")

function Battery()
	
	bat=ipc.readLvar("ovhd_batt_switch1")
	if bat==0 then
    ipc.control(69833, MDGID + 17) else
	ipc.control(69833, MDGID + 16)
	end
    
end
event.flag(10,"Battery")

function Apu_Master ()
	
	apm=ipc.readLvar("ovhd_apu_master_switch1")
	if apm==0 then
	ipc.control(69831, MDGID + 18) 
	ipc.sleep(2000) 
	ipc.control(69831, MDGID + 17) else
	ipc.control(69831, MDGID + 16)
	 end
end
event.flag(11,"Apu_Master")

function MDx_AC_Supply_L ()
	
	ac=ipc.readLvar("ovhd_L_airsupply_switch1")
	if ac==0 then
	ipc.control(69875, MDGID + 18) 
	 else
	 ipc.control(69875, MDGID + 16)
	 end
end
event.flag(12,"MDx_AC_Supply_L")

function MDx_AC_Supply_R ()
	
	ac=ipc.readLvar("ovhd_R_airsupply_switch1")
	if ac==0 then
	ipc.control(69876, MDGID + 18) 
	 else
	ipc.control(69876, MDGID + 16)
	 end
end
event.flag(13,"MDx_AC_Supply_R")

function MDx_Apu_Air ()
	
	air=ipc.readLvar("ovhd_apu_air_switch1")
	if air==0 then
	ipc.control(69830, MDGID + 17) 
	 elseif
	 air==1 then
	ipc.control(69830, MDGID + 18) else
	ipc.control(69830, MDGID + 16)
	 end
end
event.flag(14,"MDx_Apu_Air")

function Fuel_L_Aft ()
	
	fa=ipc.readLvar("ovhd_L_aft_pump_switch1")
	if fa==0 then
	ipc.control(69842, MDGID + 17) 
	 else
	ipc.control(69842, MDGID + 16)
	 end
end
event.flag(15,"Fuel_L_Aft")

function Fuel_L_fwd ()
	
	fa=ipc.readLvar("ovhd_L_fwd_pump_switch1")
	if fa==0 then
	ipc.control(69848, MDGID + 17) 
	 else
	ipc.control(69848, MDGID + 16)
	 end
end
event.flag(16,"Fuel_L_fwd")

function Fuel_C_Aft ()
	
	fa=ipc.readLvar("ovhd_ctr_aft_pump_switch1")
	if fa==0 then
	ipc.control(69843, MDGID + 17) 
	 else
	ipc.control(69843, MDGID + 16)
	 end
end
event.flag(17,"Fuel_C_Aft")

function Fuel_C_fwd ()
	
	fa=ipc.readLvar("ovhd_ctr_fwd_pump_switch1")
	if fa==0 then
	ipc.control(69849, MDGID + 17) 
	 else
	ipc.control(69849, MDGID + 16)
	 end
end
event.flag(18,"Fuel_C_fwd")

function Fuel_R_Aft ()
	
	fa=ipc.readLvar("ovhd_R_aft_pump_switch1")
	if fa==0 then
	ipc.control(69844, MDGID + 17) 
	 else
	ipc.control(69844, MDGID + 16)
	 end
end
event.flag(19,"Fuel_R_Aft")

function Fuel_R_fwd ()
	
	fa=ipc.readLvar("ovhd_R_fwd_pump_switch1")
	if fa==0 then
	ipc.control(69850, MDGID + 17) 
	 else
	ipc.control(69850, MDGID + 16)
	 end
end
event.flag(20,"Fuel_R_fwd")

function Air_foil_L ()
	
	fa=ipc.readLvar("ovhd_L_airfoil_switch1")
	if fa==0 then
	ipc.control(69855, MDGID + 17) 
	 else
	ipc.control(69855, MDGID + 16)
	 end
end
event.flag(21,"Air_foil_L")

function Air_foil_R ()
	
	fa=ipc.readLvar("ovhd_R_airfoil_switch1")
	if fa==0 then
	ipc.control(69855, MDGID + 19) 
	 else
	ipc.control(69855, MDGID + 18)
	 end
end
event.flag(22,"Air_foil_R")

function Anti_fog ()
	
	fa=ipc.readLvar("ovhd_wind_anti_fog_switch1")
	if fa==0 then
	ipc.control(69857, MDGID + 17) 
	 else
	ipc.control(69857, MDGID + 16)
	 end
end
event.flag(23,"Anti_fog")

function Anti_ice ()
	
	fa=ipc.readLvar("ovhd_wind_anti_ice_switch1")
	if fa==0 then
	ipc.control(69858, MDGID + 17) 
	 else
	ipc.control(69858, MDGID + 16)
	 end
end
event.flag(24,"Anti_ice")

function Anti_ice_ENG_L ()
	
	fa=ipc.readLvar("ovhd_L_eng_antiice_switch1")
	if fa==0 then
	ipc.control(69859, MDGID + 17) 
	 else
	ipc.control(69859, MDGID + 16)
	 end
end
event.flag(25,"Anti_ice_ENG_L")

function Anti_ice_ENG_R ()
	
	fa=ipc.readLvar("ovhd_R_eng_antiice_switch1")
	if fa==0 then
	ipc.control(69860, MDGID + 17) 
	 else
	ipc.control(69860, MDGID + 16)
	 end
end
event.flag(26,"Anti_ice_ENG_R")

function Pneux_L ()
	
	fa=ipc.readLvar("ped_pneu_lever1")
	if fa==0 then
	ipc.control(69904, MDGID + 16) 
	 else
	ipc.control(69904, MDGID + 17)
	 end
end
event.flag(27,"Pneux_L")

function Pneux_R ()
	
	fa=ipc.readLvar("ped_pneu_lever2")
	if fa==0 then
	ipc.control(69905, MDGID + 16) 
	 else
	ipc.control(69905, MDGID + 17)
	 end
end
event.flag(28,"Pneux_R")

function Ignition_sys_inc ()
	
	IG=ipc.readLvar("ovhd_ign_switch1")
	if IG==0 then
	ipc.control(69837, MDGID + 17) 
	elseif IG==1 then
	ipc.control(69837, MDGID + 18) 
	elseif IG==2 then
	ipc.control(69837, MDGID + 19)
	elseif IG==3 then	
	ipc.control(69837, MDGID + 20)
	 end
end
event.flag(29,"Ignition_sys_inc")

function EMER_LTS ()
	
	fa=ipc.readLvar("ovhd_emer_light_switch1")
	if fa==0 then
	ipc.control(69851, MDGID + 18)
	 elseif
	 fa==1 then
	ipc.control(69851, MDGID + 17) else
	ipc.control(69851, MDGID + 16)
	 end
end
event.flag(30,"EMER_LTS")

function Start_pump ()
	
	fa=ipc.readLvar("ovhd_start_pump_switch1")
	if fa==0 then
	ipc.control(69836, MDGID + 16) 
	 else
	ipc.control(69836, MDGID + 17)
	 end
end
event.flag(31,"Start_pump")

function HYD_PUMP_L ()
	
	fa=ipc.readLvar("CM2_hyd_switch1")
	if fa==0 then
	ipc.control(69785, MDGID + 17)
	 elseif
	 fa==1 then
	ipc.control(69785, MDGID + 18) else
	ipc.control(69785, MDGID + 16)
	 end
end
event.flag(32,"HYD_PUMP_L")

function HYD_PUMP_R ()
	
	fa=ipc.readLvar("CM2_hyd_switch2")
	if fa==0 then
	ipc.control(69786, MDGID + 17)
	 elseif
	 fa==1 then
	ipc.control(69786, MDGID + 18) else
	ipc.control(69786, MDGID + 16)
	 end
end
event.flag(33,"HYD_PUMP_R")

function Trans ()
	
	fa=ipc.readLvar("CM2_hyd_switch3")
	if fa==0 then
	ipc.control(69788, MDGID + 16) 
	 else
	ipc.control(69788, MDGID + 17)
	 end
end
event.flag(34,"Trans")

function AUX()
	
	fa=ipc.readLvar("CM2_hyd_switch4")
	if fa==0 then
	ipc.control(69787, MDGID + 16)
	 elseif
	 fa==1 then
	ipc.control(69787, MDGID + 18) else
	ipc.control(69787, MDGID + 17)
	 end
end
event.flag(35,"AUX")

function Md_82_anti()
	
	Anti=ipc.readLvar("CM2_anticollision_switch1")
	if Anti==0 then
	ipc.control(69648, 16777217)
	else
	ipc.control(69648, 16777216)
	end
	
end
event.flag(36,"Md_82_anti")	

function md_82_strobe()		
		
		Strobe=ipc.readLvar("CM2_strobe_switch1")
		if Strobe==0 then
		ipc.control(69649, 16777217)
		elseif
		Strobe==1 then
		ipc.control(69649, 16777218)
		else
		ipc.control(69649, 16777216)
		end
		
end
event.flag(37,"md_82_strobe")

function Nose_light()
		
		nose=ipc.readLvar("CM1_noselight_switch1")
		if nose==0 then
		 ipc.control(69644, MDGID + 17)
		elseif nose==1 then
		ipc.control(69644, MDGID + 18)
		else
		ipc.control(69644, MDGID + 16)
		end
end
event.flag(38,"Nose_light")

function landing_light_left()
		
		landing_left=ipc.readLvar("CM1_winglightl_switch1")
		if landing_left==0 then
		ipc.control(69642, MDGID + 17)
		elseif landing_left==1 then
		ipc.control(69642, MDGID + 18) 
		else
		ipc.control(69642, MDGID + 16)
		end
end
event.flag(39,"landing_light_left")

function landing_light_right()
		
		landing_right=ipc.readLvar("CM1_winglightr_switch1")
		if landing_right==0 then
		ipc.control(69643, MDGID + 17)
		elseif landing_right==1 then
		ipc.control(69643, MDGID + 18) 
		else
		ipc.control(69643, MDGID + 16)
		end
end
event.flag(40,"landing_light_right")

function flood_lights_left()
		
		fll=ipc.readLvar("CM2_floodlightl_switch1")
		if fll==0 then
		ipc.control(69645, MDGID + 17)
		else
		ipc.control(69645, MDGID + 16)
		end
end
event.flag(41,"flood_lights_left")

function flood_lights_right()
		
		flr=ipc.readLvar("CM2_floodlightr_switch1")
		if flr==0 then
		ipc.control(69646, MDGID + 17)
		else
		ipc.control(69646, MDGID + 16)
		end
end
event.flag(42,"flood_lights_right")

function Wing()   					
		
		mdx=ipc.readLvar("CM2_wingnacl_switch1")
		if mdx==0 then 
		ipc.control(69647, MDGID + 17) 
		elseif mdx==1 then
		ipc.control(69647, MDGID + 18) 
		else
		ipc.control(69647, MDGID + 16)
		end
end
event.flag(43,"Wing")

function ART()
		
		Ar=ipc.readLvar("art_switch1")
		if Ar==0 then
		 ipc.control(69737, MDGID + 18)
		else
		ipc.control(69737, MDGID + 16)
		end
end
event.flag(44,"ART")

function FD1()
		
		Ar=ipc.readLvar("CM1_fd_switch1")
		if Ar==0 then
		 ipc.control(69651, MDGID + 17)
		else
		ipc.control(69651, MDGID + 16)
		end
end
event.flag(45,"FD1")

function FD2()
		
		Ar=ipc.readLvar("CM2_fd_switch1")
		if Ar==0 then
		 ipc.control(69652, MDGID + 17)
		else
		ipc.control(69652, MDGID + 16)
		end
end
event.flag(46,"FD2")

function Autopilot()
		
		Ar=ipc.readLvar("fgcp_autopilot_switch1")
		if Ar==0 then
		 ipc.control(69666, 1)
		else
		ipc.control(69666, 0)
		end
end
event.flag(47,"Autopilot")

function AutoBRK()
		
		Ar=ipc.readLvar("ped_autobrake_switch1")
		if Ar==0 then
		 ipc.control(69988,MDGID + 17)
		else
		ipc.control(69988,MDGID + 16)
		end
end
event.flag(48,"AutoBRK")

function EFIS_NavAid()
		
		Ar=ipc.readLvar("CM1_ctrl_NAID_adv1")
		if Ar==0 then
		 ipc.control(69918,MDGID + 17)
		else
		ipc.control(69918,MDGID + 16)
		end
end
event.flag(49,"EFIS_NavAid")

function EFIS_ARPT()
		
		Ar=ipc.readLvar("CM1_ctrl_ARPT_adv1")
		if Ar==0 then
		 ipc.control(69917,MDGID + 17)
		else
		ipc.control(69917,MDGID + 16)
		end
end
event.flag(50,"EFIS_ARPT")

function EFIS_DATA()
		
		Ar=ipc.readLvar("CM1_ctrl_DATA_adv1")
		if Ar==0 then
		 ipc.control(69916,MDGID + 17)
		else
		ipc.control(69916,MDGID + 16)
		end
end
event.flag(51,"EFIS_DATA")

function EFIS_WPT()
		
		Ar=ipc.readLvar("CM1_ctrl_WPT_adv1")
		if Ar==0 then
		 ipc.control(69915,MDGID + 17)
		else
		ipc.control(69915,MDGID + 16)
		end
end
event.flag(52,"EFIS_WPT")

function EFIS_ADF2 ()
	if ipc.readLvar("CM1_ctrl_adf2_knob1") == 0 then
      ipc.control(69914, MDGID  + 17)
	else
      ipc.control(69914, MDGID  + 16)
	end
end
event.flag(53,"EFIS_ADF2")

function EFIS_ADF1 ()
	if ipc.readLvar("CM1_ctrl_adf1_knob1") == 0 then
      ipc.control(69913, MDGID  + 17)
	else
      ipc.control(69913, MDGID  + 16)
	end
end
event.flag(54,"EFIS_ADF1")

function VoltSel_inc ()
	ipc.sleep(50)
    VoltSelVar = ipc.readLvar("ovhd_ac_volts_sel_switch1")

    if VoltSelVar == 0 then VoltSet = 17
    elseif VoltSelVar == 1 then VoltSet = 18
	elseif VoltSelVar == 2 then VoltSet = 19
    elseif VoltSelVar == 3 then VoltSet = 20
    elseif VoltSelVar == 4 then VoltSet = 21
    end
    ipc.control(69819,MDGID + VoltSet)
  
end
event.flag(55,"VoltSel_inc")

function VoltSel_dec ()
	 ipc.sleep(50)
     VoltSelVar = ipc.readLvar("ovhd_ac_volts_sel_switch1")

    if VoltSelVar == 5 then VoltSet = 20
    elseif VoltSelVar == 4 then VoltSet = 19
    elseif VoltSelVar == 3 then VoltSet = 18
    elseif VoltSelVar == 2 then VoltSet = 17
	elseif VoltSelVar == 1 then VoltSet = 16
    end
    ipc.control(69819,MDGID + VoltSet)
	
end
event.flag(56,"VoltSel_dec")

function Ignition_sys_dec ()
	
	
	IG=ipc.readLvar("ovhd_ign_switch1")
	if IG==4 then
	ipc.control(69837, MDGID + 19) 
	elseif IG==3 then
	ipc.control(69837, MDGID + 18) 
	elseif IG==2 then
	ipc.control(69837, MDGID + 17)
	elseif IG==1 then	
	ipc.control(69837, MDGID + 16)
	 end
end
event.flag(57,"Ignition_sys_dec")

function ABRK_MODE_inc ()
	ipc.sleep(50)
    ABRKVar = ipc.readLvar("ped_autobrake_knob1")

    if ABRKVar == 0 then ABRKSet = 0
    elseif ABRKVar == 1 then ABRKSet = 1
	elseif ABRKVar == 2 then ABRKSet = 2
    elseif ABRKVar == 3 then ABRKSet = 3
    end
    ipc.control(69989,ABRKSet)
    
end
event.flag(58,"ABRK_MODE_inc")

function ABRK_MODE_dec ()
	ipc.sleep(50)
    ABRKVar = ipc.readLvar("ped_autobrake_knob1")

    if ABRKVar == 4 then ABRKSet = 2
    elseif ABRKVar == 3 then ABRKSet = 1
    elseif ABRKVar == 2 then ABRKSet = 0
    elseif ABRKVar == 1 then ABRKSet = -1
    end
    ipc.control(69989,ABRKSet)
    
end
event.flag(59,"ABRK_MODE_dec")

function HDG_Knob_push_sel ()
    ipc.control(69659, MDGID + 16)
end
event.flag(60,"HDG_Knob_push_sel")
    

function HDG_Knob_pull_hold ()
    ipc.control(69658, MDGID + 16)
    
end
event.flag(61,"HDG_Knob_pull_hold")


function ALT_push ()
    ipc.control(69664, MDGID + 16)
    
end
event.flag(62,"ALT_push")

function ALT_pull_sel ()
    ipc.control(69664, MDGID + 17)
    
end
event.flag(63,"ALT_pull_sel")

function Pitot_inc ()
    ipc.sleep(10)
    Ptt = ipc.readLvar("ovhd_meter_sel_switch1")

    if Ptt == 0 then PttVar = MDGID + 17
    elseif Ptt == 9 then PttVar = MDGID + 18
    elseif Ptt == 8 then PttVar = MDGID + 19
    elseif Ptt == 7 then PttVar = MDGID + 20
    elseif Ptt == 6 then PttVar = MDGID + 21
    elseif Ptt == 5 then PttVar = MDGID + 22
    elseif Ptt == 4 then PttVar = MDGID + 23
    elseif Ptt == 3 then PttVar = MDGID + 24
    elseif Ptt == 2 then PttVar = MDGID + 25
    end

    ipc.control(69854, PttVar)
   
end
event.flag(64,"Pitot_inc")

function Pitot_dec ()
    ipc.sleep(10)
    Ptt = ipc.readLvar("ovhd_meter_sel_switch1")

    if Ptt == 1 then PttVar = MDGID + 24
    elseif Ptt == 2 then PttVar = MDGID + 23
    elseif Ptt == 3 then PttVar = MDGID + 22
    elseif Ptt == 4 then PttVar = MDGID + 21
    elseif Ptt == 5 then PttVar = MDGID + 20
    elseif Ptt == 6 then PttVar = MDGID + 19
    elseif Ptt == 7 then PttVar = MDGID + 18
    elseif Ptt == 8 then PttVar = MDGID + 17
    elseif Ptt == 9 then PttVar = MDGID + 16
    end

    ipc.control(69854, PttVar)
    
end
event.flag(65,"Pitot_dec")