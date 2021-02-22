namespace PilotsDeck
{
    public abstract class HandlerBase : IHandler
    {
        public ModelBase CommonSettings { get; protected set; }      

        public virtual string ActionID { get { return Title; } }
        public string Context { get; protected set; }

        public abstract string Address { get; }

        public string DrawImage { get; protected set; } = "";
        public bool IsRawImage { get; protected set; } = false;
        public virtual string DefaultImage { get { return CommonSettings.DefaultImage; } }
        public virtual string ErrorImage { get { return CommonSettings.ErrorImage; } }
        public string Title { get; set; } = "";
        public virtual bool UseFont { get { return false; } }

        public bool ForceUpdate { get; set; } = false;
        public bool NeedRedraw { get; set; } = false;
        public virtual bool UpdateSettingsModel { get; set; } = false;
        protected virtual bool CanRedraw { get { return true; } }
        public virtual bool IsInitialized { get; set; }
        

        protected virtual StreamDeckTools.StreamDeckTitleParameters TitleParameters { get; set; }

        public HandlerBase(string context, ModelBase settings)
        {
            Context = context;
            CommonSettings = settings;
            DrawImage = DefaultImage;
        }

        public virtual void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            SetInitialization();
            
            imgManager.AddImage(DefaultImage);
            imgManager.AddImage(ErrorImage);
            
            if (this is IHandlerValue)
                (this as IHandlerValue).RegisterAddress(ipcManager);
        }

        public virtual void Deregister(ImageManager imgManager, IPCManager ipcManager)
        {
            imgManager.RemoveImage(DefaultImage);
            imgManager.RemoveImage(ErrorImage);

            if (this is IHandlerValue)
                (this as IHandlerValue).DeregisterAddress(ipcManager);
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
                SetDefault();
            }
        }

        public virtual void SetDefault()
        {
            if (DrawImage != DefaultImage)
            {
                DrawImage = DefaultImage;
                IsRawImage = false;
                NeedRedraw = true;
            }
        }

        public virtual void SetWait()
        {
            if (IsInitialized && DrawImage != AppSettings.waitImage)
            {
                DrawImage = AppSettings.waitImage;
                IsRawImage = false;
                NeedRedraw = true;
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
            if (!IsInitialized && DrawImage != DefaultImage || ForceUpdate)
            {
                DrawImage = DefaultImage;
                IsRawImage = false;
                NeedRedraw = true;
            }
        }

        protected virtual bool InitializationTest()
        {
            return !string.IsNullOrEmpty(Address);
        }

        protected virtual void SetInitialization()
        {
            if (InitializationTest())
                IsInitialized = true;
            else
                IsInitialized = false;
        }

        public virtual void Update(ImageManager imgManager, IPCManager ipcManager)
        {
            SetInitialization();

            if (this is IHandlerValue)
                (this as IHandlerValue).UpdateAddress(ipcManager);

            ForceUpdate = true;
        }

        public virtual void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters)
        {
            Title = title;
            TitleParameters = titleParameters;
            ForceUpdate = true;
        }
    }
}
