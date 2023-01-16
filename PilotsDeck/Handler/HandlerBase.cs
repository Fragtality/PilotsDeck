using System.Drawing;

namespace PilotsDeck
{
    public abstract class HandlerBase : IHandler
    {
        public ModelBase CommonSettings { get; protected set; }
        public abstract IModelSwitch SwitchSettings { get; }

        public virtual string ActionID { get { return StreamDeckTools.TitleLog(Title); } }
        public string Context { get; protected set; }
        public virtual StreamDeckType DeckType { get; protected set; }
        public virtual bool IsEncoder { get { return DeckType.IsEncoder; } }

        public abstract string Address { get; }
        protected virtual AddressValueManager ValueManager { get; set; } = new AddressValueManager();
        protected virtual IPCManager IPCManager { get; set; }
        protected virtual ImageManager ImgManager { get; set; }

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
        public virtual long TickDown { get; protected set; }

        protected virtual StreamDeckTools.StreamDeckTitleParameters TitleParameters { get; set; }

        public HandlerBase(string context, ModelBase settings, StreamDeckType deckType)
        {
            Context = context;
            CommonSettings = settings;
            DeckType = deckType;
            DrawImage = DefaultImage;

            if (IsEncoder && !CommonSettings.IsEncoder)
            {
                CommonSettings.IsEncoder = true;
                UpdateSettingsModel = true;
            }
        }

        public abstract bool OnButtonUp(long tick);

        public abstract bool OnButtonDown(long tick);

        public abstract bool OnDialRotate(int ticks);

        public abstract bool OnTouchTap();

        public virtual void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            IPCManager = ipcManager;
            ImgManager = imgManager;

            SetInitialization();
            ValueManager.RegisterManager(IPCManager);

            ImgManager.AddImage(DefaultImage, DeckType);
            ImgManager.AddImage(ErrorImage, DeckType);

            if (HasAction)
                RegisterAction();
        }

        public virtual void RegisterAction()
        {
            if (IsActionReadable(SwitchSettings.ActionType) && IPCTools.IsReadAddress(SwitchSettings.AddressAction))
                _ = ValueManager.RegisterValue(ID.SwitchState, SwitchSettings.AddressAction);

            if (SwitchSettings.HasLongPress && IsActionReadable(SwitchSettings.ActionTypeLong) && IPCTools.IsReadAddress(SwitchSettings.AddressActionLong))
                _ = ValueManager.RegisterValue(ID.SwitchStateLong, SwitchSettings.AddressActionLong);

            if (CommonSettings.IsEncoder && IsActionReadable(SwitchSettings.ActionTypeLeft) && IPCTools.IsReadAddress(SwitchSettings.AddressActionLeft))
                _ = ValueManager.RegisterValue(ID.SwitchStateLeft, SwitchSettings.AddressActionLeft);

            if (CommonSettings.IsEncoder && IsActionReadable(SwitchSettings.ActionTypeRight) && IPCTools.IsReadAddress(SwitchSettings.AddressActionRight))
                _ = ValueManager.RegisterValue(ID.SwitchStateRight, SwitchSettings.AddressActionRight);

            if (CommonSettings.IsEncoder && IsActionReadable(SwitchSettings.ActionTypeTouch) && IPCTools.IsReadAddress(SwitchSettings.AddressActionTouch))
                _ = ValueManager.RegisterValue(ID.SwitchStateTouch, SwitchSettings.AddressActionTouch);

            if (((ActionSwitchType)SwitchSettings.ActionType == ActionSwitchType.XPCMD || (ActionSwitchType)SwitchSettings.ActionType == ActionSwitchType.CONTROL) && SwitchSettings.ToggleSwitch && IPCTools.IsReadAddress(SwitchSettings.AddressMonitor))
                _ = ValueManager.RegisterValue(ID.MonitorState, SwitchSettings.AddressMonitor);
        }

        public virtual void Deregister()
        {
            ImgManager.RemoveImage(DefaultImage, DeckType);
            ImgManager.RemoveImage(ErrorImage, DeckType);

            if (HasAction)
                DeregisterAction();
        }

        public virtual void DeregisterAction()
        {
            if (ValueManager.ContainsValue(ID.SwitchState))
                ValueManager.DeregisterValue(ID.SwitchState);

            if (ValueManager.ContainsValue(ID.SwitchStateLong))
                ValueManager.DeregisterValue(ID.SwitchStateLong);

            if (ValueManager.ContainsValue(ID.SwitchStateLeft))
                ValueManager.DeregisterValue(ID.SwitchStateLeft);

            if (ValueManager.ContainsValue(ID.SwitchStateRight))
                ValueManager.DeregisterValue(ID.SwitchStateRight);

            if (ValueManager.ContainsValue(ID.SwitchStateTouch))
                ValueManager.DeregisterValue(ID.SwitchStateTouch);

            if (ValueManager.ContainsValue(ID.MonitorState))
                ValueManager.DeregisterValue(ID.MonitorState);
        }

