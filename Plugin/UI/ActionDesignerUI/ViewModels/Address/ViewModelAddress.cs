using CommunityToolkit.Mvvm.ComponentModel;
using PilotsDeck.Resources.Variables;
using System;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Address
{
    public abstract partial class ViewModelAddress : ViewModelBaseExtension<IModelAddress>
    {
        public virtual string Address { get => Source.Address; set => SetAddress(value); }
        public virtual ManagedAddress ManagedAddress => new(Address);
        public virtual Enum DisplayType => GetAddressType();
        public virtual bool HasValue => !IsTyping && GetHasValue();
        public virtual string Value => GetValue();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValue))]
        [NotifyPropertyChangedFor(nameof(Value))]
        protected bool _IsTyping = false;

        [ObservableProperty]
        protected string _LabelDescription = "Address:";

        [ObservableProperty]
        protected string _LabelInputCheck = "";

        [ObservableProperty]
        protected string _LabelType = "";

        [ObservableProperty]
        protected string _LabelValue = "";

        public ViewModelAddress(IModelAddress source, string labelText) : base(source, source.ModelAction)
        {
            if (!string.IsNullOrWhiteSpace(labelText))
                LabelDescription = labelText;
        }

        protected override void InitializeModel()
        {
            
        }

        protected virtual void SetAddress(string address)
        {
            Source.Address = address;
            NotifyAddressChange();
        }

        public virtual void NotifyAddressChange()
        {
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(ManagedAddress));
            OnPropertyChanged(nameof(DisplayType));

            if (IsTyping)
                IsTyping = false;
            else
            {
                OnPropertyChanged(nameof(HasValue));
                OnPropertyChanged(nameof(Value));
            }

            ModelAction.NotifyTreeRefresh();
        }

        protected virtual string GetValue()
        {
            if (App.PluginController.VariableManager.TryGet(ManagedAddress, out var variable))
                return variable?.Value ?? "0";
            else
                return "0";
        }

        protected abstract Enum GetAddressType();
        protected abstract bool GetHasValue();
        public abstract bool CheckInputValid(string address, out Enum detectedType);
        public abstract bool CheckAddressValid();

        public override string DisplayName => Address;
        public override string Name { get => DisplayName; set { } }
    }
}
