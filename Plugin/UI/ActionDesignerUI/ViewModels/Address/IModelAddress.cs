namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Address
{
    public interface IModelAddress
    {
        public ViewModelAction ModelAction { get; }
        public string Address { get; set; }
    }

    public enum InvalidType
    {
        INVALID = 0
    }
}
