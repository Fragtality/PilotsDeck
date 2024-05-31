using System.Globalization;

namespace PilotsDeck
{
    public class IPCValueLua(string _address) : IPCValue(_address)
    {
        private bool isChanged = false;
        private string _value = "";
        private string lastValue = "";
        public override bool IsChanged { get { return isChanged; } }

        public override void Process(SimulatorType simType)
        {
            SetValue(Plugin.ActionController.ipcManager.ScriptManager.RunFunction(Address, false));
        }

        public override void SetValue(string strValue)
        {
            //if (double.TryParse(strValue, NumberStyles.Number, new RealInvariantFormat(strValue), out double value))
            //{
            //    string num = Convert.ToString(value, CultureInfo.InvariantCulture.NumberFormat);

            //    int idxE = num.IndexOf('E');
            //    if (idxE < 0)
            //        strValue = num;
            //    else
            //        strValue = string.Format("{0:F1}", value);
            //}
            _value = strValue;
            isChanged = lastValue != strValue;
            lastValue = strValue;
        }

        protected override string Read()
        {
            return _value;
        }

        public override dynamic RawValue()
        {
            if (double.TryParse(_value, NumberStyles.Number, new RealInvariantFormat(_value), out double value))
                return value;
            else
                return _value;
        }
    }
}
