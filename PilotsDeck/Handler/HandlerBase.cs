using System;
using Serilog;

namespace PilotsDeck
{
    public abstract class HandlerBase : IHandler
    {
        
        public virtual string ActionID { get { return Title; } }
        public string Context { get; protected set; }

        public string DrawImage { get; protected set; } = "";
        public bool IsRawImage { get; protected set; } = false;
        public abstract string DefaultImage { get; }
        public abstract string ErrorImage { get; }
        public string Title { get; set; } = "";

        public bool ForceUpdate { get; set; } = false;
        public bool NeedRedraw { get; set; } = false;
        public abstract bool IsInitialized { get; }


        public HandlerBase(string context, ModelBase settings)
        {
            Context = context;
            DrawImage = settings.DefaultImage;
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

        public virtual void Refresh(ImageManager imgManager)
        {
            if (DrawImage != DefaultImage || ForceUpdate)
            {
                DrawImage = DefaultImage;
                IsRawImage = false;
                NeedRedraw = true;
            }
        }

        public abstract void Update();
    }
}
