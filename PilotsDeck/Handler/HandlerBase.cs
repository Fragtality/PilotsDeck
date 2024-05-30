using System.Collections.Generic;
using System.Drawing;

namespace PilotsDeck
{
    public abstract class HandlerBase : IHandler
    {
        public ModelBase CommonSettings { get; protected set; }
        public abstract IModelSwitch SwitchSettings { get; }

        public virtual string ActionID { get { return $"({Title.Trim()})"; } }
        public string Context { get; protected set; }
        public virtual StreamDeckType DeckType { get; protected set; }
        public virtual bool IsEncoder { get { return DeckType.IsEncoder; } }

        public abstract string Address { get; }
        protected virtual ValueManager ValueManager { get; set; }
        protected virtual IPCManager IPCManager { get; set; }
        protected virtual ImageManager ImgManager { get; set; }
        protected virtual Dictionary<int, string> imgRefs { get; set; } = [];

        public virtual string RenderImage64 { get; protected set; } = "";
        public virtual string DefaultImage64 { get; protected set; } = "";
        public virtual string WaitImage64 { get; protected set; } = "";
        public virtual string ErrorImage64 { get; protected set; } = "";

        public string Title { get; set; } = "";
        public virtual bool UseFont { get { return false; } }

        public bool NeedRedraw { get; set; } = false;
        public bool NeedRefresh { get; set; } = false;
        public virtual bool UpdateSettingsModel { get; set; } = false;
        protected virtual bool CanRedraw { get { return !string.IsNullOrEmpty(Address); } }
        public virtual bool IsInitialized { get; set; }
        public virtual bool FirstLoad { get; set; } = true;

        public virtual bool HasAction { get; protected set; } = false;
        public virtual long TickDown { get; protected set; }

        protected virtual StreamDeckTools.StreamDeckTitleParameters TitleParameters { get; set; }

        public abstract bool OnButtonUp(long tick);
        public abstract bool OnButtonDown(long tick);
        public abstract bool OnDialRotate(int ticks);
        public abstract bool OnTouchTap();

