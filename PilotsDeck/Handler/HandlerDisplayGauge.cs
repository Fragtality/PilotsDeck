using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayGauge : HandlerDisplayText
    {
        public virtual ModelDisplayGauge GaugeSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplayGauge Settings { get; protected set; }

        public override string Address { get { return GaugeSettings.Address; } }
        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplayGauge] Read: {Address}"; } }
        public override bool UseFont { get { return true; } }

        protected bool IsArc = false;

        public HandlerDisplayGauge(string context, ModelDisplayGauge settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
            IsArc = settings.DrawArc;
        }

        public override bool OnButtonDown(long tick)
        {
            if (GaugeSettings.HasAction)
            {
                TickDown = tick;
                return HandlerSwitch.RunButtonDown(SwitchSettings);
            }
            else
                return false;
        }

        public override bool OnButtonUp(IPCManager ipcManager, long tick)
        {
            if (GaugeSettings.HasAction)
            {
                bool result = HandlerSwitch.RunButtonUp(ipcManager, (tick - TickDown) >= AppSettings.longPressTicks, ValueManager[ID.SwitchState], ValueManager[ID.SwitchStateLong], SwitchSettings);
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
                ValueManager.RegisterValue("AddressColorOff", GaugeSettings.AddressColorOff);
            
            RenderDefaultImage(imgManager);
        }

        public override void Deregister(ImageManager imgManager)
        {
            if (GaugeSettings.UseColorSwitching)
                ValueManager.DeregisterValue("AddressColorOff");

            base.Deregister(imgManager);
        }

        public override void Update(ImageManager imgManager)
        {
            HasAction = GaugeSettings.HasAction;
            base.Update(imgManager);
            
            if (GaugeSettings.UseColorSwitching)
                ValueManager.UpdateValueAddress("AddressColorOff", GaugeSettings.AddressColorOff);

            RenderDefaultImage(imgManager);
            NeedRedraw = true;

            if (IsArc != GaugeSettings.DrawArc)
            {
                GaugeSettings.ResetCoords();
                IsArc = GaugeSettings.DrawArc;
                UpdateSettingsModel = true;
            }
        }

        protected virtual void RenderDefaultImage(ImageManager imgManager)
        {
            ImageRenderer render = new (imgManager.GetImageObject(GaugeSettings.DefaultImage, DeckType));

            if (GaugeSettings.DrawArc)
            {
                render.DrawArc(GaugeSettings.GetArc(), ColorTranslator.FromHtml(GaugeSettings.GaugeColor));
            }
            else
            {
                render.Rotate(GaugeSettings.BarOrientation, new PointF(0, 0));
                render.DrawBar(ColorTranslator.FromHtml(GaugeSettings.GaugeColor), GaugeSettings.GetBar());
            }
            
            DefaultImageRender = render.RenderImage64();
            render.Dispose();
        }

        public override void SetDefault()
        {
            if (DrawImage != DefaultImageRender)
            {
                DrawImage = DefaultImageRender;
                IsRawImage = true;
                NeedRedraw = true;
            }
        }

        protected override void Redraw(ImageManager imgManager)
        {
            if (!ValueManager.IsChanged(ID.ControlState) && !ValueManager.IsChanged("AddressColorOff") && !ForceUpdate)
                return;

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            
            string value = ValueManager[ID.ControlState];
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            ImageRenderer render = new (imgManager.GetImageObject(GaugeSettings.DefaultImage, DeckType));

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

            DrawImage = render.RenderImage64();
            render.Dispose();
            IsRawImage = true;
            NeedRedraw = true;
            
            //sw.Stop();
            //Log.Logger.Debug($"Time for Gauge-Frame: {sw.Elapsed.TotalMilliseconds}ms [{ActionID}]");
        }

        protected virtual void DrawBar(string value, ImageRenderer render)
        {
            Bar drawBar = GaugeSettings.GetBar();
            float min = ModelDisplayText.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayText.GetNumValue(GaugeSettings.MaximumValue, 100);

            bool useOffColor = GaugeSettings.UseColorSwitching && (!string.IsNullOrEmpty(ValueManager["AddressColorOff"]) && ModelBase.Compare(GaugeSettings.StateColorOff, ValueManager["AddressColorOff"]));
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

        protected virtual void DrawArc(string value, ImageRenderer render)
        {
            Arc drawArc = GaugeSettings.GetArc();
            float min = ModelDisplayText.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayText.GetNumValue(GaugeSettings.MaximumValue, 100);

            bool useOffColor = GaugeSettings.UseColorSwitching && (!string.IsNullOrEmpty(ValueManager["AddressColorOff"]) && ModelBase.Compare(GaugeSettings.StateColorOff, ValueManager["AddressColorOff"]));
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

        protected virtual void DrawText(string value, ImageRenderer render)
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
