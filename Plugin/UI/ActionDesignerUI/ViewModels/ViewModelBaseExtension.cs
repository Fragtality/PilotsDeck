using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppTools;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels
{
    public interface IViewModelBaseExtension
    {
        public ICopyPasteSettings CopyPasteInterface { get; }
        public ViewModelAction ModelAction { get; }
        public object SourceObject { get; }
        public string DisplayName { get; }
        public string Name { get; set; }

        public void UpdateAction();
        public void NotifyPropertyChanged(string propertyName);
        public void SetModelValue<T>(T value, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!);
        public void SetModelArray<T>(T value, int index, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!);
        public void CopyToModelList<T>(ICollection<T> itemsSource, Func<T, T> transformItem = null, [CallerMemberName] string propertyName = null!);
        public void StepModelProperty<T>(double step = 1, T[] range = default, Func<T, T> transformator = null, [CallerMemberName] string propertyName = null!);
        public void SetModelEnum<T>(object parameter, bool useSource = true, [CallerMemberName] string propertyName = null!) where T : struct;
        //public T SourceCopy<T>();
    }

    public abstract partial class ViewModelBaseExtension<TObject>(TObject source, ViewModelAction modelAction) : ViewModelBase<TObject>(source), IViewModelBaseExtension, ICopyPasteSettings where TObject : class
    {
        public virtual ICopyPasteSettings CopyPasteInterface { get { return this as ICopyPasteSettings; } }
        public virtual ConcurrentDictionary<string, SettingPropertyBinding> SettingProperties { get; } = [];
        public virtual ViewModelAction ModelAction { get; } = modelAction;
        public virtual object SourceObject { get { return Source; } }
        public abstract string DisplayName { get; }
        public abstract string Name { get; set; }

        public virtual void UpdateAction()
        {
            ModelAction.UpdateAction();
        }

        public virtual void SubscribeCollection(IViewModelCollection modelCollection)
        {
            modelCollection.CollectionChanged += (_, _) => UpdateAction();
        }

        public virtual void SetModelValue<T>(T value, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            SetSourceValue(value, validator, callback, propertyName);
            UpdateAction();
        }

        public virtual void SetModelArray<T>(T value, int index, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            SetSourceArray(value, index, validator, callback, propertyName);
            UpdateAction();
        }

        public virtual void CopyToModelList<T>(ICollection<T> itemsSource, Func<T, T> transformItem = null, [CallerMemberName] string propertyName = null!)
        {
            if (itemsSource?.Count < 1 || Source.GetPropertyValue<object>(propertyName) is not ICollection<T> itemDestination || itemsSource == itemDestination)
                return;

            transformItem ??= (v) =>
            {
                if (v is ICloneable clonable)
                    return (T)clonable.Clone();
                else
                return v;
            };

            itemDestination.Clear();
            foreach (var item in itemsSource)
                itemDestination.Add(transformItem(item));
            UpdateAction();
        }

        public virtual void StepModelProperty<T>(double step = 1, T[] range = default, Func<T, T> transformator = null, [CallerMemberName] string propertyName = null!)
        {
            T value = this.GetPropertyValue<T>(propertyName);
            value = Extensions.StepNumber(value, step);

            if (range?.Length != 2 || range?.Length == 2 && Extensions.CompareRange(value, range))
            {
                if (transformator != null)
                    value = transformator(value);
                this.SetPropertyValue(propertyName, value);
            }

            UpdateAction();
            OnPropertyChanged(propertyName);
        }

        public virtual void SetModelEnum<T>(object parameter, bool useSource = true, [CallerMemberName] string propertyName = null!) where T : struct
        {
            if (!EnumExtensions.TryParseEnumValue(parameter, out T enumValue))
                return;

            if (useSource)
                SetModelValue(enumValue, null, null, propertyName);
            else
                this.SetPropertyValue(propertyName, enumValue);
        }

        protected virtual ManagedVariable GetVariable(string address)
        {
            return App.PluginController.VariableManager[new ManagedAddress(address)] ?? VariableManager.CreateEmptyVariable();
        }
    }
}
