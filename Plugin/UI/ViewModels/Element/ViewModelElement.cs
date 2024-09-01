using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Tools;
using System.Globalization;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PilotsDeck.UI.ViewModels.Element
{
    public class ViewModelElement(DisplayElement element, ViewModelAction action, int elementID) : ISelectableItem
    {
        public virtual ViewModelAction ModelAction { get; set; } = action;
        public virtual DisplayElement Element { get; set; } = element;
        public virtual string Header { get { return ""; } }
        public DISPLAY_ELEMENT ElementType { get { return Element.Settings.ElementType; } }
        public string Type { get { return $"{Element.Settings.ElementType}"; } }
        public virtual int ElementID { get; set; } = elementID;
        public virtual int ManipulatorID { get { return -1; } }
        public virtual int ConditionID { get { return -1; } }
        public virtual StreamDeckCommand DeckCommandType { get { return (StreamDeckCommand)(-1); } }
        public virtual int CommandID { get { return -1; } }
        public virtual bool IsText { get { return Element is ElementText; } }
        public virtual bool IsValue { get { return Element is ElementValue; } }
        public virtual bool IsImage { get { return Element is ElementImage; } }
        public virtual bool IsGauge { get { return Element is ElementGauge; } }
        public virtual bool IsPrimitive { get { return Element is ElementPrimitive; } }
        public virtual string Name { get { return SelectName(); } }
        public virtual string RawName { get { return Element.Settings.Name; } }
        public virtual string PosX { get { return Conversion.ToString(Element.Settings.Position[0]); } }
        public virtual string PosY { get { return Conversion.ToString(Element.Settings.Position[1]); } }
        public virtual System.Windows.Point Pos { get { return new(Element.Settings.Position[0], Element.Settings.Position[1]); } }
        public virtual string CanvasX { get { return Conversion.ToString(Element.Parent.CanvasSize.X); } }
        public virtual string CanvasY { get { return Conversion.ToString(Element.Parent.CanvasSize.Y); } }
        public virtual CenterType Center { get { return Element.Settings.Center; } }
        public virtual string Width { get { return Conversion.ToString(Element.Settings.Size[0]); } }
        public virtual string Height { get { return Conversion.ToString(Element.Settings.Size[1]); } }
        public virtual System.Windows.Point Size { get { return new(Element.Settings.Size[0], Element.Settings.Size[1]); } }
        public virtual ScaleType Scale { get { return Element.Settings.Scale; } }
        public virtual string Rotation { get { return Conversion.ToString(Element.Settings.Rotation); } }
        public virtual string Transparency { get { return string.Format(CultureInfo.InvariantCulture, "{0:F1}", Element.Settings.Transparency); } }
        public virtual float RotationNum { get { return Element.Settings.Rotation; } }
        public virtual Color Color { get { return Color.FromArgb(Element.Settings.GetColor().A, Element.Settings.GetColor().R, Element.Settings.GetColor().G, Element.Settings.GetColor().B); } }
        public virtual System.Drawing.Color ColorForms { get { return Element.Settings.GetColor(); } }
        public virtual string FontInfo { get { return $"{Element.Settings.FontName}, {Element.Settings.FontSize:F0}, {Element.Settings.FontStyle}"; } }
        public virtual System.Drawing.Font FontForms { get { return Element.Settings.GetFont(); } }
        public virtual FontFamily FontFamily { get { return new FontFamily(Element.Settings.FontName); } }
        public virtual float FontSize { get { return Element.Settings.FontSize; } }
        public virtual string ElementText { get { return Element.Settings.Text; } }
        public virtual bool IsHorizontalLeft { get { return Element.Settings.TextHorizontalAlignment == System.Drawing.StringAlignment.Near; } }
        public virtual bool IsHorizontalCenter { get { return Element.Settings.TextHorizontalAlignment == System.Drawing.StringAlignment.Center; } }
        public virtual bool IsHorizontalRight { get { return Element.Settings.TextHorizontalAlignment == System.Drawing.StringAlignment.Far; } }
        public virtual bool IsVerticalTop { get { return Element.Settings.TextVerticalAlignment == System.Drawing.StringAlignment.Near; } }
        public virtual bool IsVerticalCenter { get { return Element.Settings.TextVerticalAlignment == System.Drawing.StringAlignment.Center; } }
        public virtual bool IsVerticalBottom { get { return Element.Settings.TextVerticalAlignment == System.Drawing.StringAlignment.Far; } }
        public virtual string ValueAddress { get { return Element.Settings.ValueAddress; } }
        public virtual ValueFormat DisplayValue { get { return (Element as ElementValue).Settings.ValueFormat; } }
        public virtual BitmapImage ImageSource
        {
            get
            {
                return Sys.GetBitmapFromFile(ImageFile) ?? Sys.GetBitmapFromFile(AppConfiguration.WaitImage);
            }
        }
        public virtual string ImageFile { get { return Element.Settings.Image; } }
        public virtual bool DrawImageBackground { get { return Element.Settings.DrawImageBackground; } }
        public virtual PrimitiveType Primitive { get { return Element.Settings.PrimitiveType; } }
        public virtual string LineSize { get { return Conversion.ToString(Element.Settings.LineSize); } }
        public virtual float LineSizeNum { get { return Element.Settings.LineSize; } }

        public virtual string GetImageInfo()
        {
            StringBuilder sb = new();

            if (Element is ElementImage element && element?.Image?.Images?.Values != null)
            {
                foreach (var img in element.Image.Images)
                {
                    sb.AppendLine($"{element.Image.FileNames[img.Key]}: {img.Value.Width} x {img.Value.Height}");
                }
            }

            return sb.ToString();
        }

        public virtual string SelectName()
        {
            if (!string.IsNullOrWhiteSpace(Element.Name))
                return Element.Name;
            else if (Element is ElementText text && !string.IsNullOrWhiteSpace(text.Settings?.Text))
                return text.Text.Compact();
            else if (Element is ElementValue value && !string.IsNullOrWhiteSpace(value.Variable?.Address))
                return value.Variable.Address.Compact();
            else if (Element is ElementImage image && !string.IsNullOrWhiteSpace(image.Settings?.Image))
            {
                int idx = image.Settings.Image.LastIndexOf('/');
                if (idx != -1 && idx + 1 < image.Settings.Image.Length)
                    return image.Settings.Image[(idx + 1)..].Compact();
                else
                    return image.Settings.Image.Compact();
            }
            else
                return Element.GetType().Name.Replace("Element", "");
        }

        public virtual void SetName(string name)
        {
            Element.Settings.Name = name;
            ModelAction.UpdateAction();
        }

        public virtual void SetPos(System.Windows.Point point)
        {
            Element.Settings.Position[0] = (float)point.X;
            Element.Settings.Position[1] = (float)point.Y;
            ModelAction.UpdateAction();
        }

        public virtual void SetPosF(System.Drawing.PointF point)
        {
            Element.Settings.Position[0] = point.X;
            Element.Settings.Position[1] = point.Y;
            ModelAction.UpdateAction();
        }

        public virtual void SetPosX(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Element.Settings.Position[0] = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetPosY(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Element.Settings.Position[1] = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetSize(System.Windows.Point point)
        {
            Element.Settings.Size[0] = (float)point.X;
            Element.Settings.Size[1] = (float)point.Y;
            ModelAction.UpdateAction();
        }

        public virtual void SetSizeF(System.Drawing.PointF point)
        {
            Element.Settings.Size[0] = point.X;
            Element.Settings.Size[1] = point.Y;
            ModelAction.UpdateAction();
        }

        public virtual void SetWidth(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Element.Settings.Size[0] = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetHeight(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Element.Settings.Size[1] = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetCenter(CenterType center)
        {
            Element.Settings.Center = center;
            ModelAction.UpdateAction();
        }

        public virtual void SetScale(ScaleType scale)
        {
            Element.Settings.Scale = scale;
            ModelAction.UpdateAction();
        }

        public virtual void SetRotation(float angle)
        {
            Element.Settings.Rotation = angle;
            ModelAction.UpdateAction();
        }

        public virtual void SetRotation(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Element.Settings.Rotation = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetTransparency(string input)
        {
            if (Conversion.IsNumberF(input, out float num) && num >= 0.0 && num <= 1.0f)
            {
                Element.Settings.Transparency = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetColor(System.Drawing.Color color)
        {
            Element.Settings.SetColor(color);
            ModelAction.UpdateAction();
        }

        public virtual void SetFont(System.Drawing.Font font)
        {
            Element.Settings.SetFont(font);
            ModelAction.UpdateAction();
        }

        public virtual void SetFontSize(float size)
        {
            Element.Settings.FontSize = size;
            ModelAction.UpdateAction();
        }

        public virtual FontSetting GetFontSettings()
        {
            return new FontSetting()
            {
                Font = Element.Settings.GetFont(),
                HorizontalAlignment = Element.Settings.TextHorizontalAlignment,
                VerticalAlignment = Element.Settings.TextVerticalAlignment,
            };
        }

        public virtual void SetFontSettings(FontSetting settings)
        {
            Element.Settings.SetFont(settings.Font);
            Element.Settings.TextHorizontalAlignment = settings.HorizontalAlignment;
            Element.Settings.TextVerticalAlignment = settings.VerticalAlignment;
            ModelAction.UpdateAction();
        }

        public virtual void SetTextHorizontalAlignment(System.Drawing.StringAlignment alignment)
        {
            Element.Settings.TextHorizontalAlignment = alignment;
            ModelAction.UpdateAction();
        }

        public virtual void SetTextVerticalAlignment(System.Drawing.StringAlignment alignment)
        {
            Element.Settings.TextVerticalAlignment = alignment;
            ModelAction.UpdateAction();
        }

        public virtual void SetText(string text)
        {
            Element.Settings.Text = text;
            ModelAction.UpdateAction();
        }

        public virtual void SetImage(string image)
        {
            Element.Settings.Image = image;
            ModelAction.UpdateAction();
        }

        public virtual void SetImageBackground(bool input)
        {
            Element.Settings.DrawImageBackground = input;
            ModelAction.UpdateAction();
        }

        public virtual void SetLineSize(float num)
        {
            Element.Settings.LineSize = num;
            ModelAction.UpdateAction();
        }

        public virtual void SetLineSize(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Element.Settings.LineSize = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetPrimitiveType(PrimitiveType input)
        {
            Element.Settings.PrimitiveType = input;
            ModelAction.UpdateAction();
        }
    }
}

