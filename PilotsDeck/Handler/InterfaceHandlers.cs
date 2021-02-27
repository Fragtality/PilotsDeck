namespace PilotsDeck
{
    public interface IHandler
    {
        string ActionID { get; }
        string Context { get; }
        
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

        void Register(ImageManager imgManager, IPCManager ipcManager);
        void Deregister(ImageManager imgManager, IPCManager ipcManager);

        void SetError();
        void SetDefault();
        void SetWait();
        void ResetDrawState();
        void Refresh(ImageManager imgManager, IPCManager ipcManager);
        void Update(ImageManager imgManager, IPCManager ipcManager);
        void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters);
    }

    public interface IHandlerValue : IHandler
    {
        string CurrentValue { get; }
        string LastAddress { get; }
        bool IsChanged { get; }
        
        void RefreshValue(IPCManager ipcManager);

        void RegisterAddress(IPCManager ipcManager);
        void UpdateAddress(IPCManager ipcManager);
        void DeregisterAddress(IPCManager ipcManager);
    }

    public interface IHandlerSwitch : IHandler
    {
        //bool Action(IPCManager ipcManager, bool longPress);
        long tickDown { get; }

        bool OnButtonDown(IPCManager ipcManager, long tick);
        bool OnButtonUp(IPCManager ipcManager, long tick);
    }
}
