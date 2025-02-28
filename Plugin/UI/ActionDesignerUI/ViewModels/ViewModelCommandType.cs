using PilotsDeck.Actions;
using System.Collections.Generic;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels
{
    public partial class ViewModelCommandType(StreamDeckCommand type, ViewModelAction parent) : ViewModelBaseExtension<ViewModelAction>(parent, parent)
    {
        public StreamDeckCommand CommandType { get; } = type;
        public virtual SortedDictionary<StreamDeckCommand, int> ActionDelays => Source.Settings.ActionDelays;

        protected override void InitializeModel()
        {
            CreateMemberIntegerBinding<int>(nameof(Delay), App.Configuration.InterActionDelay.ToString());
        }

        public virtual int Delay
        {
            get => ActionDelays[CommandType];
            set { ActionDelays[CommandType] = value; UpdateAction(); }
        }

        public override string DisplayName => CommandType.ToString();
        public override string Name { get => DisplayName; set { } }
    }
}
