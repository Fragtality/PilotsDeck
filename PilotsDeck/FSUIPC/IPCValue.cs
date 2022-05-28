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
            GC.SuppressFinalize(this);
        }

        public virtual void Process()
        {

        }

        public virtual void Connect()
        {

        }

        public string Address { get; protected set; }
        public string Value
        {
            get { return Read(); }
        }
        public abstract bool IsChanged { get; }

        public abstract dynamic RawValue();
        protected abstract string Read();        
    }
}
