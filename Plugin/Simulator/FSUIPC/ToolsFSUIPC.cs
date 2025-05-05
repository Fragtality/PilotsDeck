using CFIT.AppLogger;
using FSUIPC;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.FSUIPC
{
    public static class ToolsFSUIPC
    {
        public async static Task<bool> RunMacros(SimCommand command)
        {
            bool result = false;
            string address = command.Address.Address;

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
                    else if (command.CommandDelay > 0)
                        await Task.Delay(command.CommandDelay, App.CancellationToken);
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
                Logger.Error($"Exception while Executing Macro '{name}'! (Exception: {ex.GetType()})");
                return false;
            }

            return true;
        }

        public static bool WriteOffset(ManagedAddress address, string value)
        {
            if (address == null || address.IsEmpty == true || !FSUIPCConnection.IsOpen)
                return false;

            bool result = false;
            VariableOffset offset = null;
            try
            {
                offset = new VariableOffset(address, AppConfiguration.IpcGroupWrite, OffsetAction.Write);
                offset.Write(value, AppConfiguration.IpcGroupWrite);
                offset.Dispose();
                offset = null;
                result = true;

            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while writing Offset '{address}' to FSUIPC! (size:{offset?.Size}/float:{offset?.IsFloat}/string:{offset?.IsString}/signed:{offset?.IsSigned}) (Exception: {ex.GetType()})");
                offset?.Dispose();
            }

            return result;
        }

        public static bool WriteLvar(ManagedAddress address, double value)
        {
            bool result = false;
            try
            {
                FSUIPCConnection.WriteLVar(address.FormatFsuipcLvar(), value);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while writing LVar '{address}' via FSUIPC! (Value: '{value}') (Exception: {ex.GetType()})");
            }

            return result;
        }

        public async static Task<bool> RunScripts(SimCommand command)
        {
            try
            {
                string[] parts = command.Address.Address.Split(':');
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
                            Logger.Error($"The Parameter '{parts?[i]}' is not a valid Number!");
                            return false;
                        }
                        if (command.CommandDelay > 0)
                            await Task.Delay(command.CommandDelay, App.CancellationToken);
                        else
                            await Task.Delay(App.Configuration.FsuipcScriptFlagDelay, App.CancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while Executing Script '{command.Address}'! on FSUIPC! (Exception: {ex.GetType()})");
                return false;
            }

            return true;
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
                Logger.Error($"Exception while Executing Script '{name}' on FSUIPC! (Exception: {ex.GetType()})");
                return false;
            }

            return true;
        }

        public async static Task<bool> RunVjoy(SimCommand command)
        {
            if (command.Address.HasParameter)
            {
                return RunVjoyToggle(command.Address);
            }
            else
            {
                return await RunVjoyClearSet(command.Address);
            }
        }

        public async static Task<bool> RunVjoyClearSet(ManagedAddress address)
        {
            bool result = false;
            if (VjoyClearSet(address, false))
            {
                await Task.Delay(App.Configuration.VJoyMinimumPressed, App.CancellationToken);
                if (VjoyClearSet(address, true))
                    result = true;
            }

            return result;
        }

        public static bool VjoyClearSet(ManagedAddress address, bool clear)
        {
            if (clear)
                return SendVjoy(address, 2);
            else
                return SendVjoy(address, 1);
        }

        public static bool RunVjoyToggle(ManagedAddress address)
        {
            return SendVjoy(address, 0);
        }

        public static bool SendVjoy(ManagedAddress address, byte action)
        {
            try
            {
                string[] parts = address.Name.Split(':');
                byte[] offValue = new byte[4];

                offValue[3] = 0;
                offValue[2] = action;
                offValue[1] = Convert.ToByte(parts[0]); //joy
                offValue[0] = Convert.ToByte(parts[1]); //btn

                return WriteOffset(new ManagedAddress("0x29F0:4:i"), BitConverter.ToUInt32(offValue, 0).ToString());
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while sending Virtual Joystick '{address}' with action '{action}' to FSUIPC! (Exception: {ex.GetType()})");
                return false;
            }
        }

        public async static Task<bool> SendControls(SimCommand command)
        {
            string address = command.Address.Address;
            if (!address.Contains('=') && address.Contains(':') && TypeMatching.rxControlSeq.IsMatch(address))
                return await SendControlsSeq(command);
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
                    if (command.CommandDelay > 0)
                        await Task.Delay(command.CommandDelay, App.CancellationToken);
                }
            }

            return fails == 0;
        }

        public async static Task<bool> SendControlsSeq(SimCommand command)
        {
            int fails = 0;
            string address = command.Address.Address;
            string codeControl;
            while (address.Length > 0)
            {
                codeControl = GetNextTokenMove(ref address, ":");

                if (codeControl != null)
                {
                    if (!SendControl(codeControl))
                        fails++;
                    if (PeekNextDelim(address, ":") && command.CommandDelay > 0)
                        await Task.Delay(command.CommandDelay, App.CancellationToken);
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
                Logger.Error($"Exception while sending Control '{control}:{param}' to FSUIPC! (Exception: {ex.GetType()})");
            }

            return result;
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
    }
}
