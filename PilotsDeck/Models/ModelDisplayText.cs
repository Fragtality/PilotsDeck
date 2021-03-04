using System;
using System.Drawing;

namespace PilotsDeck
{
    public class ModelDisplayText : ModelDisplay
    {
		public virtual bool DrawBox { get; set; } = true;
		public virtual int BoxSize { get; set; } = 2;
		public virtual string BoxColor { get; set; } = "#ffffff";
		public virtual string BoxRect { get; set; } = "9; 21; 54; 44";

		public virtual bool HasIndication { get; set; } = false;
		public virtual bool IndicationHideValue { get; set; } = false;
		public virtual bool IndicationUseColor { get; set; } = false;
		public virtual string IndicationColor { get; set; } = "#ffcc00";
		public virtual string IndicationImage { get; set; } = @"Images/Empty.png";
		public virtual string IndicationValue { get; set; } = "0";

		public virtual bool FontInherit { get; set; } = true;
		public virtual string FontName { get; set; } = "Arial";
		public virtual int FontSize { get; set; } = 10;
		public virtual int FontStyle { get; set; } = (int)System.Drawing.FontStyle.Regular;
		public virtual string FontColor { get; set; } = "#ffffff";
		public virtual string RectCoord { get; set; } = "-1; 0; 0; 0";


		public ModelDisplayText()
		{
			DefaultImage = @"Images/Empty.png";
			ErrorImage = @"Images/Error.png";
		}		

		public virtual void GetFontParameters(StreamDeckTools.StreamDeckTitleParameters titleParameters, out Font drawFont, out Color drawColor)
        {
			if (FontInherit && titleParameters != null)
            {
				drawFont = StreamDeckTools.ConvertFontParameter(titleParameters);
				drawColor = StreamDeckTools.ConvertColorParameter(titleParameters);
            }
			else
            {
				drawFont = new Font(FontName, FontSize, (FontStyle)FontStyle);
				drawColor = ColorTranslator.FromHtml(FontColor);
			}
        }

		public virtual void ResetRectText()
        {
			if (DrawBox)
				RectCoord = "-1; 0; 0; 0";
			else
				RectCoord = "-1; 1; 72; 72";

		}

		public virtual RectangleF GetRectangleBox()
        {
			return GetRectangleF(BoxRect);
        }

		public virtual RectangleF GetRectangleText()
        {
			if (!DrawBox)
				return GetRectangleF(RectCoord);
			else
			{
				RectangleF box = GetRectangleF(BoxRect);
				RectangleF text = GetRectangleF(RectCoord);
				float size = (float)Math.Round(BoxSize / 2.0d, 0, MidpointRounding.ToEven);
				text.X = text.X + box.X + size;
				text.Y = text.Y + box.Y + size;
				text.Width = text.Width + box.Width - size * 2.0f;
				text.Height = text.Height + box.Height - size * 2.0f;

				return text;
			}
		}

		public static Rectangle GetRectangle(string rect)
        {
			return Rectangle.Round(GetRectangleF(rect));
        }

		public static RectangleF GetRectangleF(string rect)
        {
			//x y w h
			string[] parts = rect.Trim().Split(';');
			if (parts.Length == 4)
			{
				int parses = 0;
				for (int i = 0; i < parts.Length; i++)
					if (float.TryParse(parts[i], out _))
						parses++;

				if (parses == parts.Length)
					return new RectangleF(Convert.ToSingle(parts[0]), Convert.ToSingle(parts[1]), Convert.ToSingle(parts[2]), Convert.ToSingle(parts[3]));
				else
					return new RectangleF(11, 23, 48, 40); //-1 -1 -2 0 from actual
			}
			return new RectangleF(11, 23, 48, 40);
		}
	}
}
