using PilotsDeck.Simulator;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PilotsDeck.Actions.Advanced.SettingsModel
{
    public class ModelCommand
    {
        public ModelCommand Copy()
        {
            ModelCommand model = new()
            {
                DeckCommandType = this.DeckCommandType,
                CommandType = this.CommandType,
                DoNotRequestBvar = this.DoNotRequestBvar,
                UseXpCommandOnce = this.UseXpCommandOnce,
                Address = this.Address,
                Name = this.Name,
                TimeAfterLastDown = this.TimeAfterLastDown,
                TickDelay = this.TickDelay,
                ResetSwitch = this.ResetSwitch,
                ResetValue = this.ResetValue,
                ResetDelay = this.ResetDelay,
                UseCommandDelay = this.UseCommandDelay,
                CommandDelay = this.CommandDelay,
                WriteValue = this.WriteValue,
                AnyCondition = this.AnyCondition,
            };

            foreach (var condition in Conditions)
                model.Conditions.Add(condition.Key, condition.Value.Copy());

            return model;
        }

        public ModelCommand()
        {

        }

        public ModelCommand(StreamDeckCommand deckType)
        {
            DeckCommandType = deckType;
        }

        public StreamDeckCommand DeckCommandType { get; set; } = StreamDeckCommand.KEY_UP;
        public SimCommandType CommandType { get; set; } = SimCommandType.LVAR;
        public bool DoNotRequestBvar { get; set; } = true;
        public bool UseXpCommandOnce { get; set; } = true;
        public string Address { get; set; } = "";
        public string Name { get; set; } = "";
        public int TimeAfterLastDown { get; set; } = 0;
        public int TickDelay { get; set; } = App.Configuration.TickDelay;
        public bool ResetSwitch { get; set; } = false;
        public string ResetValue { get; set; } = "";
        public int ResetDelay { get; set; } = App.Configuration.VariableResetDelay;
        public bool UseCommandDelay { get; set; } = false;
        public int CommandDelay { get; set; } = App.Configuration.CommandDelay;
        public string WriteValue { get; set; } = "";
        public SortedDictionary<int, ConditionHandler> Conditions { get; set; } = [];
        public bool AnyCondition { get; set; } = false;

        public ConcurrentDictionary<int, ConditionHandler> GetConditions()
        {
            var dict = new ConcurrentDictionary<int, ConditionHandler>();

            foreach (var condition in Conditions)
                dict.TryAdd(condition.Key, new ConditionHandler(condition.Value));

            return dict;
        }
    }
}
