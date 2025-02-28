using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace PilotsDeck.Actions.Advanced.SettingsModel
{
    public class ModelManipulator(ELEMENT_MANIPULATOR type)
    {
        [JsonConstructor]
        public ModelManipulator() : this (ELEMENT_MANIPULATOR.COLOR)
        {

        }

        public ModelManipulator Copy()
        {
            ModelManipulator model = new(this.ManipulatorType)
            {
                Name = this.Name,
                IsNewModel = this.IsNewModel,
                AnyCondition = this.AnyCondition,
                Color = this.Color,
                ResetVisibility = this.ResetVisibility,
                ResetDelay = this.ResetDelay,
                IndicatorAddress = this.IndicatorAddress,
                IndicatorScale = this.IndicatorScale,
                IndicatorType = this.IndicatorType,
                IndicatorImage = this.IndicatorImage,
                IndicatorColor = this.IndicatorColor,
                IndicatorSize = this.IndicatorSize,
                IndicatorLineSize = this.IndicatorLineSize,
                IndicatorOffset = this.IndicatorOffset,
                IndicatorReverse = this.IndicatorReverse,
                IndicatorFlip = this.IndicatorFlip,
                DynamicTransparency = this.DynamicTransparency,
                TransparencySetValue = this.TransparencySetValue,
                TransparencyAddress = this.TransparencyAddress,
                TransparencyMinValue = this.TransparencyMinValue,
                TransparencyMaxValue = this.TransparencyMaxValue,
                RotateContinous = this.RotateContinous,
                RotateAddress = this.RotateAddress,
                RotateAngleStart = this.RotateAngleStart,
                RotateAngleSweep = this.RotateAngleSweep,
                RotateMinValue = this.RotateMinValue,
                RotateMaxValue = this.RotateMaxValue,
                RotateToValue = this.RotateToValue,
                ConditionalFormat = this.ConditionalFormat.Copy(),
                ChangeX = this.ChangeX,
                ChangeY = this.ChangeY,
                ChangeW = this.ChangeW,
                ChangeH = this.ChangeH,
                ValueX = this.ValueX,
                ValueY = this.ValueY,
                ValueW = this.ValueW,
                ValueH = this.ValueH,
                ChangeSizePosDynamic = this.ChangeSizePosDynamic,
                SizePosAddress = this.SizePosAddress,
                SizePosMinValue = this.SizePosMinValue,
                SizePosMaxValue = this.SizePosMaxValue,
            };

            foreach (var condition in Conditions)
                model.Conditions.Add(condition.Key, condition.Value.Copy());

            return model;
        }

        public virtual bool IsNewModel { get; set; } = true;
        public virtual ELEMENT_MANIPULATOR ManipulatorType { get; set; } = type;
        public virtual string Name { get; set; } = "";
        public virtual bool AnyCondition { get; set; } = false;
        public virtual SortedDictionary<int, ConditionHandler> Conditions { get; set; } = [];

        //ManipulatorVisible
        public virtual bool ResetVisibility { get; set; } = false;
        public virtual int ResetDelay { get; set; } = 1500;

        //ManipulatorColor
        public virtual string Color { get; set; } = "#ffffff";

        public virtual Color GetColor()
        {
            return ColorTranslator.FromHtml(Color);
        }

        public virtual void SetColor(Color color)
        {
            Color = ColorTranslator.ToHtml(color);
        }

        //ManipulatorIndicator
        public virtual string IndicatorAddress { get; set; } = "";
        public virtual float IndicatorScale { get; set; } = 1;
        public virtual IndicatorType IndicatorType { get; set; } = IndicatorType.TRIANGLE;
        public virtual string IndicatorImage { get; set; } = "Images/indicator/Dial-White.png";
        public virtual string IndicatorColor { get; set; } = "#c7c7c7";
        public virtual float IndicatorSize { get; set; } = 10;
        public virtual float IndicatorLineSize { get; set; } = 2;
        public virtual float IndicatorOffset { get; set; } = 0;
        public virtual bool IndicatorReverse { get; set; } = false;
        public virtual bool IndicatorFlip { get; set; } = false;

        public virtual Color GetIndicatorColor()
        {
            return ColorTranslator.FromHtml(IndicatorColor);
        }

        public virtual void SetIndicatorColor(Color color)
        {
            IndicatorColor = ColorTranslator.ToHtml(color);
        }

        //ManipulatorTransparency
        public virtual bool DynamicTransparency { get; set; } = false;
        public virtual float TransparencySetValue { get; set; } = 0.5f;
        public virtual string TransparencyAddress { get; set; } = "";
        public virtual float TransparencyMinValue { get; set; } = 0;
        public virtual float TransparencyMaxValue { get; set; } = 100;

        //ManipulatorRotate
        public virtual bool RotateContinous { get; set; } = false;
        public virtual string RotateAddress { get; set; } = "";
        public virtual float RotateAngleStart { get; set; } = 0;
        public virtual float RotateAngleSweep { get; set; } = 360;
        public virtual float RotateMinValue { get; set; } = 0;
        public virtual float RotateMaxValue { get; set; } = 100;
        public virtual float RotateToValue { get; set; } = 0;

        //ManipulatorFormat
        public virtual ValueFormat ConditionalFormat { get; set; } = new();

        //ManipulatorSizePos
        public virtual bool ChangeX { get; set; } = false;
        public virtual float ValueX { get; set; } = 0;
        public virtual bool ChangeY { get; set; } = false;
        public virtual float ValueY { get; set; } = 0;
        public virtual bool ChangeW { get; set; } = false;
        public virtual float ValueW { get; set; } = 0;
        public virtual bool ChangeH { get; set; } = false;
        public virtual float ValueH { get; set; } = 0;
        public virtual bool ChangeSizePosDynamic { get; set; } = false;
        public virtual string SizePosAddress { get; set; } = "";
        public virtual float SizePosMinValue { get; set; } = 0;
        public virtual float SizePosMaxValue { get; set; } = 100;
    }
}
