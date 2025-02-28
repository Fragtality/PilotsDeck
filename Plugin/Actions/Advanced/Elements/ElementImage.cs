using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Images;
using System.Drawing;

namespace PilotsDeck.Actions.Advanced.Elements
{
    public class ElementImage(ModelDisplayElement model, ActionMeta parent) : DisplayElement(model, parent)
    {
        public virtual ManagedImage Image { get; set; } = null;
        public virtual string ImageFile { get { return Settings.Image; } }
        public virtual bool DrawImageBackground { get { return Settings.DrawImageBackground; } }

        protected override RectangleF GetRectangle(Renderer render)
        {
            var size = render.GetImageSize(Image);
            if (Size.X > 0 && Size.Y > 0)
                return new RectangleF(Position.X, Position.Y, Size.X, Size.Y);
            else if (Size.Y > 0)
                return new RectangleF(Position.X, Position.Y, size.X, Size.Y);
            else if (Size.X > 0)
                return new RectangleF(Position.X, Position.Y, Size.X, size.Y);
            else
                return new RectangleF(Position.X, Position.Y, size.X, size.Y);
        }

        protected override bool IgnoreRender()
        {
            return base.IgnoreRender() || Image == null;
        }

        protected override void Render(Renderer render)
        {
            var drawRect = GetRectangle(render);
            if (DrawImageBackground)
                render.FillRectangle(Color, drawRect, Center, Scale);
            render.DrawImage(Image, drawRect, Center, Scale);
        }

        public override void RegisterRessources()
        {
            base.RegisterRessources();
            if (Image == null)
            {
                Logger.Verbose($"Register Image '{Settings.Image}'");
                Image = App.PluginController.ImageManager.RegisterImage(Settings.Image);
            }
        }

        public override void DeregisterRessources()
        {
            base.DeregisterRessources();
            if (Image != null)
            {
                Logger.Verbose($"Deregister Image '{Image.RequestedFile}'");
                App.PluginController.ImageManager.DeregisterImage(Image.RequestedFile);
                Image = null;
            }
        }
    }
}

