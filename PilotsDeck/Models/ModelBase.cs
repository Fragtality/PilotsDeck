namespace PilotsDeck
{
    public class ModelBase
    {
        public virtual string DefaultImage { get; set; }
        public virtual string ErrorImage { get; set; }
        public virtual bool IsInitialized { get; set; } = false;

        public virtual void Update()
        {

        }
    }
}
