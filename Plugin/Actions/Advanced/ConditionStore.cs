using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Actions.Advanced.SettingsModel;
using System.Collections.Concurrent;
using System.Linq;

namespace PilotsDeck.Actions.Advanced
{
    public class ConditionStore
    {
        public ModelManipulator Settings { get; set; }
        public ElementManipulator Parent { get; set; }
        public ConcurrentDictionary<int, ConditionHandler> Conditions { get; set; } = [];
        public bool AnyCondition { get { return Settings.AnyCondition; } }

        public ConditionStore(ModelManipulator model, ElementManipulator manipulator)
        {
            Settings = model;
            Parent = manipulator;
            SetConditions(model);
        }

        public bool HasChanges()
        {
            return Conditions.Values.Where(c => c.Variable?.IsChanged == true).Any();
        }

        public bool Compare()
        {
            int success = 0;
            foreach (var condition in Conditions.Values)
            {
                if (condition.Compare())
                {
                    if (AnyCondition)
                    {
                        success = Conditions.Count;
                        break;
                    }
                    else
                        success++;
                }
            }

            Logger.Verbose($"Compared Conditions - success: {success} | anycondition: {AnyCondition} | count {Conditions.Count}");
            return success > 0 && success == Conditions.Count;
        }

        public void SetConditions(ModelManipulator model)
        {
            foreach (var condition in model.Conditions)
                Conditions.TryAdd(condition.Key, new ConditionHandler(condition.Value));
        }

        public void RegisterRessources()
        {
            foreach (var condition in Conditions.Values)
            {
                if (condition.Variable == null)
                {
                    Logger.Verbose($"Register Variable '{condition.Address}'");
                    condition.Variable = App.PluginController.VariableManager.RegisterVariable(condition.Address);
                }
            }
        }

        public void DeregisterRessources()
        {
            foreach (var condition in Conditions.Values)
            {
                if (condition.Variable != null)
                {
                    Logger.Verbose($"Deregister Variable '{condition.Variable.Address}'");
                    App.PluginController.VariableManager.DeregisterVariable(condition.Variable.Address);
                    condition.Variable = null;
                }
            }
            Conditions.Clear();
        }
    }
}
