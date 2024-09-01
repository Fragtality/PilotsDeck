using PilotsDeck.Tools;
using System;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.MSFS
{
    public static class ToolsMSFS
    {
        public static bool IsCalculatorTemplate(SimCommandType? type, string address)
        {
            return type == SimCommandType.CALCULATOR && address?.StartsWith('$') == true;
        }

        public static string BuildCalculatorCode(string template, int ticks)
        {
            string code;
            if (template.StartsWith('$'))
                template = template.Remove(0, 1);
            string[] parts = template.Split(':');

            if (parts[0].Length == 1 && parts[0] == "K")
                code = BuildEventCode(parts, ticks);
            else
                code = BuildLvarCode(template, ticks);

            Logger.Debug($"Build Calculator Code: '{template}' => '{code}'");
            return code;
        }

        public static string BuildEventCode(string[] parts, int ticks)
        {
            string code = "";
            ticks = Math.Abs(ticks);

            string cmd = BuildEventCommandCode(parts);

            if (!string.IsNullOrWhiteSpace(cmd))
                for (int i = 0; i < ticks; i++)
                    code += (i > 0 ? " " : "") + cmd;

            return code;
        }

        public static string BuildEventCommandCode(string[] parts)
        {
            string cmd = "";
            if (parts.Length >= 3 && Conversion.IsNumber(parts[2]))
            {
                if (parts.Length == 3)
                    cmd = $"{parts[2]} (>K:{parts[1]})";
                else if (parts.Length == 4 && Conversion.IsNumber(parts[3]))
                    cmd = $"{parts[2]} {parts[3]} (>K:{parts[1]})";
            }
            else if (parts.Length == 2)
                cmd = $"(>K:{parts[1]})";

            return cmd;
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

            if (parts.Length >= 2 && TypeMatching.rxLvar.IsMatch(parts[0]) && Conversion.IsNumber(parts[1], out double numStep))
            {
                string lvar = parts[0];
                if (!lvar.StartsWith("L:"))
                    lvar = "L:" + lvar;
                string op = increase ? "+" : "-";
                numStep *= (double)Math.Abs(ticks);
                string step = Conversion.ToString(numStep);
                if (step.Contains(','))
                    step = step.Replace(',', '.');

                code = $"({lvar}) {step} {op} (>{lvar})";

                if (parts.Length == 3 && Conversion.IsNumber(parts[2]))
                {
                    if (parts[2].Contains(','))
                        parts[2] = parts[2].Replace(',', '.');

                    string cmp = increase ? "<=" : ">=";
                    code = $"({lvar}) {step} {op} {parts[2]} {cmp} if{{ {code} }}";
                }

                if (parts.Length == 4 && Conversion.IsNumber(parts[2]) && Conversion.IsNumber(parts[3]))
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

        public async static Task<bool> WriteHvar(SimCommand command)
        {
            bool result = false;
            string[] hVars = command.Address.Split(':');

            for (int idx = 0; idx < hVars.Length; idx++)
            {
                if (hVars[idx] == "H")
                    continue;

                if (idx + 1 < hVars.Length && Conversion.IsNumberI(hVars[idx + 1], out _))
                {
                    result = WriteSingleHvar(hVars[idx], hVars[idx + 1]);
                    idx++;
                }
                else
                    result = WriteSingleHvar(hVars[idx]);

                if (!result)
                    break;

                if (command.CommandDelay > 0)
                    await Task.Delay(command.CommandDelay, App.CancellationToken);
            }

            return result;
        }

        public static bool WriteSingleHvar(string name, string value = null)
        {
            bool result = false;

            if (!name.StartsWith("H:"))
                name = "H:" + name;

            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    MobiModule.ExecuteCode($"(>{name})");
                else
                    MobiModule.ExecuteCode($"{value} (>{name})");
                result = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public async static Task<bool> WriteKvar(SimCommand command)
        {
            bool result = false;
            string[] kVars = command.Address.Split(':');

            for (int idx = 0; idx < kVars.Length; idx++)
            {
                if (kVars[idx] == "K")
                    continue;

                if (idx + 1 < kVars.Length && Conversion.IsNumberF(kVars[idx + 1], out _) && idx + 2 < kVars.Length && Conversion.IsNumberF(kVars[idx + 2], out _))
                {
                    result = WriteSingleKvar(kVars[idx], kVars[idx + 1], kVars[idx +2]);
                    idx += 2;
                }
                else if (idx + 1 < kVars.Length && Conversion.IsNumberF(kVars[idx + 1], out _))
                {
                    result = WriteSingleKvar(kVars[idx], kVars[idx + 1]);
                    idx++;
                }
                else
                    result = WriteSingleKvar(kVars[idx]);

                if (!result)
                    break;

                if (command.CommandDelay > 0)
                    await Task.Delay(command.CommandDelay, App.CancellationToken);
            }

            return result;
        }

        public static bool WriteSingleKvar(string name, string value1 = null, string value2 = null)
        {
            bool result = false;

            if (!name.StartsWith("K:"))
                name = "K:" + name;

            try
            {
                
                if (!string.IsNullOrWhiteSpace(value1) && !string.IsNullOrWhiteSpace(value2))
                    MobiModule.ExecuteCode($"{value2} {value1} (>{name})");
                if (!string.IsNullOrWhiteSpace(value1))
                    MobiModule.ExecuteCode($"{value1} (>{name})");
                else
                    MobiModule.ExecuteCode($"(>{name})");
                result = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }
    }
}
