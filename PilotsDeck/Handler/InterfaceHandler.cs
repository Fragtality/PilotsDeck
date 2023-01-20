namespace PilotsDeck
{
    public interface IHandler
    {
        string ActionID { get; }
        string Context { get; }
        StreamDeckType DeckType { get; }

        string Address { get; }

        string DrawImage { get; }
        bool IsRawImage { get; }
        string DefaultImage { get; }
        string ErrorImage { get; }
        string Title { get; set; }
        bool UseFont { get; }

        bool ForceUpdate { get; set; }
        bool NeedRedraw { get; set; }
        bool UpdateSettingsModel { get; set; }
        bool IsInitialized { get; }

        bool HasAction { get; }
        long TickDown { get; }

        bool OnButtonDown(long tick);
        bool OnButtonUp(long tick);
        bool OnDialRotate(int ticks);
        bool OnTouchTap();

        void Register(ImageManager imgManager, IPCManager ipcManager);
        void Deregister();

        void SetError();
        void SetDefault();
        void SetWait();
        void ResetDrawState();
        void Refresh();
        void RefreshTitle();
        void Update(bool skipActionUpdate = false);
        void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters);
    }
}
