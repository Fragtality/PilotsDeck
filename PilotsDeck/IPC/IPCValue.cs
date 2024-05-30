using System;

namespace PilotsDeck
{
    public abstract class IPCValue(string address) : IDisposable
    {
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public virtual void Process(SimulatorType simType)
        {

        }

        public virtual void Connect()
        {

        }

        public string Address { get; protected set; } = address;
        public string Value
        {
            get { return Read(); }
        }
        public abstract bool IsChanged { get; }

        public abstract dynamic RawValue();
        protected abstract string Read();

        public virtual void SetValue(string value)
        {

        }

        public virtual void SetValue(double value)
        {

        }
    }
}
