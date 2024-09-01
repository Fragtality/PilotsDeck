using PilotsDeck.Actions;

namespace PilotsDeck.UI.ViewModels
{
    public class StaticItem(int elementID = -1, int manipulatorID = -1, int conditionID = -1, StreamDeckCommand deckType = (StreamDeckCommand)(-1), int cmdID = -1) : ISelectableItem
    {
        public string Header { get; set; } = "";
        public string Type { get { return ""; } }
        public int ElementID { get; set; } = elementID;
        public int ManipulatorID { get; set; } = manipulatorID;
        public int ConditionID { get; set; } = conditionID;
        public StreamDeckCommand DeckCommandType { get; set; } = deckType;
        public int CommandID { get; set; } = cmdID;
    }
}