        public virtual void Update()
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
            if (IPCTools.IsReadAddress(ValueManager.GetAddress(ID.SwitchState)) && !IsActionReadable(SwitchSettings.ActionType))
                ValueManager.DeregisterValue(ID.SwitchState);
            else if (IsActionReadable(SwitchSettings.ActionType) && IPCTools.IsReadAddress(SwitchSettings.AddressAction))
                ValueManager.UpdateValueAddress(ID.SwitchState, SwitchSettings.AddressAction);

            if (IPCTools.IsReadAddress(ValueManager.GetAddress(ID.SwitchStateLong)) && !IsActionReadable(SwitchSettings.ActionTypeLong))
                ValueManager.DeregisterValue(ID.SwitchStateLong);
            else if (SwitchSettings.HasLongPress && IsActionReadable(SwitchSettings.ActionTypeLong) && IPCTools.IsReadAddress(SwitchSettings.AddressActionLong))
                ValueManager.UpdateValueAddress(ID.SwitchStateLong, SwitchSettings.AddressActionLong);

            if (IPCTools.IsReadAddress(ValueManager.GetAddress(ID.SwitchStateLeft)) && !IsActionReadable(SwitchSettings.ActionTypeLeft))
                ValueManager.DeregisterValue(ID.SwitchStateLeft);
            else if (CommonSettings.IsEncoder && IsActionReadable(SwitchSettings.ActionTypeLeft) && IPCTools.IsReadAddress(SwitchSettings.AddressActionLeft))
                ValueManager.UpdateValueAddress(ID.SwitchStateLeft, SwitchSettings.AddressActionLeft);

            if (IPCTools.IsReadAddress(ValueManager.GetAddress(ID.SwitchStateRight)) && !IsActionReadable(SwitchSettings.ActionTypeRight))
                ValueManager.DeregisterValue(ID.SwitchStateRight);
            else if (CommonSettings.IsEncoder && IsActionReadable(SwitchSettings.ActionTypeRight) && IPCTools.IsReadAddress(SwitchSettings.AddressActionRight))
                ValueManager.UpdateValueAddress(ID.SwitchStateRight, SwitchSettings.AddressActionRight);

            if (IPCTools.IsReadAddress(ValueManager.GetAddress(ID.SwitchStateTouch)) && !IsActionReadable(SwitchSettings.ActionTypeTouch))
                ValueManager.DeregisterValue(ID.SwitchStateTouch);
            else if (CommonSettings.IsEncoder && IsActionReadable(SwitchSettings.ActionTypeTouch) && IPCTools.IsReadAddress(SwitchSettings.AddressActionTouch))
                ValueManager.UpdateValueAddress(ID.SwitchStateTouch, SwitchSettings.AddressActionTouch);

            if (IPCTools.IsReadAddress(ValueManager.GetAddress(ID.MonitorState)) && !SwitchSettings.ToggleSwitch)
                ValueManager.DeregisterValue(ID.MonitorState);
            else if (SwitchSettings.ToggleSwitch && ((ActionSwitchType)SwitchSettings.ActionType == ActionSwitchType.XPCMD || (ActionSwitchType)SwitchSettings.ActionType == ActionSwitchType.CONTROL) && IPCTools.IsReadAddress(SwitchSettings.AddressMonitor))
                ValueManager.UpdateValueAddress(ID.MonitorState, SwitchSettings.AddressMonitor);
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
            if (IsInitialized && DrawImage != CommonSettings.WaitImage)
            {
                if (!IsEncoder)
                {
                    DrawImage = CommonSettings.WaitImage;
                    IsRawImage = false;
                    NeedRedraw = true;
                }
                else
                {
                    ImageRenderer render = new(ImgManager.GetImageDefinition(CommonSettings.WaitImage, DeckType));
                    DrawTitle(render);
                    DrawImage = render.RenderImage64();
                    render.Dispose();
                    IsRawImage = true;
                    NeedRedraw = true;
                }
            }
        }

        public virtual void ResetDrawState()
        {
            NeedRedraw = false;
            ForceUpdate = false;
        }

        public virtual void Refresh()
        {
            if (!CanRedraw)
                SetError();
            else
                Redraw();
        }

        public virtual void RefreshTitle()
        {

        }

        protected void DrawTitle(ImageRenderer render, PointF? rect = null)
        {
            var titleParam = TitleParameters ?? new StreamDeckTools.StreamDeckTitleParameters();
            render.DrawTitle(Title, titleParam.GetFont(12), titleParam.GetColor(), rect);
        }

        protected virtual void Redraw()
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

            if (IsEncoder && !CommonSettings.IsEncoder)
            {
                CommonSettings.IsEncoder = true;
                UpdateSettingsModel = true;
            }
        }

        public virtual void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters)
        {
            Title = title;
            TitleParameters = titleParameters;
            ForceUpdate = true;
        }

        public static bool IsActionReadable(int type)
        {
            return type == (int)ActionSwitchType.LVAR || type == (int)ActionSwitchType.OFFSET || type == (int)ActionSwitchType.XPWREF;
        }

        public static bool IsActionReadable(ActionSwitchType type)
        {
            return IsActionReadable((int)type);
        }
    }
}
