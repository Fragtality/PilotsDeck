using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public class ManipulatorFormat(ModelManipulator model, DisplayElement parent) : ElementManipulator(model, parent)
    {
        public override void ManipulateElement()
        {
            if (Element is not ElementValue element)
                return;

            if (ConditionStore.Compare())
                element.ValueFormat = Settings.ConditionalFormat.Copy();
        }
    }
}
