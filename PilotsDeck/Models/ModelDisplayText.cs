using System;
using System.Drawing;

namespace PilotsDeck
{
    public class ModelDisplayText : ModelDisplay
    {
		public bool HasIndication { get; set; } = false;
		public bool IndicationHideValue { get; set; } = false;
		public bool IndicationUseColor { get; set; } = false;
		public string IndicationColor { get; set; } = "#ffffff";
		public string IndicationImage { get; set; } = @"Images/ValueFault.png";
		public string IndicationValue { get; set; } = "0";

		public bool FontInherit { get; set; } = true;
		public string FontName { get; set; } = "Arial";
		public int FontSize { get; set; } = 10;
		public int FontStyle { get; set; } = (int)System.Drawing.FontStyle.Regular;
		public string FontColor { get; set; } = "#ffffff";
		public RectangleF FontRect = new RectangleF(11, 23, 48, 40); //-1 -1 -2 0
		public string RectCoord { get; set; } = "11; 23; 48; 40";


		public ModelDisplayText()
		{
			DefaultImage = @"Images/ValueFrame.png";
			ErrorImage = @"Images/ValueError.png";

			//RectCoord = $"{FontRect.X}; {FontRect.Y}; {FontRect.Width}; {FontRect.Height}";
			UpdateRectangle();
		}

        public override void Update()
        {
			UpdateRectangle();

			if (FontInherit)
				RestoreDeckFont();
		}

		public override void SetTitleParameters(StreamDeckTools.StreamDeckTitleParameters titleParameters)
		{
			base.SetTitleParameters(titleParameters);

			if (FontInherit)
				RestoreDeckFont();
		}

		//      public virtual void SetFontParameter(ModelDisplayText model)
		//      {
		//	if (FontInherit)
		//	{
		//		FontName = model.FontName;
		//		FontSize = model.FontSize;
		//		FontStyle = model.FontStyle;
		//		FontColor = model.FontColor;
		//	}

		//	DeckFontName = model.FontName;
		//	DeckFontSize = model.FontSize;
		//	DeckFontStyle = model.FontStyle;
		//	DeckFontColor = model.FontColor;
		//}

		protected virtual void RestoreDeckFont()
        {
			FontName = TitleParameters.FontName;
			FontSize = TitleParameters.FontSize;
			FontStyle = TitleParameters.FontStyle;
			FontColor = TitleParameters.FontColor;
		}

		protected virtual void UpdateRectangle()
        {
			string[] parts = RectCoord.Trim().Split(';');
			if (parts.Length == 4)
			{
				int parses = 0;
				for (int i = 0; i < parts.Length; i++)
					if (int.TryParse(parts[i], out _))
						parses++;

				if (parses == parts.Length)
					FontRect = new RectangleF(Convert.ToSingle(parts[0]), Convert.ToSingle(parts[1]), Convert.ToSingle(parts[2]), Convert.ToSingle(parts[3]));
            }
        }
	}
}
