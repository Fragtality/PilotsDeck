using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using System;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public class ManipulatorVisible(ModelManipulator model, DisplayElement parent) : ElementManipulator(model, parent)
    {
        public virtual DateTime TimeChanged { get; set; } = DateTime.MinValue;
        public virtual bool LastCompare { get; set; } = false;

        public override bool HasChanges()
        {
            return base.HasChanges() || (Settings.ResetVisibility && (DateTime.Now - TimeChanged) >= TimeSpan.FromMilliseconds(Settings.ResetDelay));
        }

        public override void ManipulateElement()
        {
            var timeCompare = (DateTime.Now - TimeChanged) >= TimeSpan.FromMilliseconds(Settings.ResetDelay);
            bool compares = ConditionStore.Compare();

            if (Settings.ResetVisibility)
            {
                if (timeCompare && Element.Visible)
                {
                    HideElement();
                }
                else if (compares && !LastCompare)
                {
                    if (!Element.Visible)
                        ShowElement();
                    else
                        TimeChanged = DateTime.Now;
                }
            }
            else
            {
                if (compares && !Element.Visible)
                    ShowElement();
                else if (!compares && Element.Visible)
                    HideElement();
            }

            LastCompare = compares;
        }

        protected void ShowElement()
        {
            Logger.Verbose($"Showing Element {Element.Name}");
            Element.Visible = true;
            if (Settings.ResetVisibility)
                TimeChanged = DateTime.Now;
        }

        protected void HideElement()
        {
            Logger.Verbose($"Hiding Element {Element.Name}");
            Element.Visible = false;
            TimeChanged = DateTime.MaxValue;
        }
    }
}
