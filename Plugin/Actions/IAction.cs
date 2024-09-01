using PilotsDeck.Simulator;
using PilotsDeck.StreamDeck;
using PilotsDeck.StreamDeck.Messages;
using System.Text.Json.Nodes;

namespace PilotsDeck.Actions
{
    public enum StreamDeckCommand
    {
        KEY_DOWN = 0,
        KEY_UP = 1,
        DIAL_DOWN = 2,
        DIAL_UP = 3,
        DIAL_LEFT = 4,
        DIAL_RIGHT = 5,
        TOUCH_TAP = 6
    }

    public enum GaugeOrientation
    {
        UP = -90,
        DOWN = 90,
        LEFT = 180,
        RIGHT = 0
    }

    public enum IndicatorType
    {
        TRIANGLE,
        CIRCLE,
        DOT,
        LINE,
        IMAGE
    }

    public interface IAction
    {
        public string ActionID { get; }
        public string Context { get; set; }
        public string Title { get; set; }
        public bool IsEncoder { get; }
        public StreamDeckCanvasInfo CanvasInfo { get; set; }
        public void SetSettingModel(StreamDeckEvent sdEvent);
        public JsonNode GetSettingModel();
        public bool SettingModelUpdated { get; set; }
        public void SetTitleParameters(string title, StreamDeckEvent.TitleParameters titleParameters);
        public void RegisterRessources();
        public void UpdateRessources();
        public void DeregisterRessources();
        public string RenderImage64 { get; }
        public void ResetDrawState();
        public bool FirstLoad { get; set; }
        public bool NeedRedraw { get; set; }
        public bool NeedRefresh { get; set; }
        public void Refresh();

        public SimCommand[] OnTouchTap(StreamDeckEvent sdEvent);
        public SimCommand[] OnDialDown(StreamDeckEvent sdEvent);
        public SimCommand[] OnDialUp(StreamDeckEvent sdEvent);
        public SimCommand[] OnDialRotate(StreamDeckEvent sdEvent);
        public SimCommand[] OnKeyDown(StreamDeckEvent sdEvent);
        public SimCommand[] OnKeyUp(StreamDeckEvent sdEvent);
    }
}
