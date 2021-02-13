using System;
using System.Drawing;

namespace PilotsDeck
{
    public class ModelDisplayText : ModelDisplay
    {
		public virtual bool HasIndication { get; set; } = false;
		public virtual bool IndicationHideValue { get; set; } = false;
		public virtual bool IndicationUseColor { get; set; } = false;
		public virtual string IndicationColor { get; set; } = "#ffffff";
		public virtual string IndicationImage { get; set; } = @"Images/ValueFault.png";
		public virtual string IndicationValue { get; set; } = "0";

		public virtual bool FontInherit { get; set; } = true;
		public virtual string FontName { get; set; } = "Arial";
		public virtual int FontSize { get; set; } = 10;
		public virtual int FontStyle { get; set; } = (int)System.Drawing.FontStyle.Regular;
		public virtual string FontColor { get; set; } = "#ffffff";
		public virtual string RectCoord { get; set; } = "11; 23; 48; 40";


		public ModelDisplayText()
		{
			DefaultImage = @"Images/ValueFrame.png";
			ErrorImage = @"Images/ValueError.png";
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

		public virtual RectangleF GetRectangle()
        {
			return GetRectangle(RectCoord);
		}

		public static RectangleF GetRectangle(string rect)
        {
			//x y w h
			string[] parts = rect.Trim().Split(';');
			if (parts.Length == 4)
			{
				int parses = 0;
				for (int i = 0; i < parts.Length; i++)
					if (int.TryParse(parts[i], out _))
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
