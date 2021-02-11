﻿using System;

namespace PilotsDeck
{
    public interface IHandler
    {
        string ActionID { get; }
        string Context { get; }
        
        string Address { get; }
        //bool NeedRegistration { get; }

        string DrawImage { get; }
        bool IsRawImage { get; }
        string DefaultImage { get; }
        string ErrorImage { get; }
        string Title { get; set; }
        bool UseFont { get; }

        bool ForceUpdate { get; set; }
        bool NeedRedraw { get; set; }
        bool IsInitialized { get; }

        //void Register(ImageManager imgManager, IPCManager ipcManager);
        //void Deregister(ImageManager imgManager, IPCManager ipcManager);

        void SetError();
        void ResetDrawState();
        void Refresh(ImageManager imgManager, IPCManager ipcManager);
        void Update(IPCManager ipcManager);
        void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters);
    }

    public interface IHandlerValue : IHandler //NEEDED WITH register/unregister?!
    {
        string CurrentValue { get; }
        string LastAddress { get; }
        bool IsChanged { get; }
        
        void RefreshValue(IPCManager ipcManager);

        void RegisterAddress(IPCManager ipcManager);
        void UpdateAddress(IPCManager ipcManager);
        void DeregisterAddress(IPCManager ipcManager);
    }

    //public interface IHandlerDisplay : IHandler //NEEDED ?!
    //{
    //    void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters);
    //}

    public interface IHandlerSwitch : IHandler
    {
        bool Action(IPCManager ipcManager);
    }
}
