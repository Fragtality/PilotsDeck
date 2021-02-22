namespace PilotsDeck
{
    public class ModelDisplayGaugeDual : ModelDisplayGauge
    {
        public string Address2 { get; set; } = "";

        public virtual string RectCoord2 { get; set; } = "6; 6; 60; 21";

        public ModelDisplayGaugeDual()
        {
            IndicatorFlip = true;
        }

        public override void ResetCoords()
        {
            base.ResetCoords();

            if (!DrawArc)
                IndicatorFlip = true;
            else
                IndicatorFlip = false;
        }
    }
}
