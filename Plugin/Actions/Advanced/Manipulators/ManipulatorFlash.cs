using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using System;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public class ManipulatorFlash(ModelManipulator model, DisplayElement parent) : ElementManipulator(model, parent)
    {
        public virtual DateTime NextChange { get; set; } = DateTime.MinValue;
        public virtual bool IsFlashing { get; set; } = false;

        public override bool HasChanges()
        {
            return base.HasChanges() || (IsFlashing && NextChange <= DateTime.Now) || (Settings.FlashResetOnInteraction && Element.Parent.HasStreamDeckInteraction);
        }

        public override void ManipulateElement()
        {
            if (!IsFlashing && ConditionStore.Compare())
            {
                Logger.Debug($"Start Flashing for Action {Element.Parent.ActionID} - Context {Element.Parent.Context}");
                IsFlashing = true;
                NextChange = DateTime.MinValue;
            }

            if (IsFlashing)
            {
                if ((!Settings.FlashResetOnInteraction && !ConditionStore.Compare()) || (Settings.FlashResetOnInteraction && Element.Parent.HasStreamDeckInteraction))
                {
                    Logger.Debug($"Stop Flashing for Action {Element.Parent.ActionID} - Context {Element.Parent.Context}");
                    IsFlashing = false;
                    if (!Settings.FlashDoNotHideOnStop)
                        Element.Visible = false;
                    else
                        Element.Visible = true;
                }
                else if (NextChange <= DateTime.Now)
                {
                    Element.Visible = !Element.Visible;
                    NextChange = DateTime.Now + TimeSpan.FromMilliseconds(Settings.FlashInterval);
                }
            }
            else
            {
                if (Element.Visible && !Settings.FlashDoNotHideOnStop)
                    Element.Visible = false;
                else if (!Element.Visible && Settings.FlashDoNotHideOnStop)
                    Element.Visible = true;
            }
        }
    }
}
