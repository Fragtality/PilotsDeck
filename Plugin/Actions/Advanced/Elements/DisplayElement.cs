using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PilotsDeck.Actions.Advanced.Elements
{
    public abstract class DisplayElement
    {
        public virtual ModelDisplayElement Settings { get; set; }
        public virtual ActionMeta Parent { get; set; }
        public virtual string Name { get { return Settings.Name; } }
        public virtual ConcurrentDictionary<int, ElementManipulator> ElementManipulators { get; set; } = [];
        public virtual PointF Position { get; set; } = new(0, 0);
        public virtual CenterType Center { get; set; } = CenterType.NONE;
        public virtual PointF Size { get; set; } = new(0, 0);
        public virtual ScaleType Scale { get; set; } = ScaleType.NONE;
        public virtual float Rotation { get; set; } = 0.0f;
        public virtual float Transparency { get; set; } = 1.0f;
        public virtual Color Color { get; set; } = Color.White;

        protected bool _visible = true;
        public virtual bool Visible { get { return _visible && Transparency > 0.0f; } set { _visible = value; } }

        public DisplayElement(ModelDisplayElement model, ActionMeta parent)
        {
            Settings = model;
            Position = model.GetPosition();
            Center = model.Center;
            Size = model.GetSize();
            Scale = model.Scale;
            Rotation = model.Rotation;
            Transparency = model.Transparency;
            Color = model.GetColor();
            Parent = parent;

            foreach (var manipulator in model.Manipulators)
            {
                var instance = ElementManipulator.CreateInstance(manipulator.Value.ManipulatorType, manipulator.Value, this);
                if (instance != null)
                    ElementManipulators.TryAdd(manipulator.Key, instance);
                else
                    Logger.Warning($"Could not create Instance for Manipulator '{manipulator.Value?.ManipulatorType}'");
            }
        }


        protected virtual RectangleF GetRectangle(Renderer render)
        {
            if (Size.X > 0 && Size.Y > 0)
                return new RectangleF(Position.X, Position.Y, Size.X, Size.Y);
            else if (Size.X <= 0)
                return new RectangleF(Position.X, Position.Y, render.DeviceCanvas.X, Size.Y);
            else if (Size.Y <= 0)
                return new RectangleF(Position.X, Position.Y, Size.X, render.DeviceCanvas.Y);
            else
                return new RectangleF(Position.X, Position.Y, render.DeviceCanvas.X, render.DeviceCanvas.Y);
        }

        public virtual void SetDefaultState()
        {
            Rotation = Settings.Rotation;
            Position = Settings.GetPosition();
            Size = Settings.GetSize();
        }

        public virtual bool HasChanges()
        {
            return ElementManipulators.Values.Where(m => m.HasChanges()).Any();
        }

        public virtual void RunManipulators()
        {
            SetDefaultState();

            foreach (var manipulator in ElementManipulators.Values)
                manipulator.ManipulateElement();
        }

        protected virtual bool IgnoreRender()
        {
            return !Visible;
        }

        public void RenderElement(Renderer render)
        {
            if (IgnoreRender())
                return;

            render.Transparency = Transparency;
            if (Rotation != 0)
                render.RotateRectangle(Rotation, GetRectangle(render), Center, Scale);

            Render(render);
            foreach (var manipulator in ElementManipulators.Values)
                manipulator.RenderManipulator(render);

            if (Rotation != 0)
                render.RotateRectangle(Rotation * -1, GetRectangle(render), Center, Scale);
            render.Transparency = 1.0f;
        }

        protected abstract void Render(Renderer render);

        public static DisplayElement CreateInstance(DISPLAY_ELEMENT type, ModelDisplayElement model, ActionMeta parent)
        {
            DisplayElement element = null;

            if (type == DISPLAY_ELEMENT.TEXT)
            {
                element = new ElementText(model ?? new(type), parent);
                if (element.Settings.IsNewModel)
                {
                    element.Settings.Size[0] = parent.CanvasSize.X;
                    element.Settings.Size[1] = parent.CanvasSize.Y;
                    if (parent.CanvasSize.Y == 100)
                        element.Settings.FontSize *= 1.5f;
                    else if (parent.CanvasSize.X == 144)
                        element.Settings.FontSize *= 2.0f;
                }
            }
            else if (type == DISPLAY_ELEMENT.VALUE)
            {
                element = new ElementValue(model ?? new(type), parent);
                if (element.Settings.IsNewModel)
                {
                    element.Settings.Size[0] = parent.CanvasSize.X;
                    element.Settings.Size[1] = parent.CanvasSize.Y;
                    if (parent.CanvasSize.Y == 100)
                        element.Settings.FontSize *= 1.5f;
                    else if (parent.CanvasSize.X == 144)
                        element.Settings.FontSize *= 2.0f;
                }
            }
            else if (type == DISPLAY_ELEMENT.IMAGE)
            {
                element = new ElementImage(model ?? new(type), parent);
            }
            else if (type == DISPLAY_ELEMENT.GAUGE)
            {
                element = new ElementGauge(model ?? new(type), parent);
                if (element.Settings.IsNewModel)
                {
                    if (parent.CanvasSize.X == 200)
                        element.Settings.Size = [50 * 1.5f, 8 * 1.5f];
                    else if (parent.CanvasSize.X == 144)
                        element.Settings.Size = [50 * 2, 8 * 2];
                    else
                        element.Settings.Size = [50, 8];
                    element.Settings.Color = "#006400";
                }
            }
            else if (type == DISPLAY_ELEMENT.PRIMITIVE)
            {
                element = new ElementPrimitive(model ?? new(type), parent);
                if (element.Settings.IsNewModel)
                {
                    if (parent.CanvasSize.X == 200)
                    {
                        element.Settings.Size = [60 * 1.5f, 60 * 1.5f];
                        element.Settings.LineSize *= 1.5f;
                    }
                    else if (parent.CanvasSize.X == 144)
                    {
                        element.Settings.Size = [60 * 2, 60 * 2];
                        element.Settings.LineSize *= 2.0f;
                    }
                    else
                    {
                        element.Settings.Size = [60, 60];
                    }
                    element.Settings.Center = CenterType.BOTH;
                }
            }

            return element;
        }

        public virtual int AddManipulator(ELEMENT_MANIPULATOR type, ModelManipulator model = null)
        {
            ElementManipulator instance = ElementManipulator.CreateInstance(type, model, this);
            if (instance == null)
            {
                Logger.Warning($"Could not create Instance for Manipulator '{type}'");
                return -1;
            }

            int id = ActionMeta.GetNextID(Settings.Manipulators.Keys);
            if (!Settings.Manipulators.TryAdd(id, instance.Settings))
            {
                Logger.Warning($"Could not add Instance '{type}' to Settings");
                return -1;
            }
            else
            {
                if (instance.ConditionStore.Conditions.IsEmpty
                    && instance is not ManipulatorIndicator
                    && instance is not ManipulatorTransparency
                    && (instance is not ManipulatorRotate && !instance.Settings.RotateContinous))
                {
                    instance.AddCondition(new ConditionHandler());
                    Logger.Debug($"Added {instance.GetType().Name} for ID '{id}'");
                }
            }

            return id;
        }

        public virtual void RemoveManipulator(int id)
        {
            if (!Settings.Manipulators.ContainsKey(id))
            {
                return;
            }
            Settings.Manipulators.Remove(id);
            var oldDict = Settings.Manipulators;
            Settings.Manipulators = [];
            int n = 0;
            foreach (var manipulator in oldDict.Values)
                Settings.Manipulators.TryAdd(n++, manipulator);
            Logger.Debug($"Removed ElementManipulator for ID '{id}'");
        }

        public virtual void RegisterRessources()
        {
            foreach (var manipulator in ElementManipulators.Values)
                manipulator.RegisterRessources();
        }

        public virtual void DeregisterRessources()
        {
            foreach (var manipulator in ElementManipulators.Values)
                manipulator.DeregisterRessources();
            ElementManipulators.Clear();
        }
    }
}
