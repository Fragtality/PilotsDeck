using CFIT.AppLogger;
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

        public bool TryGet(ManagedVariable variable, out SubMapping mapping)
        {
            var query = SubscriptionMappings.Where(m => m.Key.Variable.Address == variable.Address);
            if (query?.Any() == true)
            {
                mapping = query.FirstOrDefault().Key;
                return true;
            }
            else
            {
                mapping = null;
                return false;
            }
        }

        public SubMapping Subscribe(ManagedVariable variable)
        {
            SubMapping mapping = null;
            if (variable == null)
                return mapping;
            if (TryGet(variable, out mapping))
            {
                Logger.Debug($"Resubscribe '{variable.Address}' (ResName: {mapping.Subscription.SimResource.Name})");
                mapping.Resubscribe(variable);
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

        public static void Unsubscribe(SubMapping mapping)
        {
            mapping?.Unsubscribe();
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
