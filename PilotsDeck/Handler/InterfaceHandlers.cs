using System;

namespace PilotsDeck
{
    public interface IHandler
    {
        string ActionID { get; }
        string Context { get; }

        string DrawImage { get; }
        bool IsRawImage { get; }
        string DefaultImage { get; }
        string ErrorImage { get; }
        string Title { get; set; }

        bool ForceUpdate { get; set; }
        bool NeedRedraw { get; set; }
        bool IsInitialized { get; }

        void SetError();
        void ResetDrawState();
        void Refresh(ImageManager imgManager);
        void Update();
    }

    public interface IHandlerValue : IHandler
    {
        void RegisterValue(IPCManager ipcManager);
        void UpdateValue(IPCManager ipcManager);
        void DeregisterValue(IPCManager ipcManager);
    }

    public interface IHandlerDisplay : IHandler
    {
        void SetTitleParameters(StreamDeckTools.StreamDeckTitleParameters titleParameters);
    }

    public interface IHandlerSwitch : IHandler
    {
        bool Action(IPCManager ipcManager);
    }
}
