using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using System.Drawing;

namespace PilotsDeck.Actions.Advanced.Elements
{
    public class ElementText(ModelDisplayElement model, ActionMeta parent) : DisplayElement(model, parent)
    {
        public virtual Font Font { get { return Settings.GetFont(); } }
        public virtual string Text { get { return Settings.Text; } }

        protected override void Render(Renderer render)
        {
            render.DrawText(Text ?? "", Font, Color, GetRectangle(render), Center, Scale, Settings.TextHorizontalAlignment, Settings.TextVerticalAlignment);
        }
    }
}
