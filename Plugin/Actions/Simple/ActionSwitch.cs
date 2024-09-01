using PilotsDeck.Plugin.Render;
using PilotsDeck.StreamDeck.Messages;
using System;

namespace PilotsDeck.Actions.Simple
{
    public class ActionSwitch(StreamDeckEvent sdEvent) : ActionBaseSimple(sdEvent)
    {
        public override string ActionID { get { return $"{this.GetType().Name} (Title: {Title} | Command: {Settings.AddressAction})"; } }

        protected override void CheckSettings()
        {
            if (!Settings.HasAction)
            {
                Settings.HasAction = true;
                SettingModelUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(Settings.DefaultImage))
            {
                Settings.DefaultImage = @"Images/Switch.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.DefaultRect))
            {
                Settings.DefaultRect = "72; 72";
                SettingModelUpdated = true;
            }
        }

        public override void Refresh()
        {
            try
            {
                if (!RessourceStore.HasChanges() && !NeedRefresh)
                    return;

                Renderer render = new(CanvasInfo);
                render.DrawImage(RessourceStore.GetImage(ImageID.Background), Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);

                if (Settings.HasAction && Settings.IsGuarded)
                    RenderGuard(render, null);

                DrawTitle(render);

                RenderImage64 = render.RenderImage64();
                render.Dispose();
                NeedRedraw = true;
                NeedRefresh = false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                NeedRefresh = true;
            }
        }
    }
}
