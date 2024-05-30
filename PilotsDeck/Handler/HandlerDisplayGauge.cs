using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayGauge(string context, ModelDisplayGauge settings, StreamDeckType deckType) : HandlerDisplayText(context, settings, deckType)
    {
        public virtual ModelDisplayGauge GaugeSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplayGauge Settings { get; protected set; } = settings;

        public override string Address { get { return GaugeSettings.Address; } }
        public override string ActionID { get { return $"(HandlerDisplayGauge) ({Title.Trim()}) {(GaugeSettings.IsEncoder ? "(Encoder) " : "")}(Read: {GaugeSettings.Address}) (HasAction: {HasAction}) (Action: {(ActionSwitchType)SwitchSettings.ActionType} / {Address}) (Long: {SwitchSettings.HasLongPress} / {(ActionSwitchType)SwitchSettings.ActionTypeLong} / {SwitchSettings.AddressActionLong})"; } }
        public override bool UseFont { get { return true; } }

        protected bool IsArc = settings.DrawArc;

        public override bool OnButtonDown(long tick)
        {
            if (GaugeSettings.HasAction)
            {
                TickDown = tick;
                return HandlerSwitch.RunButtonDown(IPCManager, SwitchSettings);
            }
            else
                return false;
        }

        public override bool OnButtonUp(long tick)
        {
            if (GaugeSettings.HasAction)
            {
                bool result = HandlerSwitch.RunButtonUp(IPCManager, tick - TickDown, ValueManager, SwitchSettings);
                TickDown = 0;

                return result;
            }
            else
                return false;
        }

        public override bool OnDialRotate(int ticks)
        {
            if (GaugeSettings.HasAction)
            {
                bool result = HandlerSwitch.RunDialRotate(IPCManager, ticks, ValueManager, SwitchSettings);
                TickDown = 0;

                return result;
            }
            else
                return false;
        }

        public override bool OnTouchTap()
        {
            if (GaugeSettings.HasAction)
            {
                bool result = HandlerSwitch.RunTouchTap(IPCManager, ValueManager, SwitchSettings);
                TickDown = 0;

                return result;
            }
            else
                return false;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            HasAction = GaugeSettings.HasAction;
            base.Register(imgManager, ipcManager);
            
            if (GaugeSettings.UseColorSwitching)
                ValueManager.AddValue(ID.GaugeColor, GaugeSettings.AddressColorOff);
            
            RenderDefaultImages();
        }

        public override void Deregister()
        {
            if (GaugeSettings.UseColorSwitching)
                ValueManager.RemoveValue(ID.GaugeColor);

            base.Deregister();
        }

        public override void Update(bool skipActionUpdate = false)
        {
            HasAction = GaugeSettings.HasAction;
            base.Update(skipActionUpdate);

            if (GaugeSettings.UseColorSwitching && !ValueManager.Contains(ID.GaugeColor))
                ValueManager.AddValue(ID.GaugeColor, GaugeSettings.AddressColorOff);
            else if (!GaugeSettings.UseColorSwitching && ValueManager.Contains(ID.GaugeColor))
                ValueManager.RemoveValue(ID.GaugeColor);
            else if (GaugeSettings.UseColorSwitching)
                ValueManager.UpdateValue(ID.GaugeColor, GaugeSettings.AddressColorOff);

            if (IsArc != GaugeSettings.DrawArc)
            {
                GaugeSettings.ResetCoords();
                IsArc = GaugeSettings.DrawArc;
                UpdateSettingsModel = true;
                NeedRefresh = true;
            }

            RenderDefaultImages();
        }

        protected override void RenderDefaultImages()
        {
            //Default
            ImageRenderer render = new (ImgManager.GetImage(GaugeSettings.DefaultImage, DeckType), DeckType);

            if(GaugeSettings.DrawArc)
            {
                DrawText("0", render, true);
                DrawArc("0", render, true);
            }
            else
            {
                DrawBar("0", render, true);
                DrawText("0", render, true);
            }

            if (IsEncoder)
                DrawTitle(render);

            DefaultImage64 = render.RenderImage64();
            render.Dispose();

            //Error
            render = new(ImgManager.GetImage(GaugeSettings.ErrorImage, DeckType), DeckType);
            if (IsEncoder)
                DrawTitle(render);
            ErrorImage64 = render.RenderImage64();
            render.Dispose();

            //Wait
            render = new(ImgManager.GetImage(GaugeSettings.WaitImage, DeckType), DeckType);
            if (IsEncoder)
                DrawTitle(render);
            WaitImage64 = render.RenderImage64();
            render.Dispose();
        }

        public override void Refresh()
        {
            if (!ValueManager.IsChanged(ID.Gauge) && !ValueManager.IsChanged(ID.GaugeColor) && !NeedRefresh)
                return;

            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            string value = ValueManager[ID.Gauge];
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            ImageRenderer render = new (ImgManager.GetImage(GaugeSettings.DefaultImage, DeckType), DeckType);

            if (GaugeSettings.DrawArc)
            {
                DrawText(value, render);
                DrawArc(value, render);
            }
            else
            {
                DrawBar(value, render);
                DrawText(value, render);
            }

            if (IsEncoder)
                DrawTitle(render);

            RenderImage64 = render.RenderImage64();
            render.Dispose();
            NeedRedraw = true;
            
            //sw.Stop();
            //Log.Logger.Debug($"Time for Gauge-Frame: {sw.Elapsed.TotalMilliseconds}ms [{ActionID}]");
        }

        protected virtual void DrawBar(string value, ImageRenderer render, bool defaultImage = false)
        {
            Bar drawBar = GaugeSettings.GetBar();
            float min = ModelDisplayText.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayText.GetNumValue(GaugeSettings.MaximumValue, 100);

            bool useOffColor = GaugeSettings.UseColorSwitching && (!string.IsNullOrEmpty(ValueManager[ID.GaugeColor]) && ModelBase.Compare(GaugeSettings.StateColorOff, ValueManager[ID.GaugeColor]));
            Color drawColor;
            if (useOffColor)
                drawColor = ColorTranslator.FromHtml(GaugeSettings.GaugeColorOff);
            else
                drawColor = ColorTranslator.FromHtml(GaugeSettings.GaugeColor);

            render.Rotate(GaugeSettings.BarOrientation, new PointF(0, 0));
            render.DrawBar(drawColor, drawBar);

            if (GaugeSettings.DrawWarnRange && !useOffColor)
                render.DrawBarRanges(drawBar, GaugeSettings.GetColorRange(), GaugeSettings.GetWarnRange(), min, max, GaugeSettings.SymmRange);

            if (GaugeSettings.CenterLine)
                render.DrawBarCenterLine(drawBar, ColorTranslator.FromHtml(GaugeSettings.CenterLineColor), ModelDisplayText.GetNumValue(GaugeSettings.CenterLineThickness, 2));

            render.DrawBarIndicator(drawBar, ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), ModelDisplayText.GetNumValue(GaugeSettings.IndicatorSize, 10), ModelDisplayText.GetNumValue(value, 0), min, max, GaugeSettings.IndicatorFlip);
        }

        protected virtual void DrawArc(string value, ImageRenderer render, bool defaultImage = false)
        {
            Arc drawArc = GaugeSettings.GetArc();
            float min = ModelDisplayText.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayText.GetNumValue(GaugeSettings.MaximumValue, 100);

            bool useOffColor = GaugeSettings.UseColorSwitching && (!string.IsNullOrEmpty(ValueManager[ID.GaugeColor]) && ModelBase.Compare(GaugeSettings.StateColorOff, ValueManager[ID.GaugeColor]));
            Color drawColor;
            if (useOffColor)
                drawColor = ColorTranslator.FromHtml(GaugeSettings.GaugeColorOff);
            else
                drawColor = ColorTranslator.FromHtml(GaugeSettings.GaugeColor);

            render.DrawArc(drawArc, drawColor);

            if (GaugeSettings.DrawWarnRange && !useOffColor)
                render.DrawArcRanges(drawArc, GaugeSettings.GetColorRange(), GaugeSettings.GetWarnRange(), min, max, GaugeSettings.SymmRange);
            
            if (GaugeSettings.CenterLine)
                render.DrawArcCenterLine(drawArc, ColorTranslator.FromHtml(GaugeSettings.CenterLineColor), ModelDisplayText.GetNumValue(GaugeSettings.CenterLineThickness, 2));

            render.DrawArcIndicator(drawArc, ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), ModelDisplayText.GetNumValue(GaugeSettings.IndicatorSize, 10), ModelDisplayText.GetNumValue(value, 0), min, max, GaugeSettings.IndicatorFlip);
        }

        protected virtual void DrawText(string value, ImageRenderer render, bool defaultImage = false)
        {
            if (GaugeSettings.ShowText)
            {
                value = GaugeSettings.RoundValue(value);

                if (GaugeSettings.BarOrientation == (int)GaugeOrientation.LEFT && !GaugeSettings.DrawArc)
                    render.Rotate(180, new PointF(0, 0));

                GaugeSettings.GetFontParameters(TitleParameters, value, out Font drawFont, out Color drawColor);
                string text = ModelDisplay.FormatValue(value, GaugeSettings.Format);
                text = GaugeSettings.GetValueMapped(text);
                render.DrawText(text, drawFont, drawColor, ModelDisplayText.GetRectangleF(GaugeSettings.RectCoord));
            }
        }
    }
}
