using FSUIPC;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace PilotsDeck
{
    public static class SimTools
    {
        public static bool VjoyToggle(ActionSwitchType type, string address)
        {
            if (!IPCTools.IsVjoyToggle(address, type))
                return false;

            if (type == ActionSwitchType.VJOYDRV)
                return vJoyManager.ToggleButton(address);
            else
                return SendVjoy(address, 0);
        }

        public static bool SendVjoy(string address, byte action)
        {
            try
            {
                string[] parts = address.Split(':');
                byte[] offValue = new byte[4];

                offValue[3] = 0;
                offValue[2] = action;
                offValue[1] = Convert.ToByte(parts[0]); //joy
                offValue[0] = Convert.ToByte(parts[1]); //btn

                return WriteOffset("0x29F0:4:i", BitConverter.ToUInt32(offValue, 0).ToString());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:SendVjoy", $"Exception while sending Virtual Joystick '{address}' with action '{action}' to FSUIPC! (Exception: {ex.GetType()})");
                return false;
            }
        }

        public static bool VjoyClearSet(ActionSwitchType type, string address, bool clear)
        {
            if (type == ActionSwitchType.VJOYDRV)
            {
                if (clear)
                    return vJoyManager.ClearButton(address);
                else
                    return vJoyManager.SetButton(address);
            }
            else
            {
                if (clear)
                    return SendVjoy(address, 2);
                else
                    return SendVjoy(address, 1);
            }
        }

        public static bool WriteOffset(string address, string value)
        {
            if (string.IsNullOrEmpty(address) || !FSUIPCConnection.IsOpen)
                return false;

            bool result = false;
            IPCValueOffset offset = null;
            try
            {
                offset = new IPCValueOffset(address, AppSettings.groupStringWrite, OffsetAction.Write);
                offset.Write(value, AppSettings.groupStringWrite);
                offset.Dispose();
                offset = null;
                result = true;

            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:WriteOffset", $"Exception while writing Offset '{address}' to FSUIPC! (size:{offset?.Size}/float:{offset?.IsFloat}/string:{offset?.IsString}/signed:{offset?.IsSigned}) (Exception: {ex.GetType()})");
                offset?.Dispose();
            }

            return result;
        }

        public static bool RunMacros(string address)
        {
            bool result = false;

            string[] tokens = address.Split(':');
            if (tokens.Length == 2)
                result = RunMacro(address);
            else
            {
                string macroFile = tokens[0];
                int fails = 0;
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (!RunMacro(macroFile + ":" + tokens[i]))
                        fails++;
                    else
                        Thread.Sleep(25);
                }
                if (fails == 0)
                    result = true;
            }

            return result;
        }

        public static bool RunMacro(string name)
        {
            try
            {
                Offset request = new(0x0D70, 128);
                request.SetValue(name);
                FSUIPCConnection.Process();
                request.Disconnect();
                request = null;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:RunMacro", $"Exception while Executing Macro '{name}'! (Exception: {ex.GetType()})");
                return false;
            }

            return true;
        }

        public static bool WriteLvar(string address, string newValue)
        {
            bool result = false;
            if (newValue?.Length < 1)
                return result;

            double value = Convert.ToDouble(newValue, new RealInvariantFormat(newValue));
            address = address.Replace("L:", "");
            result = WriteLvar(address, value);

            return result;
        }

        public static bool WriteLvar(string address, double value)
        {
            bool result = false;
            try
            {
                FSUIPCConnection.WriteLVar(address, value);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:WriteLvar", $"Exception while writing LVar '{address}' via FSUIPC! (Value: '{value}') (Exception: {ex.GetType()})");
            }

            return result;
        }

        public static bool WriteSimVar(MobiSimConnect mobiConnect, string address, string onValue)
        {
            try
            {
                if (!double.TryParse(onValue, new RealInvariantFormat(onValue), out double dValue))
                return false;

                mobiConnect.SetSimVar(address, dValue);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:WriteSimVar", $"Exception while writing SimVar '{address}' via Mobi! (Value: '{onValue}') (Exception: {ex.GetType()})");
            }

            return false;
        }

        public static bool SendControls(string address, bool useControlDelay)
        {
            if (!address.Contains('=') && address.Contains(':') && IPCTools.rxControlSeq.IsMatch(address))
                return SendControlsSeq(address, useControlDelay);
            else if (!address.Contains('=') && !address.Contains(':'))
                return SendControl(address);

            int fails = 0;

            string codeControl;
            string codeParam;
            while (address.Length > 0)
            {
                codeControl = GetNextTokenMove(ref address, "=");

                if (address.Length == 0)
                    if (!SendControl(codeControl))
                        fails++;

                while (address.Length > 0 && !PeekNextDelim(address, "="))
                {
                    codeParam = GetNextTokenMove(ref address, ":");
                    if (!SendControl(codeControl, codeParam))
                        fails++;
                    if (useControlDelay)
                        Thread.Sleep(AppSettings.controlDelay);
                }
            }

            return fails == 0;
        }

        public static bool SendControl(string control, string param = "0")
        {
            return SendControl(Convert.ToInt32(control), Convert.ToInt32(param));
        }

        public static bool SendControl(int control, int param = 0)
        {
            bool result = false;
            try
            {
                FSUIPCConnection.SendControlToFS(control, param);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:SendControl", $"Exception while sending Control '{control}:{param}' to FSUIPC! (Exception: {ex.GetType()})");
            }

            return result;
        }

        public static bool RunSingleScript(string name, int flag)
        {
            try
            {
                Offset param = null;
                if (flag != -1)
                {
                    param = new Offset(0x0D6C, 4);
                    param.SetValue(flag);
                    FSUIPCConnection.Process();
                }

                Offset request = new(0x0D70, 128);
                request.SetValue(name);

                FSUIPCConnection.Process();
                request.Disconnect();
                request = null;
                if (flag != -1)
                {
                    param.Disconnect();
                    param = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:RunSingleScript", $"Exception while Executing Script '{name}' on FSUIPC! (Exception: {ex.GetType()})");
                return false;
            }

            return true;
        }

        public static bool RunScript(string name)
        {
            try
            {
                string[] parts = name.Split(':');
                if (parts.Length == 3 && int.TryParse(parts[2], out int result))    //Script with flag
                    return RunSingleScript(parts[0] + ":" + parts[1], result);
                if (parts.Length == 2)                                              //Script
                    return RunSingleScript(parts[0] + ":" + parts[1], -1);          //Script with mulitple flags
                if (parts.Length >= 3)
                {
                    for (int i = 2; i < parts.Length; i++)
                    {
                        if (int.TryParse(parts[i], out result))
                        {
                            if (!RunSingleScript(parts[0] + ":" + parts[1], result))
                                return false;
                        }
                        else
                        {
                            Logger.Log(LogLevel.Error, "SimTools:RunScript", $"The Parameter '{parts?[i]}' is not a valid Number!");
                            return false;
                        }
                        Thread.Sleep(25);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:RunScript", $"Exception while Executing Script '{name}'! on FSUIPC! (Exception: {ex.GetType()})");
                return false;
            }

            return true;
        }

        public static bool WriteHvar(MobiSimConnect connection, string Address, bool useDelay)
        {
            bool result = false;
            string[] hVars = Address.Split(':');

            if (hVars.Length > 2 || (hVars.Length == 2 && hVars[0].Length > 2))
            {
                foreach (string var in hVars)
                {
                    if (var.Length > 1)
                    {
                        result = WriteSingleHvar(connection, var);
                        if (useDelay)
                            Thread.Sleep(AppSettings.controlDelay);
                    }
                }
            }
            else if ((hVars.Length == 2 && hVars[0] == "H") || (hVars.Length == 1 && hVars[0].Length > 1))
            {
                result = WriteSingleHvar(connection, Address);
            }
            else
            {
                Logger.Log(LogLevel.Error, "SimTools:WriteHvar", $"HVar Address '{Address}' is not valid! (Parts: {hVars.Length})");
            }

            return result;
        }

        public static bool WriteSingleHvar(MobiSimConnect connection, string name)
        {
            bool result = false;

            if (!name.Contains("H:"))
                name = "H:" + name;

            try
            {
                connection.ExecuteCode($"(>{name})");
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:WriteSingleHvar", $"Exception while setting HVar '{name}' via WASM! (Exception: {ex.GetType()})");
            }

            return result;
        }

        public static bool RunCalculatorCode(MobiSimConnect connection, string code, int ticks = 1)
        {
            bool result = false;

            if (code[0] == '$')
                code = BuildCalculatorCode(code[1..], ticks);

            try
            {
                if (!string.IsNullOrEmpty(code))
                {
                    connection.ExecuteCode(code);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "SimTools:RunCalculatorCode", $"Exception while running Calculator Code '{code}' via WASM! (Exception: {ex.GetType()})");
            }

            return result;
        }

        public static string BuildCalculatorCode(string template, int ticks)
        {
            string code;
            string[] parts = template.Split(':');


            if (parts[0].Length == 1 && parts[0] == "K")
                code = BuildEventCode(parts, ticks);
            else
                code = BuildLvarCode(template, ticks);

            

            Logger.Log(LogLevel.Information, "SimTools:BuildCalculatorCode", $"Resulting Calculator Code '{code}'");

            return code;
        }

        public static string BuildEventCode(string[] parts, int ticks)
        {
            string code = "";

            string cmd = "";
            if (parts.Length == 3 && double.TryParse(parts[2], new RealInvariantFormat(parts[2]), out _))
                cmd = $"{parts[2]} (>K:{parts[1]})";
            else if (parts.Length == 2)
                cmd = $"(>K:{parts[1]})";

            if (!string.IsNullOrWhiteSpace(cmd))
                for (int i = 0; i < ticks; i++)
                    code += (i > 0 ? " " : "") + cmd;

            return code;
        }

        public static string BuildLvarCode(string template, int ticks)
        {
            string code = "";
            template = template.Replace("$L:", "");
            if (template.StartsWith("L:"))
                template = template[2..];

            string[] parts = template.Split(':');

            if (parts.Length < 2)
                return code;

            bool increase = parts[1][0] != '-';
            if (parts[1][0] == '-' || parts[1][0] == '+')
                parts[1] = parts[1][1..];

            if (parts[1][0] != '+' && parts[1][0] != '-')
                parts[1] = parts[1].Insert(0, "+");

            if (parts.Length >= 2 && IPCTools.rxLvar.IsMatch(parts[0]) && double.TryParse(parts[1], NumberStyles.Number, new RealInvariantFormat(parts[1]), out double numStep))
            {
                string lvar = parts[0];
                if (!lvar.StartsWith("L:"))
                    lvar = "L:" + lvar;
                string op = increase ? "+" : "-";
                numStep *= (double)ticks;
                string step = string.Format(CultureInfo.InvariantCulture, "{0:G}", numStep);
                if (step.Contains(','))
                    step = step.Replace(',', '.');

                code = $"({lvar}) {step} {op} (>{lvar})";

                if (parts.Length == 3 && double.TryParse(parts[2], NumberStyles.Number, new RealInvariantFormat(parts[2]), out _))
                {
                    if (parts[2].Contains(','))
                        parts[2] = parts[2].Replace(',', '.');

                    string cmp = increase ? "<=" : ">=";
                    code = $"({lvar}) {step} {op} {parts[2]} {cmp} if{{ {code} }}";
                }
                
                if (parts.Length == 4 && double.TryParse(parts[2], NumberStyles.Number, new RealInvariantFormat(parts[2]), out _) && double.TryParse(parts[3], NumberStyles.Number, new RealInvariantFormat(parts[3]), out _))
                {
                    if (parts[2].Contains(','))
                        parts[2] = parts[2].Replace(',', '.');
                    if (parts[3].Contains(','))
                        parts[3] = parts[3].Replace(',', '.');

                    string cmp = increase ? "<" : ">";
                    string cmp2 = increase ? ">=" : "<=";
                    code = $"({lvar}) {step} {op} {parts[2]} {cmp} if{{ {code} }} ({lvar}) {step} {op} {parts[2]} {cmp2} if{{ {parts[3]} (>{lvar}) }}";
                }
            }

            return code;
        }

        public static bool PeekNextDelim(string address, string delim)
        {
            return Regex.IsMatch(address, $"^[0-9]{{1,}}{delim}.*");
        }

        public static string GetNextTokenMove(ref string address, string delim)
        {
            string result = null;
            int matchIndex = address.IndexOf(delim);
            if (matchIndex != -1)
            {
                result = address[..matchIndex];
                address = address.Remove(0, matchIndex + 1);
            }
            else if (address.Length > 0)
            {
                result = address;
                address = "";
            }

            return result;
        }

        public static bool SendControlsSeq(string address, bool useControlDelay)
        {
            int fails = 0;

            string codeControl;
            while (address.Length > 0)
            {
                codeControl = GetNextTokenMove(ref address, ":");

                if (codeControl != null)
                {
                    if (!SendControl(codeControl))
                        fails++;
                    if (PeekNextDelim(address, ":") && useControlDelay)
                        Thread.Sleep(AppSettings.controlDelay);
                }
            }

            return fails == 0;
        }

        public static bool RunLuaFunc(string address)
        {
            Plugin.ActionController.ipcManager.ScriptManager.RunFunction(address, true);
            return true;
        }

        public static bool WriteInternal(string address, string newValue)
        {
            if (Plugin.ActionController.ipcManager.TryGetValue(address, out IPCValue value))
            {
                value.SetValue(newValue);
                return true;
            }
            return false;
        }
    }
}
