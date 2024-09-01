using PilotsDeck.Resources.Images;
using PilotsDeck.StreamDeck.Messages;
using System.Drawing;

namespace PilotsDeck.StreamDeck
{
    public class StreamDeckCanvasInfo
    {
        public static DeckController DeckController { get { return App.DeckController; } }
        public StreamDeckType Type { get; set; }
        public bool IsEncoder { get; set; }

        public PointF GetCanvasSize()
        {
            if (IsEncoder)
                return new PointF(200, 100);
            else
            {
                if (Type == StreamDeckType.StreamDeckXL)
                    return new PointF(144, 144);
                else if (Type == StreamDeckType.StreamDeckPlus)
                    return new PointF(144, 144);
                else
                    return new PointF(72, 72);
            }
        }

        public ImageVariant GetImageVariant()
        {
            if (IsEncoder)
                return ImageVariant.PLUS;
            else
            {
                if (Type == StreamDeckType.StreamDeckXL)
                    return ImageVariant.DEFAULT_HQ;
                else if (Type == StreamDeckType.StreamDeckPlus)
                    return ImageVariant.DEFAULT_HQ;
                else
                    return ImageVariant.DEFAULT;
            }
        }

        public static StreamDeckCanvasInfo GetInfo(StreamDeckEvent sdEvent)
        {
            StreamDeckCanvasInfo info = new()
            {
                IsEncoder = sdEvent?.payload?.controller == AppConfiguration.SdEncoder,
                Type = DeckController.GetDeckType(sdEvent?.device),
            };

            return info;
        }
    }
}
