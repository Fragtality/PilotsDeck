using System;

namespace PilotsDeck
{
    public abstract class IPCValue : IDisposable
    {
        public IPCValue(string address)
        {
            Address = address;
        }

        public virtual void Dispose()
        {

        }

        public virtual void Process()
        {

        }

        public string Address { get; protected set; }
        public string Value
        {
            get { return Read(); }
        }
        public abstract bool IsChanged { get; }

        public abstract string ScaledValue(double scalar);
        public abstract dynamic RawValue();
        protected abstract string Read();        
    }
}
