using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using System;

namespace PilotsDeck.UI.ViewModels
{
    public class ViewModelCondition(ConditionHandler condition, ViewModelAction action, int conditionID, int elementID = -1, int manipulatorID = -1, StreamDeckCommand typeID = (StreamDeckCommand)(-1), int cmdID = -1) : ISelectableItem
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ConditionHandler Condition { get; set; } = condition;
        protected ConditionHandler Settings
        {
            get
            {
                try
                {
                    if (ManipulatorID != -1)
                        return ModelAction.Action.Settings.DisplayElements[ElementID].Manipulators[ManipulatorID].Conditions[ConditionID];
                    else if (CommandID != -1)
                        return ModelAction.Action.Settings.ActionCommands[DeckCommandType][CommandID].Conditions[ConditionID];
                    else
                        return new ConditionHandler();
                }
                catch(Exception ex)
                {
                    Logger.LogException(ex);
                    return new ConditionHandler();
                }
            }
        }
        public string Header { get { return ""; } }
        public string Type { get { return ""; } }
        public int ElementID { get; set; } = elementID;
        public int ManipulatorID { get; set; } = manipulatorID;
        public int ConditionID { get; set; } = conditionID;
        public StreamDeckCommand DeckCommandType { get; set; } = typeID;
        public int CommandID { get; set; } = cmdID;
        public bool IsManipulatorCondition { get { return ElementID != -1 && ManipulatorID != -1; } }
        public string Name { get { return $"'{Condition.Address.Compact()}' {TranslateComparison(Condition.Comparison)} '{Condition.Value}'"; } }
        public string Address { get { return Condition.Address; } }
        public bool IsValidAddress { get { return Condition?.Variable?.Type != Resources.Variables.SimValueType.NONE; } }
        public Comparison Compare { get { return Condition.Comparison; } }
        public string Value { get { return Condition.Value; } }

        public void SetAddress(string input)
        {
            Settings.Address = input;
            ModelAction.UpdateAction();
        }

        public void SetComparison(Comparison comparison)
        {
            Settings.Comparison = comparison;
            ModelAction.UpdateAction();
        }

        public void SetValue(string input)
        {
            Settings.Value = input;
            ModelAction.UpdateAction();
        }

        public static string TranslateComparison(Comparison comparison)
        {
            if (comparison == Comparison.LESS)
                return "<";
            else if (comparison == Comparison.LESS_EQUAL)
                return "<=";
            else if (comparison == Comparison.GREATER)
                return ">";
            else if (comparison == Comparison.GREATER_EQUAL)
                return ">=";
            else if (comparison == Comparison.EQUAL)
                return "==";
            else if (comparison == Comparison.NOT_EQUAL)
                return "!=";
            else if (comparison == Comparison.CONTAINS)
                return "contains";
            else if (comparison == Comparison.NOT_CONTAINS)
                return "not contains";
            else if (comparison == Comparison.HAS_CHANGED)
                return "has changed";
            else
                return "";
        }
    }
}
