using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Address
{
    public partial class ViewModelVariableAddress(IModelAddress source, string labelText) : ViewModelAddress(source, labelText)
    {
        protected override Enum GetAddressType()
        {
            var managedAddress = ManagedAddress;
            if (managedAddress?.IsRead == true && managedAddress?.IsEmpty == false)
                return managedAddress.ReadType;
            else
                return InvalidType.INVALID;
        }

        public override bool CheckAddressValid()
        {
            return ManagedAddress?.ReadType != SimValueType.NONE && ManagedAddress?.IsEmpty == false;
        }

        public override bool CheckInputValid(string address, out Enum detectedType)
        {
            var readType = TypeMatching.GetReadType(address);
            detectedType = readType;
            return readType != SimValueType.NONE;
        }

        protected override bool GetHasValue()
        {
            return true;
        }
    }
}
