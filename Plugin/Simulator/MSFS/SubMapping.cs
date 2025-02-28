using CFIT.SimConnectLib.SimResources;
using PilotsDeck.Resources.Variables;

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
            Subscription.OnReceived += (sub, value) =>
            {
                if (Subscription.SimResource.IsNumeric)
                    Variable.SetValue(sub.GetNumber());
                else
                    Variable.SetValue(sub.GetString());
            };
            Variable.IsSubscribed = true;
        }

        public void Resubscribe(ManagedVariable variable)
        {
            Variable = variable;
            Subscription.Subscribe();

            Subscription.OnReceived += (sub, value) =>
            {
                if (Subscription.SimResource.IsNumeric)
                    Variable.SetValue(sub.GetNumber());
                else
                    Variable.SetValue(sub.GetString());
            };
            Variable.IsSubscribed = true;
        }

        public void Unsubscribe()
        {
            Subscription.Unsubscribe();
            Variable.IsSubscribed = false;
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
