local FDswitched = false

function FBW_SYNC()
	FBW_SYNC_EXTPWR() -- 0x5800:1:i
	FBW_SYNC_LIGHTS() -- 0x5801:1:i
	FBW_SYNC_BARO()	-- 0x5802:5:s | 0x5840:4:s
	FBW_SYNC_FCU() -- 0x5807:9:s | 0x5810:8:s | 0x5818:7:s | 0x581F:s:9 | 0x5828:s:17
	FBW_SYNC_GEAR() --0x5839:1:i | 0x583A:1:i
	FBW_SYNC_CLOCK() -- 0x583B:5:s
end

function FBW_SYNC_EXTPWR()
	local extAvail = ipc.readUB(0x07AA)
	local extOn = ipc.readUB(0x07AB)
	
	if extAvail == 1 and extOn == 0 then
		ipc.writeUB(0x5800, 1)
	else
		ipc.writeUB(0x5800, 0)
	end
end

function FBW_SYNC_LIGHTS()
	--Landing Lights
	local pos = ipc.readLvar("LIGHTING_LANDING_2") + ipc.readLvar("LIGHTING_LANDING_3")
	ipc.writeUB(0x5801,pos)
end

function FBW_SYNC_BARO()
	--Main / EFIS Cp
	local unitMode = ipc.readLvar("XMLVAR_Baro_Selector_HPA_1")
	local pressure = ipc.readUW(0x0330)
	local offset = 0x5802
	local newvalue = "Std"

	if ipc.readLvar("A32NX_OVHD_INTLT_ANN") == 0 then
		newvalue = "8888"
	elseif ipc.readLvar("XMLVAR_Baro1_Mode") ~= 3 then
		if unitMode == 0 then
			newvalue = string.format("%.0f", pressure * 0.1845585973546601)
			newvalue = string.sub(newvalue, 0,2) .. "." .. string.sub(newvalue, 3)
		else
			newvalue = string.format("%.0f", pressure * 0.0625)
		end
	end
	
	ipc.writeSTR(offset, newvalue, 5)
	
	--ISIS
	local value = ipc.readUW(0x0332)
	local displ = "STD"
	local isisStd = ipc.readLvar("A32NX_ISIS_BARO_MODE") == 1
	
	if isisStd and value ~= 16212 then
		ipc.execCalcCode("16212 (>K:2:KOHLSMAN_SET)")
		value = 16212
	elseif not isisStd then
		displ = string.format("%.0f", value * 0.0625)
	end
	
	ipc.writeSTR(0x5840, displ, 4)
end

function FBW_SYNC_FCU()
	--------speed
	local spd = "---"
	local isMachModeOn = ipc.readUB(0x5410) == 1
	local isSpdManaged = ipc.readLvar("A32NX_FCU_SPD_MANAGED_DOT") == 1
	local isSpdDashed = ipc.readLvar("A32NX_FCU_SPD_MANAGED_DASHES") == 1
	local selSpd = ipc.readLvar("A32NX_AUTOPILOT_SPEED_SELECTED")
	if selSpd == nil or selSpd == 0 then
		selSpd = "100"
	end

	if not isSpdDashed then
		if isMachModeOn then
			spd = "MCH\n" .. string.format("%.2f", selSpd)
		else
			spd = "SPD\n" .. selSpd
		end
	else
		if isMachModeOn then
			spd = "MCH\n" .. spd
		else
			spd = "SPD\n" .. spd
		end
	end
	
	if isSpdManaged then
		spd = spd .. "*"
	end

	ipc.writeSTR(0x5807, spd, 9)
	
	--------hdg
	local hdg = "---"
	local isLatManaged = ipc.readLvar("A32NX_FCU_HDG_MANAGED_DOT") == 1
	local isTrkModeOn = ipc.readLvar("A32NX_TRK_FPA_MODE_ACTIVE") == 1
	local isHdgDashed = ipc.readLvar("A32NX_FCU_HDG_MANAGED_DASHES") == 1
	local selHdg = ipc.readLvar("A32NX_AUTOPILOT_HEADING_SELECTED") or ""

	if not isHdgDashed then
		if isTrkModeOn then
			hdg = "TRK\n" .. string.format("%03d", selHdg)
		else
			hdg = "HDG\n" .. string.format("%03d", selHdg)
		end
	else
		if isTrkModeOn then
			hdg = "TRK\n" .. hdg
		else
			hdg = "HDG\n" .. hdg
		end
	end

	if isLatManaged then
		hdg = hdg .. "*"
	end
	
	ipc.writeSTR(0x5810, hdg, 8)
	
	--------alt
	local alt = string.format("%05.f", (ipc.readUD(0x0798) * 0.0000500616455078125) or 100)
	if ipc.readLvar("XMLVAR_Autopilot_Altitude_Increment") == 100 then
		alt = string.sub(alt, 1, 2) .. " " .. string.sub(alt, 3, 5)
	end

	if ipc.readLvar("A32NX_FCU_ALT_MANAGED") == 1 then
		alt = alt .. "*"
	end

	ipc.writeSTR(0x5818, alt, 7)
	
	-----vs
	local vs = "----"
	local vsPrefix = "V/S"
	local vsfpa = ipc.readLvar("A32NX_AUTOPILOT_FPA_SELECTED") or ""
	local vsft = ipc.readLvar("A32NX_AUTOPILOT_VS_SELECTED") or ""
	local isVsDashed = ipc.readLvar("A32NX_FCU_VS_MANAGED") == 1

	if isTrkModeOn then
		vsPrefix = "FPA"
	end
	if not isVsDashed then
		if isTrkModeOn then
			if vsfpa >= 0 then
				vs = "+" .. string.format("%.1f", vsfpa)
			else
				vs = string.format("%.1f", vsfpa)
			end
		else
			if vsft >= 0 then
				vs = "+" .. string.format("%04d", vsft)
			else
				vs =  string.format("%05d", vsft)
			end
		end
	end

	-- vs
	ipc.writeSTR(0x581F, vsPrefix .. "\n" .. vs, 9)

	--- alt + vs
	ipc.writeSTR(0x5828, vsPrefix .. "\n" .. alt .. "\n" .. vs, 17)
end

function FBW_SYNC_GEAR()
	local gearSum = ipc.readUD(0x0BEC) + ipc.readUD(0x0BF0) + ipc.readUD(0x0BF4)
	
	local gearUnlk = 0
	if gearSum > 0 and gearSum < 49149 then
		gearUnlk = 1
	end
	
	local gearDown = 0
	if gearSum > 0 then
		gearDown = 1
	end
	ipc.writeUB(0x5839, gearUnlk)
	ipc.writeUB(0x583A, gearDown)
end

function FBW_SYNC_CLOCK()
	local timeSec = ipc.readLvar("A32NX_CHRONO_ET_ELAPSED_TIME")
	
	if timeSec ~= nil and timeSec > -1 then
		local timeHr = math.floor(timeSec / 3600)
		
		timeSec = timeSec - timeHr * 3600
		timeHr = string.format("%02d", timeHr)
		
		local timeMin = string.format("%02d", math.floor(timeSec / 60))
		ipc.writeSTR(0x583B, timeHr .. ":" .. timeMin, 5)
	else
		ipc.writeSTR(0x583B, "", 5)
	end
end

event.timer(250, "FBW_SYNC")