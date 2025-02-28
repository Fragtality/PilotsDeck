using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Plugin.Render;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace PilotsDeck.Actions.Advanced.SettingsModel
{
    public enum PrimitiveType
    {
        LINE = 1,
        RECTANGLE,
        RECTANGLE_FILLED,
        CIRCLE,
        CIRCLE_FILLED,
    }

    public class ModelDisplayElement(DISPLAY_ELEMENT type)
    {
        [JsonConstructor]
        public ModelDisplayElement() : this(DISPLAY_ELEMENT.IMAGE)
        {

        }

        public ModelDisplayElement Copy()
        {
            ModelDisplayElement model = new(this.ElementType)
            {
                IsNewModel = this.IsNewModel,
                Name = this.Name,
                Position = [this.Position[0], this.Position[1]],
                Center = this.Center,
                Size = [this.Size[0], this.Size[1]],
                Scale = this.Scale,
                Rotation = this.Rotation,
                Color = this.Color,
                FontName = this.FontName,
                FontSize = this.FontSize,
                FontStyle = this.FontStyle,
                Text = this.Text,
                TextHorizontalAlignment = this.TextHorizontalAlignment,
                TextVerticalAlignment = this.TextVerticalAlignment,
                ValueAddress = this.ValueAddress,
                ValueFormat = this.ValueFormat.Copy(),
                Image = this.Image,
                DrawImageBackground = this.DrawImageBackground,
                GaugeValueScale = this.GaugeValueScale,
                GaugeValueMin = this.GaugeValueMin,
                GaugeValueMax = this.GaugeValueMax,
                GaugeSizeAddress = this.GaugeSizeAddress,
                UseGaugeDynamicSize = this.UseGaugeDynamicSize,
                GaugeRevereseDirection = this.GaugeRevereseDirection,
                GaugeFixedRanges = this.GaugeFixedRanges,
                GaugeFixedMarkers = this.GaugeFixedMarkers,
                GaugeIsArc = this.GaugeIsArc,
                GaugeAngleStart = this.GaugeAngleStart,
                GaugeAngleSweep = this.GaugeAngleSweep,
                PrimitiveType = this.PrimitiveType,
                LineSize = this.LineSize,
            };

            foreach (var manipulator in Manipulators)
                model.Manipulators.Add(manipulator.Key, manipulator.Value.Copy());

            foreach (var range in GaugeColorRanges)
                model.GaugeColorRanges.Add(new ColorRange(range));

            foreach (var marker in GaugeMarkers)
                model.GaugeMarkers.Add(new MarkerDefinition(marker));

            foreach (var markerRange in GaugeRangeMarkers)
                model.GaugeRangeMarkers.Add(new MarkerRangeDefinition(markerRange));

            return model;
        }

        public virtual int AddManipulator(ELEMENT_MANIPULATOR type, ModelManipulator model = null)
        {
            ElementManipulator instance = ElementManipulator.CreateInstance(type, model, null);
            if (instance == null)
            {
                Logger.Warning($"Could not create Instance for Manipulator '{type}'");
                return -1;
            }

            int id = ActionMeta.GetNextID(Manipulators.Keys);
            if (!Manipulators.TryAdd(id, instance.Settings))
            {
                Logger.Warning($"Could not add Instance '{type}' to Settings");
                return -1;
            }
            else if (instance.Settings.IsNewModel)
            {
                if (instance is not ManipulatorIndicator
                    && instance is not ManipulatorTransparency
                    && instance is not ManipulatorRotate)
                {
                    instance.AddCondition(new ConditionHandler() { Value = "0" });
                    Logger.Debug($"Added {instance.GetType().Name} for ID '{id}'");
                }
            }

            return id;
        }

        public virtual bool IsNewModel { get; set; } = true;
        public virtual DISPLAY_ELEMENT ElementType { get; set; } = type;
        public virtual string Name { get; set; } = "";
        public virtual SortedDictionary<int, ModelManipulator> Manipulators { get; set; } = [];
        public virtual float[] Position {  get; set; } = [0, 0];
        public virtual CenterType Center { get; set; } = CenterType.NONE;
        public virtual float[] Size { get; set; } = [0, 0];
        public virtual ScaleType Scale { get; set; } = ScaleType.NONE;
        public virtual float Rotation { get; set; } = 0;
        public virtual float Transparency { get; set; } = 1.0f;
        public virtual string Color { get; set; } = "#ffffff";

        public virtual PointF GetPosition()
        {
            return new PointF(Position[0], Position[1]);
        }

        public virtual void SetPosition(PointF pos)
        {
            Position[0] = pos.X;
            Position[1] = pos.Y;
        }

        public virtual PointF GetSize()
        {
            return new PointF(Size[0], Size[1]);
        }

        public virtual void SetSize(PointF size)
        {
            Size[0] = size.X;
            Size[1] = size.Y;
        }

        public virtual Color GetColor()
        {
            return ColorTranslator.FromHtml(Color);
        }

        public virtual void SetColor(Color color)
        {
            Color = ColorTranslator.ToHtml(color);
        }

        //ElementText
        public virtual string FontName { get; set; } = "Arial";
        public virtual float FontSize { get; set; } = 12;
        public virtual FontStyle FontStyle { get; set; } = FontStyle.Regular;
        public virtual string Text { get; set; } = "";
        public virtual StringAlignment TextHorizontalAlignment { get; set; } = StringAlignment.Center;
        public virtual StringAlignment TextVerticalAlignment { get; set; } = StringAlignment.Center;

        public virtual Font GetFont()
        {
            return new Font(FontName, FontSize, FontStyle);
        }

        public virtual void SetFont(Font font)
        {
            FontName = font.Name.Replace("SemiLight","").Replace("Light", "").Replace("SemiCondensed", "").Replace("Condensed", "").Replace("SemiBold", "").Replace("Bold", "").Replace("SemiConden", "").Replace("SemiConde", "").TrimEnd();
            FontSize = font.Size;
            FontStyle = font.Style;
        }

        //ElementValue
        public virtual string ValueAddress { get; set; } = "";
        public virtual ValueFormat ValueFormat { get; set; } = new();

        //ElementImage
        public virtual string Image { get; set; } = "Images/Empty.png";
        public virtual bool DrawImageBackground { get; set; } = false;

        //ElementGauge
        public virtual float GaugeValueMin { get; set; } = 0;
        public virtual float GaugeValueMax { get; set; } = 100;
        public virtual float GaugeValueScale { get; set; } = 1;
        public virtual string GaugeSizeAddress { get; set; } = "";
        public virtual bool UseGaugeDynamicSize { get; set; } = false;
        public virtual bool GaugeRevereseDirection { get; set; } = false;
        public virtual bool GaugeFixedRanges { get; set; } = false;
        public virtual List<ColorRange> GaugeColorRanges { get; set; } = [];
        public virtual bool GaugeFixedMarkers { get; set; } = true;
        public virtual List<MarkerDefinition> GaugeMarkers { get; set; } = [];
        public virtual List<MarkerRangeDefinition> GaugeRangeMarkers { get; set; } = [];
        public virtual bool GaugeIsArc { get; set; } = false;
        public virtual float GaugeAngleStart { get; set; } = 135;
        public virtual float GaugeAngleSweep { get; set; } = 180;

        public virtual void GetRanges(out Color[] colors, out float[][] ranges)
        {
            List<Color> listColors = [];
            List<float[]> listRanges = [];

            foreach (ColorRange range in GaugeColorRanges)
            {
                listColors.Add(range.GetColor());
                listRanges.Add(range.Range);
            }

            colors = [..listColors];
            ranges = [..listRanges];
        }

        //ElementPrimitive
        public PrimitiveType PrimitiveType { get; set; } = PrimitiveType.RECTANGLE;
        public float LineSize { get; set; } = 2;
    }
}
