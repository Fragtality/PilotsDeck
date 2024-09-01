using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using System.Drawing;

namespace PilotsDeck.Actions.Advanced.Elements
{
    public class ElementPrimitive(ModelDisplayElement model, ActionMeta parent) : DisplayElement(model, parent)
    {
        protected override void Render(Renderer render)
        {
            switch (Settings.PrimitiveType)
            {
                case PrimitiveType.RECTANGLE:
                    render.DrawRectangle(Color, Settings.LineSize, GetRectangle(render), Center, Scale);
                    break;
                case PrimitiveType.RECTANGLE_FILLED:
                    render.FillRectangle(Color, GetRectangle(render), Center, Scale);
                    break;
                case PrimitiveType.CIRCLE:
                    render.DrawEllipse(Color, Settings.LineSize, GetRectangle(render), Center, Scale);
                    break;
                case PrimitiveType.CIRCLE_FILLED:
                    render.FillEllipse(Color, GetRectangle(render), Center, Scale);
                    break;
                default:
                    render.DrawLine(Color, Settings.LineSize, new RectangleF(Position.X, Position.Y, Size.X, Size.Y), Center, Scale);
                    break;
            }
        }
    }
}
