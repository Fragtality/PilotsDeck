using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using System.Drawing;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public class ManipulatorColor(ModelManipulator model, DisplayElement parent) : ElementManipulator(model, parent)
    {
        public virtual Color ConditionalColor { get { return Settings.GetColor(); } }

        public override void ManipulateElement()
        {
            if (ConditionStore.Compare())
            {
                Element.Color = ConditionalColor;
                Logger.Verbose($"Color set to {ColorTranslator.ToHtml(Element.Color)}");
            }
        }
    }
}
