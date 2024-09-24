using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using PilotsDeck.Simulator.MSFS;
using PilotsDeck.Tools;
using System;
using System.Collections.Concurrent;

namespace PilotsDeck.Actions.Advanced
{
    public class ActionCommand(ModelCommand model)
    {
        public ModelCommand Settings { get; set; } = model;
        public StreamDeckCommand DeckCommandType { get; set; } = model.DeckCommandType;
        public bool IsEncoder { get { return DeckCommandType == StreamDeckCommand.DIAL_LEFT || DeckCommandType == StreamDeckCommand.DIAL_RIGHT || DeckCommandType == StreamDeckCommand.TOUCH_TAP; } }
        public SimCommandType CommandType { get; set; } = model.CommandType;
        public bool DoNotRequestBvar { get; set; } = model.DoNotRequestBvar;
        public string Address { get; set; } = model.Address;
        public string Name { get; set; } = model.Name;
        public bool IsValidCommand { get { return SimCommand.IsValidAddressForType(Address, CommandType, DoNotRequestBvar); } }
        public bool IsValidValueType { get { return SimCommand.IsValidValueCommand(Address, DoNotRequestBvar, CommandType); } }
        public bool IsValueType { get { return SimCommand.IsValueCommand(CommandType, DoNotRequestBvar); } }
        public bool IsCommandType { get { return SimCommand.IsNonvalueCommand(CommandType, DoNotRequestBvar); } }
        public ManagedVariable Variable { get; set; } = null;
        public string Value { get { return Variable?.Value ?? "0"; } }
        public bool IsNumericValue { get { return Variable?.IsNumericValue == true; } }
        public double NumericValue { get { return Variable?.NumericValue ?? 0; } }
        public int TimeAfterLastDown { get; set; } = model.TimeAfterLastDown;
        public TimeSpan SpanAfterLastDown { get { return TimeSpan.FromMilliseconds(TimeAfterLastDown); } }
        public int TickDelay { get; set; } = model.TickDelay;
        public bool ResetSwitch { get; set; } = model.ResetSwitch;
        public string ResetValue { get; set; } = model.ResetValue;
        public int ResetDelay { get; set; } = model.ResetDelay;
        public bool UseCommandDelay { get; set; } = model.UseCommandDelay;
        public int CommandDelay { get; set; } = model.CommandDelay;
        public bool IsCode { get { return (IsValueType && WriteValue?.StartsWith('$') == true) || (CommandType == SimCommandType.CALCULATOR && Address?.StartsWith('$') == true); } }
        public bool IsCounter { get { return WriteValue?.Length >= 2 && !WriteValue?.Contains(',') == true && Conversion.IsNumber(Code?.Split(':')?[0]); } }
        public bool IsSequence { get { return IsCode && !IsCounter && (WriteValue?.Contains(',') == true && WriteValue?.Length >= 4) || (WriteValue?.StartsWith("$=") == true && WriteValue.Length >= 3); } }
        public string Code { get { return WriteValue?.Trim()?[1..]; } }
        public string WriteValue { get; set; } = model.WriteValue;        
        public ConcurrentDictionary<int, ConditionHandler> Conditions { get; set; } = model.GetConditions();
        public bool AnyCondition { get; set; } = model.AnyCondition;

        public void RegisterRessources()
        {
            if (IsValidValueType && Variable == null)
            {
                Logger.Verbose($"Register Variable '{Address}'");
                Variable = App.PluginController.VariableManager.RegisterVariable(Address);
            }

            if (CommandType == SimCommandType.LUAFUNC)
                App.PluginController.ScriptManager.RegisterScript(Address);

            foreach (var condition in Conditions.Values)
            {
                if (condition.Variable == null)
                {
                    Logger.Verbose($"Register Variable '{condition.Address}'");
                    condition.Variable = App.PluginController.VariableManager.RegisterVariable(condition.Address);
                }
            }
        }

        public void DeregisterRessources()
        {
            if (Variable != null)
            {
                Logger.Verbose($"Deregister Variable '{Address}'");
                App.PluginController.VariableManager.DeregisterVariable(Variable.Address);
                Variable = null;
            }

            if (CommandType == SimCommandType.LUAFUNC)
                App.PluginController.ScriptManager.DeregisterScript(Address);

            foreach (var condition in Conditions.Values)
            {
                if (condition.Variable != null)
                {
                    Logger.Verbose($"Deregister Variable '{condition.Variable.Address}'");
                    App.PluginController.VariableManager.DeregisterVariable(condition.Variable.Address);
                    condition.Variable = null;
                }
            }
            Conditions.Clear();
        }

        public bool CompareConditions()
        {
            if (Conditions.IsEmpty)
                return true;

            int success = 0;
            foreach (var condition in Conditions.Values)
            {
                if (condition.Compare())
                {
                    if (AnyCondition)
                    {
                        success = Conditions.Count;
                        break;
                    }
                    else
                        success++;
                }
            }
            Logger.Verbose($"Compared Conditions - success: {success} | anycondition: {AnyCondition} | count {Conditions.Count}");
            return success == Conditions.Count;
        }
        
        public bool CompareTime(TimeSpan diff)
        {
            return diff >= SpanAfterLastDown;
        }

        public SimCommand GetSimCommand(string context, bool keyUp, int ticks, bool encoderAction = false)
        {
            ticks = ticks == 0 ? 1 : ticks;

            SimCommand command = new()
            {
                Context = context,
                Address = Address,
                Type = CommandType,
                IsUp = keyUp,
                Ticks = Math.Abs(ticks),
                TickDelay = TickDelay,
                EncoderAction = encoderAction
            };

            if (IsCode)
            {
                if (IsCounter && IsValidValueType)
                    command.Value = ToolsValueState.GetCounter(Code, NumericValue, Value, ticks);
                else if (IsSequence && IsValidValueType)
                    command.Value = ToolsValueState.GetSequence(Code, NumericValue, Value, IsNumericValue);
                else if (ToolsMSFS.IsCalculatorTemplate(CommandType, Address))
                    command.Address = ToolsMSFS.BuildCalculatorCode(Address, ticks);
                else
                    Logger.Warning($"Could not calculate a Value from Code '{WriteValue}' | '{Address}'");
            }

            if (IsValidValueType && !IsCode)
            {
                command.Value = WriteValue;
                if (ResetSwitch)
                {
                    command.IsValueReset = true;
                    command.ResetValue = ResetValue;
                    command.ResetDelay = ResetDelay;
                }
                else if (!string.IsNullOrWhiteSpace(ResetValue))
                    command.Value = ToolsValueState.Toggle(Value, WriteValue, ResetValue);
            }

            if (CommandType == SimCommandType.BVAR)
                command.DoNotRequest = DoNotRequestBvar;

            if (SimCommand.CommandTypeUsesDelay(CommandType, DoNotRequestBvar) && UseCommandDelay)
            {
                command.CommandDelay = CommandDelay;
            }

            Logger.Verbose($"Created Command - Ticks: {command.Ticks}");
            return command; 
        }
    }
}
