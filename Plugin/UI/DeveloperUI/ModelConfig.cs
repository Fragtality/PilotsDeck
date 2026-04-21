using CFIT.AppFramework.UI.ViewModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace PilotsDeck.UI.DeveloperUI
{
    public class ModelConfig(AppConfiguration source) : ViewModelBase<AppConfiguration>(source)
    {
        protected override void InitializeModel()
        {

        }

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();
        }

        public virtual void SetModelValue<T>(T value, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            SetSourceValue(value, validator, callback, propertyName);
            AppConfiguration.SaveConfiguration();
        }

        public virtual int IntervalDeckRefresh { get => Source.IntervalDeckRefresh; set => SetModelValue<int>(value); }
        public virtual int IntervalSimProcess { get => Source.IntervalSimProcess; set => SetModelValue<int>(value); }
        public virtual bool UseFsuipcForMSFS { get => Source.UseFsuipcForMSFS; set => SetModelValue<bool>(value); }
        public virtual bool XPlaneUseWebApi { get => Source.XPlaneUseWebApi; set => SetModelValue<bool>(value); }
        public virtual bool XPlaneWebApiCmdOnly { get => Source.XPlaneWebApiCmdOnly; set => SetModelValue<bool>(value); }
    }
}
