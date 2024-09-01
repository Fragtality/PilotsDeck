using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Simulator;
using PilotsDeck.Tools;

namespace PilotsDeck.UI.ViewModels
{
    public class ViewModelCommand(ActionCommand command, ViewModelAction action, int cmdID) : ISelectableItem
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ActionCommand Command { get; set; } = command;
        public string Header { get { return ""; } }
        public string Type { get { return ""; } }
        public int ElementID { get { return -1; } }
        public int ManipulatorID { get { return -1; } }
        public int ConditionID { get { return -1; } }
        public StreamDeckCommand DeckCommandType { get { return Command.DeckCommandType; } }
        public bool IsEncoder { get { return Command.IsEncoder; } }
        public int CommandID { get; set; } = cmdID;
        public string Address { get { return Command.Address; } }
        public SimCommandType CommandType { get { return Command.CommandType; } }
        public bool DoNotRequestBvar { get { return Command.DoNotRequestBvar; } }
        public string Name { get { return string.IsNullOrWhiteSpace(Command.Name) ? $"{CommandType}: {Address.Compact()}" : Command.Name; } }
        public bool IsValidValueType { get { return Command.IsValidValueType; } }
        public bool IsValueType { get { return Command.IsValueType; } }
        public bool IsValidCommand { get { return Command.IsValidCommand; } }
        public bool IsCommandType { get { return Command.IsCommandType; } }
        public bool HasCommandDelay { get { return SimCommand.CommandTypeUsesDelay(CommandType, Command.DoNotRequestBvar); } }
        public string TimeAfterLastDown { get { return Conversion.ToString(Command.TimeAfterLastDown); } }
        public bool ResetSwitch { get { return Command.ResetSwitch; } }
        public string ResetValue { get { return Command.ResetValue; } }
        public string ResetDelay { get { return Conversion.ToString(Command.ResetDelay); } }
        public bool UseCommandDelay { get { return Command.UseCommandDelay; } }
        public string CommandDelay { get { return Conversion.ToString(Command.CommandDelay); } }
        public string TickDelay { get { return Conversion.ToString(Command.TickDelay); } }
        public bool IsCode { get { return Command.IsCode; } }
        public string WriteValue { get { return Command.WriteValue; } }
        public bool AnyCondition { get { return Command.AnyCondition; } }
        public int ConditionCount { get { return Command.Conditions.Count; } }

        public void SetType(SimCommandType type)
        {
            Command.Settings.CommandType = type;
            ModelAction.UpdateAction();
        }

        public void SetDoNotRequestBvar(bool input)
        {
            Command.Settings.DoNotRequestBvar = input;
            ModelAction.UpdateAction();
        }

        public void SetAddress(string input)
        {
            if (input == null)
                return;

            Command.Settings.Address = input;
            ModelAction.UpdateAction();
        }

        public void SetName(string input)
        {
            if (input == null)
                return;

            Command.Settings.Name = input;
            ModelAction.UpdateAction();
        }

        public void SetTimeAfter(string input)
        {
            if (!Conversion.IsNumberI(input, out int numValue))
                return;

            Command.Settings.TimeAfterLastDown = numValue;
            ModelAction.UpdateAction();
        }

        public void SetResetSwitch(bool input)
        {
            Command.Settings.ResetSwitch = input;
            ModelAction.UpdateAction();
        }

        public void SetResetDelay(string input)
        {
            if (!Conversion.IsNumberI(input, out int numValue))
                return;

            Command.Settings.ResetDelay = numValue;
            ModelAction.UpdateAction();
        }

        public void SetUseDelay(bool input)
        {
            Command.Settings.UseCommandDelay = input;
            ModelAction.UpdateAction();
        }

        public void SetCommandDelay(string input)
        {
            if (!Conversion.IsNumberI(input, out int numValue))
                return;

            Command.Settings.CommandDelay = numValue;
            ModelAction.UpdateAction();
        }

        public void SetWriteValue(string input)
        {
            if (input == null)
                return;

            Command.Settings.WriteValue = input;
            ModelAction.UpdateAction();
        }

        public void SetResetValue(string input)
        {
            if (input == null)
                return;

            Command.Settings.ResetValue = input;
            ModelAction.UpdateAction();
        }

        public void SetAnyCondition(bool input)
        {
            Command.Settings.AnyCondition = input;
            ModelAction.UpdateAction();
        }

        public void SetTickDelay(string input)
        {
            if (!Conversion.IsNumberI(input, out int numValue))
                return;

            Command.Settings.TickDelay = numValue;
            ModelAction.UpdateAction();
        }
    }
}
