using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;

namespace PilotsDeck.UI.ViewModels
{
    public interface ISelectableItem
    {
        public string Header { get; }
        public string Type { get; }
        public int ElementID { get; }
        public int ManipulatorID { get; }
        public int ConditionID { get; }
        public StreamDeckCommand DeckCommandType { get; }
        public int CommandID { get; }

        public virtual bool IsHeaderElement()
        {
            return Header == "Elements";
        }

        public virtual bool IsHeaderCommands()
        {
            return Header == "Commands";
        }

        public virtual bool IsHeaderManipulator()
        {
            return Header == "Manipulators" && ElementID != -1 && ManipulatorID == -1 && ConditionID == -1;
        }

        public virtual bool IsHeaderManipulatorConditions()
        {
            return Header == "Conditions" && ElementID != -1 && ManipulatorID != -1 && ConditionID == -1;
        }

        public virtual bool IsHeaderActionType()
        {
            return !string.IsNullOrEmpty(Header) && DeckCommandType != (StreamDeckCommand)(-1) && CommandID == -1 && ConditionID == -1;
        }

        public virtual bool IsHeaderActionConditions()
        {
            return Header == "Conditions" && DeckCommandType != (StreamDeckCommand)(-1) && CommandID != -1 && ConditionID == -1;
        }

        public virtual bool IsDisplayElement()
        {
            return Header == "" && ElementID != -1 && ManipulatorID == -1 && ConditionID == -1;
        }

        public virtual bool IsElementManipulator()
        {
            return Header == "" && ElementID != -1 && ManipulatorID != -1 && ConditionID == -1;
        }

        public virtual bool IsManipulatorCondition()
        {
            return Header == "" && ElementID != -1 && ManipulatorID != -1 && ConditionID != -1;
        }

        public virtual bool IsActionCommand()
        {
            return Header == "" && DeckCommandType != (StreamDeckCommand)(-1) && CommandID != -1 && ConditionID == -1;
        }

        public virtual bool IsActionCondition()
        {
            return Header == "" && DeckCommandType != (StreamDeckCommand)(-1) && CommandID != -1 && ConditionID != -1;
        }

        public virtual bool IsTypeElementGauge()
        {
            return Type == $"{DISPLAY_ELEMENT.GAUGE}";
        }

        public virtual bool IsTypeElementValue()
        {
            return Type == $"{DISPLAY_ELEMENT.VALUE}";
        }
    }
}
