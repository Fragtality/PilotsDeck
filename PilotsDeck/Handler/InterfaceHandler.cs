namespace PilotsDeck
{
    public interface IHandler
    {
        string ActionID { get; }
        string Context { get; }
        StreamDeckType DeckType { get; }

        string Address { get; }

        string RenderImage64 { get; }
        string DefaultImage64 { get; }
        string WaitImage64 { get; }
        string ErrorImage64 { get; }
        string Title { get; set; }
        bool UseFont { get; }

        bool NeedRedraw { get; set; }
        bool NeedRefresh { get; set; }
        bool UpdateSettingsModel { get; set; }
        bool IsInitialized { get; }
        bool FirstLoad { get; set; }

        bool HasAction { get; }
        long TickDown { get; }

        bool OnButtonDown(long tick);
        bool OnButtonUp(long tick);
        bool OnDialRotate(int ticks);
        bool OnTouchTap();

        void Register(ImageManager imgManager, IPCManager ipcManager);
        void Deregister();

        void ResetDrawState();
        void Refresh();
        void RefreshTitle();
        void Update(bool skipActionUpdate = false);
        void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters);
    }
}