        #region Initialization
        public HandlerBase(string context, ModelBase settings, StreamDeckType deckType)
        {
            Context = context;
            CommonSettings = settings;
            DeckType = deckType;

            if (IsEncoder && !CommonSettings.IsEncoder)
            {
                CommonSettings.IsEncoder = true;
                UpdateSettingsModel = true;
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
        #endregion

        #region RegisterDeregister
        public virtual void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            IPCManager = ipcManager;
            ImgManager = imgManager;
            ValueManager = new(ipcManager);

            SetInitialization();

            DefaultImage64 = ImgManager.AddImage(CommonSettings.DefaultImage, DeckType).GetImageBase64();
            imgRefs.Add(ID.Default, CommonSettings.DefaultImage);

            ErrorImage64 = ImgManager.AddImage(CommonSettings.ErrorImage, DeckType).GetImageBase64();
            imgRefs.Add(ID.Error, CommonSettings.ErrorImage);

            WaitImage64 = ImgManager.AddImage(CommonSettings.WaitImage, DeckType).GetImageBase64();
            imgRefs.Add(ID.Wait, CommonSettings.WaitImage);

            if (HasAction)
            {
                if (SwitchSettings.IsGuarded)
                {
                    if (!SwitchSettings.UseImageGuardMapping)
                    {
                        imgManager.AddImage(SwitchSettings.ImageGuard, DeckType);
                        imgRefs.Add(ID.ImgGuard, SwitchSettings.ImageGuard);
                    }
                    else
                    {
                        ModelSwitchDisplay.ManageImageMap(SwitchSettings.ImageGuardMap, imgManager, DeckType, true);
                        imgRefs.Add(ID.MapGuard, SwitchSettings.ImageGuardMap);
                    }

                    ValueManager.AddValue(ID.Guard, SwitchSettings.AddressGuardActive);

                    RenderDefaultImages();
                }
                RegisterActions();
            }

            NeedRefresh = true;
            NeedRedraw = true;
        }

        public virtual void RegisterActions()
        {
            ValueManager.AddValue(ID.Switch, SwitchSettings.AddressAction, SwitchSettings.ActionType);
            if (IPCTools.IsToggleableCommand(SwitchSettings.ActionType) && SwitchSettings.ToggleSwitch)
                ValueManager.AddValue(ID.Monitor, SwitchSettings.AddressMonitor);

            if (SwitchSettings.HasLongPress)
                ValueManager.AddValue(ID.SwitchLong, SwitchSettings.AddressActionLong, SwitchSettings.ActionTypeLong);

            if (CommonSettings.IsEncoder)
            {
                ValueManager.AddValue(ID.SwitchLeft, SwitchSettings.AddressActionLeft, SwitchSettings.ActionTypeLeft);
                ValueManager.AddValue(ID.SwitchRight, SwitchSettings.AddressActionRight, SwitchSettings.ActionTypeRight);
                ValueManager.AddValue(ID.SwitchTouch, SwitchSettings.AddressActionTouch, SwitchSettings.ActionTypeTouch);
            }

            if (SwitchSettings.IsGuarded)
                ValueManager.AddValue(ID.GuardCmd, SwitchSettings.AddressActionGuard, SwitchSettings.ActionTypeGuard);
        }

        public virtual void Deregister()
        {
            ImgManager.RemoveImage(CommonSettings.DefaultImage, DeckType);
            imgRefs.Remove(ID.Default);
            ImgManager.RemoveImage(CommonSettings.ErrorImage, DeckType);
            imgRefs.Remove(ID.Error);
            ImgManager.RemoveImage(CommonSettings.WaitImage, DeckType);
            imgRefs.Remove(ID.Wait);

            if (HasAction)
            {
                if (SwitchSettings.IsGuarded)
                {
                    ImgManager.RemoveImage(SwitchSettings.ImageGuard, DeckType);
                    imgRefs.Remove(ID.ImgGuard);

                    ModelSwitchDisplay.ManageImageMap(SwitchSettings.ImageGuardMap, ImgManager, DeckType, false);
                    imgRefs.Remove(ID.MapGuard);

                    ValueManager.RemoveValue(ID.Guard);
                }
                DeregisterActions();
            }
        }

        public virtual void DeregisterActions()
        {
            ValueManager.RemoveValue(ID.Switch);
            if (IPCTools.IsToggleableCommand(SwitchSettings.ActionType) && SwitchSettings.ToggleSwitch)
                ValueManager.RemoveValue(ID.Monitor);

            if (SwitchSettings.HasLongPress)
                ValueManager.RemoveValue(ID.SwitchLong);

            if (CommonSettings.IsEncoder)
            {
                ValueManager.RemoveValue(ID.SwitchLeft);
                ValueManager.RemoveValue(ID.SwitchRight);
                ValueManager.RemoveValue(ID.SwitchTouch);
            }

            if (SwitchSettings.IsGuarded)
                ValueManager.RemoveValue(ID.GuardCmd);
        }
        #endregion

        #region Update
        public virtual void Update(bool skipActionUpdate = false)
        {
            SetInitialization();

            UpdateImages();

            if (HasAction && !skipActionUpdate)
            {
                if (SwitchSettings.IsGuarded && !ValueManager.Contains(ID.Guard))
                    ValueManager.AddValue(ID.Guard, SwitchSettings.AddressGuardActive);
                else if (!SwitchSettings.IsGuarded && ValueManager.Contains(ID.Guard))
                    ValueManager.RemoveValue(ID.Guard);
                else if (SwitchSettings.IsGuarded)
                    ValueManager.UpdateValue(ID.Guard, SwitchSettings.AddressGuardActive);
                UpdateActionSettings();
                UpdateActions();
            }

            NeedRefresh = true;
            NeedRedraw = true;
        }

        protected void UpdateImage(string imgNew, int id, out ManagedImage image)
        {
            if (imgRefs.TryGetValue(id, out string strRef) && imgNew != strRef)
            {
                ImgManager.RemoveImage(strRef, DeckType);
                image = ImgManager.AddImage(imgNew, DeckType);
            }
            else
                image = null;

            if (imgRefs.ContainsKey(id))
                imgRefs[id] = imgNew;
        }

        protected virtual void UpdateImages()
        {
            UpdateImage(CommonSettings.DefaultImage, ID.Default, out ManagedImage image);
            if (image != null)
                DefaultImage64 = image.GetImageBase64();

            UpdateImage(CommonSettings.ErrorImage, ID.Error, out image);
            if (image != null)
                ErrorImage64 = image.GetImageBase64();

            UpdateImage(CommonSettings.WaitImage, ID.Wait, out image);
            if (image != null)
                WaitImage64 = image.GetImageBase64();

            if (HasAction && SwitchSettings.IsGuarded)
            {
                UpdateImage(SwitchSettings.ImageGuard, ID.ImgGuard, out image);
                if (image != null)
                    NeedRefresh = true;

                if (imgRefs.TryGetValue(ID.ImgGuard, out string oldMap))
                {
                    ModelSwitchDisplay.ManageImageMap(oldMap, ImgManager, DeckType, false);
                    if (!string.IsNullOrWhiteSpace(SwitchSettings.ImageGuardMap) && SwitchSettings.ImageGuardMap != oldMap && SwitchSettings.UseImageGuardMapping)
                    {
                        imgRefs[ID.MapGuard] = SwitchSettings.ImageGuardMap;
                        ModelSwitchDisplay.ManageImageMap(SwitchSettings.ImageGuardMap, ImgManager, DeckType, true);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(SwitchSettings.ImageGuardMap) && SwitchSettings.UseImageGuardMapping)
                {
                    imgRefs[ID.MapGuard] = SwitchSettings.ImageGuardMap;
                    ModelSwitchDisplay.ManageImageMap(SwitchSettings.ImageGuardMap, ImgManager, DeckType, true);
                }
            }
        }

        public virtual void UpdateActionSettings()
        {

        }

        public virtual void UpdateActions()
        {
            if (HasAction && !ValueManager.Contains(ID.Switch))
                RegisterActions();
            else if (!HasAction && ValueManager.Contains(ID.Switch))
                DeregisterActions();
            else
            {
                ValueManager.UpdateValue(ID.Switch, SwitchSettings.AddressAction, SwitchSettings.ActionType);

                if (SwitchSettings.ToggleSwitch && !ValueManager.Contains(ID.Monitor))
                    ValueManager.AddValue(ID.Monitor, SwitchSettings.AddressMonitor);
                else if (!SwitchSettings.ToggleSwitch && ValueManager.Contains(ID.Monitor))
                    ValueManager.RemoveValue(ID.Monitor);
                else
                    ValueManager.UpdateValue(ID.Monitor, SwitchSettings.AddressMonitor);

                if (SwitchSettings.HasLongPress && !ValueManager.Contains(ID.SwitchLong))
                    ValueManager.AddValue(ID.SwitchLong, SwitchSettings.AddressActionLong, SwitchSettings.ActionTypeLong);
                else if (!SwitchSettings.HasLongPress && ValueManager.Contains(ID.SwitchLong))
                    ValueManager.RemoveValue(ID.SwitchLong);
                else if (SwitchSettings.HasLongPress)
                    ValueManager.UpdateValue(ID.SwitchLong, SwitchSettings.AddressActionLong, SwitchSettings.ActionTypeLong);

                if (CommonSettings.IsEncoder)
                {
                    ValueManager.UpdateValue(ID.SwitchLeft, SwitchSettings.AddressActionLeft, SwitchSettings.ActionTypeLeft);
                    ValueManager.UpdateValue(ID.SwitchRight, SwitchSettings.AddressActionRight, SwitchSettings.ActionTypeRight);
                    ValueManager.UpdateValue(ID.SwitchTouch, SwitchSettings.AddressActionTouch, SwitchSettings.ActionTypeTouch);
                }

                if (SwitchSettings.IsGuarded)
                    ValueManager.UpdateValue(ID.GuardCmd, SwitchSettings.AddressActionGuard, SwitchSettings.ActionTypeGuard);
            }
        }
        #endregion

        #region DrawRefresh
        protected virtual void RenderDefaultImages()
        {

        }

        public virtual void ResetDrawState()
        {
            NeedRedraw = false;
            NeedRefresh = false;
        }

        public virtual void RefreshTitle()
        {

        }

        protected void DrawTitle(ImageRenderer render, PointF? rect = null)
        {
            var titleParam = TitleParameters ?? new StreamDeckTools.StreamDeckTitleParameters();
            if (titleParam.ShowTitle)
                render.DrawTitle(Title, titleParam.GetFont(12), titleParam.GetColor(), rect);
        }

        public virtual void Refresh()
        {
            if (NeedRefresh)
            {
                RenderImage64 = DefaultImage64;
                NeedRedraw = true;
            }
        }

        public virtual void SetTitleParameters(string title, StreamDeckTools.StreamDeckTitleParameters titleParameters)
        {
            Title = title;
            TitleParameters = titleParameters;
            RefreshTitle();
            NeedRefresh = true;
        }
        #endregion
    }
}
