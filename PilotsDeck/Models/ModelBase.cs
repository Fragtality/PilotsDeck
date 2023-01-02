using System.Globalization;

namespace PilotsDeck
{
    public class ModelBase
    {
        public virtual string DefaultImage { get; set; }
        public virtual string ErrorImage { get; set; }

        public virtual bool SwitchOnCurrentValue { get; set; } = false;

        public static bool Compare(string a, string b)
        {
            if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && (a.Contains('<') || a.Contains('>')))
            {
                bool greater = a.Contains('>');
                a = a.Replace("=","").Replace("<","").Replace(">","");

                float fa = ModelDisplayText.GetNumValue(a, 0.0f);
                float fb = ModelDisplayText.GetNumValue(b, 0.0f);
                
                if (greater)
                        return fb >= fa;
                    else
                    return fb <= fa;
            }
            else if (!string.IsNullOrEmpty(a) && float.TryParse(a, NumberStyles.Number, new RealInvariantFormat(a), out _) && float.TryParse(b, NumberStyles.Number, new RealInvariantFormat(b), out _))
            {
                return ModelDisplayText.GetNumValue(a, 0.0f) == ModelDisplayText.GetNumValue(b, 0.0f);
            }
            else
            {
                return a == b;
            }
        }
    }
}
