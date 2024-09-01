using PilotsDeck.StreamDeck.Messages;

namespace PilotsDeck.Actions.Simple
{
    public class ActionDisplaySwitch(StreamDeckEvent sdEvent) : ActionDisplayText(sdEvent)
    {
        protected override void CheckSettings()
        {
            if (!Settings.HasAction)
            {
                Settings.HasAction = true;
                SettingModelUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(Settings.DefaultImage))
            {
                Settings.DefaultImage = @"Images/Empty.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.IndicationImage))
            {
                Settings.IndicationImage = @"Images/Empty.png";
                SettingModelUpdated = true;
            }
        }
    }
}
