namespace PilotsDeck
{
    public abstract class HandlerBase : IHandler
    {
        public ModelBase CommonSettings { get; protected set; }
        public abstract IModelSwitch SwitchSettings { get; }

        public virtual string ActionID { get { return Title; } }
        public string Context { get; protected set; }
        public virtual StreamDeckType DeckType { get; protected set; }

        public abstract string Address { get; }
        protected virtual AddressValueManager ValueManager { get; set; } = new AddressValueManager();

        public string DrawImage { get; protected set; } = "";
        public bool IsRawImage { get; protected set; } = false;
        public virtual string DefaultImage { get { return CommonSettings.DefaultImage; } }
        public virtual string ErrorImage { get { return CommonSettings.ErrorImage; } }
        public string Title { get; set; } = "";
        public virtual bool UseFont { get { return false; } }

        public bool ForceUpdate { get; set; } = false;
        public bool NeedRedraw { get; set; } = false;
        public virtual bool UpdateSettingsModel { get; set; } = false;
        protected virtual bool CanRedraw { get { return !string.IsNullOrEmpty(Address); } }
        public virtual bool IsInitialized { get; set; }

        public virtual bool HasAction { get; protected set; } = false;
        public virtual long tickDown { get; protected set; }

        protected virtual StreamDeckTools.StreamDeckTitleParameters TitleParameters { get; set; }

        public HandlerBase(string context, ModelBase settings, StreamDeckType deckType)
        {
            Context = context;
            CommonSettings = settings;
            DeckType = deckType;
            DrawImage = DefaultImage;
        }

        public abstract bool OnButtonUp(IPCManager ipcManager, long tick);

        public abstract bool OnButtonDown(IPCManager ipcManager, long tick);

        public virtual void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            SetInitialization();
            ValueManager.RegisterManager(ipcManager);

            imgManager.AddImage(DefaultImage, DeckType);
            imgManager.AddImage(ErrorImage, DeckType);

            if (HasAction)
                RegisterAction();
        }

        public virtual void RegisterAction()
        {
            if (IsActionReadable(SwitchSettings.ActionType) && IPCTools.IsReadAddress(SwitchSettings.AddressAction))
                ValueManager.RegisterValue(ID.SwitchState, SwitchSettings.AddressAction);

            if (SwitchSettings.HasLongPress && IsActionReadable(SwitchSettings.ActionTypeLong) && IPCTools.IsReadAddress(SwitchSettings.AddressActionLong))
                ValueManager.RegisterValue(ID.SwitchStateLong, SwitchSettings.AddressActionLong);
        }

        public virtual void Deregister(ImageManager imgManager)
        {
            imgManager.RemoveImage(DefaultImage, DeckType);
            imgManager.RemoveImage(ErrorImage, DeckType);

            if (HasAction)
                DeregisterAction();
        }

        public virtual void DeregisterAction()
        {
            if (ValueManager.ContainsValue(ID.SwitchState))
                ValueManager.DeregisterValue(ID.SwitchState);

            if (ValueManager.ContainsValue(ID.SwitchStateLong))
                ValueManager.DeregisterValue(ID.SwitchStateLong);
        }

        public virtual void Update(ImageManager imgManager)
        {
            SetInitialization();

            if (HasAction)
            {
                UpdateActionSettings();
                UpdateActionValues();
            }

            ForceUpdate = true;
        }

        public virtual void UpdateActionSettings()
        {

        }

        public virtual void UpdateActionValues()
        {
            if (IsActionReadable(SwitchSettings.ActionType) && IPCTools.IsReadAddress(SwitchSettings.AddressAction))
                ValueManager.UpdateValueAddress(ID.SwitchState, SwitchSettings.AddressAction);

            if (SwitchSettings.HasLongPress && IsActionReadable(SwitchSettings.ActionTypeLong) && IPCTools.IsReadAddress(SwitchSettings.AddressActionLong))
                ValueManager.UpdateValueAddress(ID.SwitchStateLong, SwitchSettings.AddressActionLong);
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

        public virtual void Refresh(ImageManager imgManager)
        {
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

        public virtual void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters)
        {
            Title = title;
            TitleParameters = titleParameters;
            ForceUpdate = true;
        }

        public static bool IsActionReadable(int type)
        {
            return type == (int)ActionSwitchType.LVAR || type == (int)ActionSwitchType.OFFSET;
        }
    }
}
