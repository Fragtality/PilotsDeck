using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using PilotsDeck.Resources.Variables;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.MSFS
{
    public class SubMapping
    {
        public ManagedVariable Variable { get; set; }
        public ISimResourceSubscription Subscription { get; set; }

        public SubMapping(ManagedVariable variable, ISimResourceSubscription subscription)
        {
            Variable = variable;
            Subscription = subscription;
            Subscription.OnReceived += OnReceived;
            Variable.IsSubscribed = true;
        }

        public Task Unsubscribe()
        {
            Variable.IsSubscribed = false;
            Subscription.OnReceived -= OnReceived;
            return Subscription.Unsubscribe();
        }

        protected Task OnReceived(ISimResourceSubscription sub, object data)
        {
            if (App.Configuration.VerboseLogging)
                Logger.Verbose($"Received {sub.Name} = {data}");
            if (Subscription.SimResource.IsNumeric)
                Variable.SetValue(sub.GetNumber());
            else
                Variable.SetValue(sub.GetString());

            return Task.CompletedTask;
        }

        public override bool Equals(object? obj)
        {
            if (obj is SubMapping map)
                return Variable?.Address == map?.Variable?.Address;
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Variable?.Address?.GetHashCode() ?? 0;
        }

        public static bool operator ==(SubMapping left, SubMapping right)
        {
            return left?.Variable?.Address == right?.Variable?.Address;
        }

        public static bool operator !=(SubMapping left, SubMapping right)
        {
            return left?.Variable?.Address != right?.Variable?.Address;
        }
    }
}
