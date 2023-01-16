using System;

namespace PilotsDeck
{
    public class IPCValueDataRefString : IPCValue
    {
        private char[] charArray;
        private string _lastString = "";
        private bool _valueChanged = false;
        public int Length { private set; get; }
        public string BaseAddress
        {
            get
            {
                return Address.Split(':')[0];
            }
        }

        public IPCValueDataRefString(string address) : base(address)
        {
            var parts = address.Split(':');
            if (int.TryParse(parts[1][1..], out int length))
            {
                Length = length;
                charArray = new char[length];
            }
            
        }

        public override bool IsChanged
        {
            get
            {
                return _valueChanged;
            }
        }

        public override void Process(SimulatorType simType)
        {
            string current = new(charArray);
            _valueChanged = _lastString != current;
            _lastString = current;
        }

        public override dynamic RawValue()
        {
            return Read();
        }

        protected override string Read()
        {
            return new string(charArray);
        }

        public virtual void SetChar(int index, char chr)
        {
            if (charArray != null && index >= 0 && index < charArray.Length)
            {
                charArray[index] = chr;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            charArray = null;
            GC.SuppressFinalize(this);
        }

        public static bool IsStringDataRef(string address)
        {
            var parts = address.Split(':');
            return parts.Length == 2 && parts[1].Length >= 2;
        }
    }
}
