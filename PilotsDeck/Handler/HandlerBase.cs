using System;
using Serilog;

namespace PilotsDeck
{
    public abstract class HandlerBase : IHandler
    {
        public ModelBase CommonSettings { get; protected set; }      

        public virtual string ActionID { get { return Title; } }
        public string Context { get; protected set; }

        public abstract string Address { get; }
        //public virtual bool NeedRegistration { get; } = false;

        public string DrawImage { get; protected set; } = "";
        public bool IsRawImage { get; protected set; } = false;
        public virtual string DefaultImage { get { return CommonSettings.DefaultImage; } }
        public virtual string ErrorImage { get { return CommonSettings.ErrorImage; } }
        public string Title { get; set; } = "";
        public virtual bool UseFont { get { return false; } }

        public bool ForceUpdate { get; set; } = false;
        public bool NeedRedraw { get; set; } = false;
        protected virtual bool CanRedraw { get { return true; } }
        public virtual bool IsInitialized
        {
            get { return CommonSettings.IsInitialized; }
            set { CommonSettings.IsInitialized = value; }
        }

        protected virtual StreamDeckTools.StreamDeckTitleParameters TitleParameters { get; set; }

        public HandlerBase(string context, ModelBase settings)
        {
            Context = context;
            CommonSettings = settings;
            DrawImage = DefaultImage;
        }

        public virtual void SetError()
        {
            if (IsInitialized) 
            {
                if (DrawImage != ErrorImage)
                {
                    DrawImage = ErrorImage;
                    IsRawImage = false;
                    NeedRedraw = true;
                }
            }
            else
            {
                if (DrawImage != DefaultImage)
                {
                    DrawImage = DefaultImage;
                    IsRawImage = false;
                    NeedRedraw = true;
                }
            }
        }

        public virtual void ResetDrawState()
        {
            NeedRedraw = false;
            ForceUpdate = false;
        }

        public virtual void Refresh(ImageManager imgManager, IPCManager ipcManager)
        {
            if (this is IHandlerValue)
                (this as IHandlerValue).RefreshValue(ipcManager);

            if (!CanRedraw)
                SetError();
            else
                Redraw(imgManager);
        }

        protected virtual void Redraw(ImageManager imgManager)
        {
            //if (DrawImage != DefaultImage || ForceUpdate)
            //{
            //    DrawImage = DefaultImage;
            //    IsRawImage = false;
            //    NeedRedraw = true;
            //}

            if (!IsInitialized && DrawImage != DefaultImage)
            {
                DrawImage = DefaultImage;
                IsRawImage = false;
                NeedRedraw = true;
                imgManager.AddImage(DrawImage);
            }
        }

        protected virtual bool CheckInitialization()
        {
            return !string.IsNullOrEmpty(Address);
        }

        public virtual void Update(IPCManager ipcManager)
        {
            if (CheckInitialization())
                IsInitialized = true;
            else
                IsInitialized = false;

            if (this is IHandlerValue)
                (this as IHandlerValue).UpdateAddress(ipcManager);
        }

        public virtual void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters)
        {
            Title = title;
            TitleParameters = titleParameters;
        }

        //public static IPCValue RegisterValue(IPCManager ipcManager, string address)
        //{
        //    return ipcManager.RegisterValue(address, AppSettings.groupStringRead);
        //}

        //public static IPCValue UpdateValue(IPCManager ipcManager, string newAddress, string oldAddress)
        //{
        //    return ipcManager.UpdateValue(newAddress, oldAddress, AppSettings.groupStringRead);
        //}

        //public static void DeregisterValue(IPCManager ipcManager, string context)
        //{
        //    ipcManager.DeregisterValue(context);
        //}
    }
}
