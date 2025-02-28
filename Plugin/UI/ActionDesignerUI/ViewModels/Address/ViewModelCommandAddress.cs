using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using System;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Address
{
    public partial class ViewModelCommandAddress(IModelAddress source, string labelText) : ViewModelAddress(source, labelText)
    {
        public virtual ViewModelCommand ModelCommand { get; } = source as ViewModelCommand;
        public override ManagedAddress ManagedAddress => new(Address, ModelCommand.CommandType, ModelCommand.DoNotRequestBvar);

        protected override Enum GetAddressType()
        {
            var managedAddress = ManagedAddress;
            if (managedAddress?.IsCommand == true)
                return managedAddress.CommandType;
            else
                return InvalidType.INVALID;
        }

        public override bool CheckAddressValid()
        {
            return SimCommand.IsValidAddressForType(Address, ModelCommand.CommandType, ModelCommand.DoNotRequestBvar);
        }

        public override bool CheckInputValid(string address, out Enum detectedType)
        {
            if (SimCommand.IsValidAddressForType(address, ModelCommand.CommandType, ModelCommand.DoNotRequestBvar))
            {
                detectedType = ModelCommand.CommandType;
                return true;
            }
            else
            {
                detectedType = InvalidType.INVALID;
                return false;
            }
        }

        protected override bool GetHasValue()
        {
            return SimCommand.IsValidValueCommand(Address, ModelCommand.DoNotRequestBvar, ModelCommand.CommandType);
        }
    }
}
