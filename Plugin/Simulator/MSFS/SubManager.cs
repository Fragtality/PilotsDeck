using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.InputEvents;
using CFIT.SimConnectLib.Modules.MobiFlight;
using CFIT.SimConnectLib.SimEvents;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using PilotsDeck.Resources.Variables;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.MSFS
{
    public class SubManager(SimConnectManager manager, MobiModule mobi)
    {
        protected SimConnectManager SimConnect { get; } = manager;
        protected SimVarManager VarManager { get { return SimConnect.VariableManager; } }
        protected SimEventManager EventManager { get { return SimConnect.EventManager; } }
        protected InputEventManager InputManager { get { return SimConnect.InputManager; } }
        protected MobiModule MobiModule { get; } = mobi;
        protected ConcurrentDictionary<SubMapping, bool> SubscriptionMappings { get; } = [];

        public bool Contains(ManagedVariable variable)
        {
            return SubscriptionMappings.Where(m => m.Key.Variable.Address == variable.Address).Any();
        }

        public bool Contains(string name)
        {
            return SubscriptionMappings.Where(m => m.Key.Subscription.Name == name).Any();
        }

        public bool TryGet(ManagedVariable variable, out SubMapping mapping)
        {
            var query = SubscriptionMappings.Where(m => m.Key.Variable.Address == variable.Address);
            if (query?.Any() == true)
            {
                mapping = query.First().Key;
                return true;
            }
            else
            {
                mapping = null;
                return false;
            }
        }

        public async Task<SubMapping> Subscribe(ManagedVariable variable)
        {
            SubMapping mapping = null;
            if (variable == null)
                return mapping;

            if (TryGet(variable, out mapping))
            {
                variable.IsSubscribed = true;
                return mapping;
            }

            ISimResourceSubscription subscription = null;
            if (variable.Type == SimValueType.AVAR)
            {
                if (variable.Address.Prefix != "L:")
                    subscription = VarManager.Subscribe(variable.Address.Name, variable.Address.Parameter);
                else
                    subscription = VarManager.Subscribe(variable.Address.NamePrefixed, variable.Address.Parameter);
            }
            else if (variable.Type == SimValueType.LVAR)
            {
                subscription = VarManager.Subscribe(variable.Address.NamePrefixed, variable.Address.Parameter);
            }
            else if (variable.Type == SimValueType.BVAR)
            {
                subscription = InputManager.Subscribe(variable.Address.Name);
            }
            else if (variable.Type == SimValueType.KVAR)
            {
                subscription = EventManager.Subscribe(variable.Address.Name);
            }
            else if (variable.Type == SimValueType.CALCULATOR && MobiModule.IsMobiConnected)
            {
                subscription = MobiModule.SubscribeCode(variable.Address.Name);
            }

            if (subscription != null)
            {
                mapping = new SubMapping(variable, subscription);
                SubscriptionMappings.Add(mapping);
            }

            return mapping;
        }

        public virtual async Task Unsubscribe(SubMapping mapping)
        {
            if (mapping != null)
            {
                await mapping.Unsubscribe();
                mapping.Subscription = null;
                SubscriptionMappings.Remove(mapping);
            }
        }

        public void Clear()
        {
            foreach (var mapping in SubscriptionMappings)
            {
                mapping.Key.Unsubscribe();
                mapping.Key.Subscription = null;
            }

            SubscriptionMappings.Clear();
        }
    }
}
