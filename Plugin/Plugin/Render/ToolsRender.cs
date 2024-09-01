using System;
using System.Drawing;

namespace PilotsDeck.Plugin.Render
{
    public enum ScaleType
    {
        NONE,
        DEFAULT_KEEP,
        DEFAULT_STRETCH,
        DEVICE_KEEP,
        DEVICE_STRETCH
    }

    public enum CenterType
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        BOTH
    }

    public static class ToolsRender
    {
        public static StringFormat DefaultStringFormat { get; } = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.FitBlackBox
        };

        public static StringFormat GetStringFlags(StringAlignment horizontal, StringAlignment vertical)
        {
            return new StringFormat()
            {
                Alignment = horizontal,
                LineAlignment = vertical,
                FormatFlags = StringFormatFlags.FitBlackBox
            };
        }

        public static Rectangle Convert(this RectangleF rect)
        {
            return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }

        public static bool IsSquare(this RectangleF rect)
        {
            return rect.Width == rect.Height;
        }

        public static bool IsSquare(this PointF point)
        {
            return point.X == point.Y;
        }

        public static bool MatchesSize(this RectangleF rect, PointF size)
        {
            return rect.Width == size.X && rect.Height == size.Y;
        }

        public static bool MatchesSize(this PointF point, PointF size)
        {
            return point.X == size.X && point.Y == size.Y;
        }

        public static bool WithinSize(this RectangleF rect, PointF size)
        {
            return rect.Width <= size.X && rect.Height <= size.Y;
        }

        public static bool WithinSize(this PointF point, PointF size)
        {
            return point.X <= size.X && point.Y <= size.Y;
        }

        public static PointF Scale(this PointF point, PointF scale)
        {
            return new(point.X * scale.X, point.Y * scale.Y);
        }

        public static PointF GetScale(PointF canvas, PointF size)
        {
            return new(canvas.X / size.X, canvas.Y / size.Y);
        }

        public static PointF GetScale(PointF canvas, RectangleF size)
        {
            return new(canvas.X / size.Width, canvas.Y / size.Height);
        }

        public static PointF Center(this PointF point)
        {
            return new(point.X / 2.0f, point.Y / 2.0f);
        }

        public static RectangleF Center(this RectangleF rect, PointF center, CenterType centerType)
        {
            if (centerType == CenterType.BOTH)
                return new((center.X - (rect.Width / 2.0f)) + (rect.X / 2.0f), (center.Y - (rect.Height / 2.0f)) + (rect.Y / 2.0f), rect.Width, rect.Height);
            else if (centerType == CenterType.HORIZONTAL)
                return new((center.X - (rect.Width / 2.0f)) + (rect.X / 2.0f), rect.Y, rect.Width, rect.Height);
            else if (centerType == CenterType.VERTICAL)
                return new(rect.X, (center.Y - (rect.Height / 2.0f)) + (rect.Y / 2.0f), rect.Width, rect.Height);
            else
                return rect;
        }

        public static PointF GetPoint(this RectangleF rect)
        {
            return new(rect.X + rect.Width, rect.Y + rect.Height);
        }

        public static PointF GetDestination(this RectangleF rect)
        {
            return new(rect.Width, rect.Height);
        }

        public static PointF GetPoint(this Image image)
        {
            return new(image.Width, image.Height);
        }

        public static RectangleF GetRectangle(this Image image)
        {
            return new(0, 0, image.Width, image.Height);
        }

        public static bool GetColorFromString(ref string text, out Color color)
        {
            color = default;
            try
            {
                if (!string.IsNullOrWhiteSpace(text) && text.StartsWith("[[#"))
                {
                    string strColor = text[0..9].Replace("[[", "");
                    color = ColorTranslator.FromHtml(strColor);
                    text = text[9..];
                    return true;
                }
            }
            catch { }

            return false;
        }

        public static PointF RotatePoint(this PointF pointToRotate, PointF centerPoint, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new PointF
            {
                X =
                    (float)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y =
                    (float)
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        public static float NormalizedDiffRatio(float minuend, float subtrahend, float minimumTotal, float maximumTotal)
        {
            SwapMinMax(ref minimumTotal, ref maximumTotal);

            return Ratio(NormalizedValue(minuend, minimumTotal) - NormalizedValue(subtrahend, minimumTotal), NormalizedValue(maximumTotal, minimumTotal));
        }

        public static float NormalizedValue(float value, float minimum)
        {
            if (minimum < 0.0f)
                value += Math.Abs(minimum);
            else if (minimum > 0.0f)
                value -= minimum;

            return value;
        }

        public static float ClampAngle(float angle)
        {
            return (angle % 360.0f) + (angle < 0.0f ? 360.0f : 0.0f);
        }

        public static float NormalizeAngle(float angle)
        {
            int rotations = (int)Math.Floor(angle / 360.0f);
            if (rotations > 1)
                return angle - (rotations * 360.0f);
            else
                return angle;
        }

        public static float NormalizedRatio(float value, float minimum, float maximum)
        {
            SwapMinMax(ref minimum, ref maximum);

            if (minimum < 0.0f)
            {
                maximum += Math.Abs(minimum);
                value += Math.Abs(minimum);
            }
            else if (minimum > 0.0f)
            {
                maximum -= minimum;
                value -= minimum;
            }

            return Ratio(value, maximum);
        }

        public static float Ratio(float value, float maximum)
        {
            float ratio = value / maximum;
            if (ratio < 0.0f)
                ratio = 0.0f;
            else if (ratio > 1.0f)
                ratio = 1.0f;

            return ratio;
        }

        public static void SwapMinMax(ref float minimum, ref float maximum)
        {
            if (minimum > maximum)
            {
                (maximum, minimum) = (minimum, maximum);
            }
        }
    }
}
