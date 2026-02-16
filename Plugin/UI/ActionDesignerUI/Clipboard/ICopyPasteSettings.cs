using CFIT.AppTools;
using CommunityToolkit.Mvvm.Input;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PilotsDeck.UI.ActionDesignerUI.Clipboard
{
    public enum SettingType
    {
        NONE = 0,
        COLOR = 1,
        FONT,
        SIZE,
        POS,
        VALUEMAP,
        GAUGEMAP,
        GAUGEMARKER
    }

    public class SettingPropertyBinding(string propertyName, SettingType settingType, List<string> notifications)
    {
        public virtual string Name { get; } = propertyName;
        public virtual SettingType Type { get; } = settingType;
        public virtual List<string> Notifications { get; } = notifications;
    }

    public interface ICopyPasteSettings
    {
        public bool IsSettingSupported(SettingType settingType)
        {
            return SettingProperties.Where((kv) => kv.Value.Type == settingType).Any();
        }

        public ConcurrentDictionary<string, SettingPropertyBinding> SettingProperties { get; }

        public void BindProperty(string propertyName, SettingType settingType, params string[] notifications)
        {
            var binding = new SettingPropertyBinding(propertyName, settingType, notifications != null && notifications.Length > 0 ? [.. notifications] : []);
            SettingProperties.Add(propertyName, binding);
        }

        public RelayCommand GetCopyCommand(string propertyName, SettingType settingType)
        {
            if (!IsSettingSupported(settingType) || !SettingClipboard.IsEmpty || !SettingProperties.TryGetValue(propertyName, out var type) || type.Type != settingType)
                return null;

            if (settingType == SettingType.COLOR)
                return BuildCopyCommand<Color>(propertyName, settingType);
            else if (settingType == SettingType.SIZE || settingType == SettingType.POS)
                return BuildCopyCommand<float[]>(propertyName, settingType);
            else if (settingType == SettingType.FONT)
                return BuildCopyCommand<FontSetting>(propertyName, settingType);
            else if (settingType == SettingType.VALUEMAP)
                return BuildCopyCommand<ICollection<KeyValuePair<string, string>>>(propertyName, settingType);
            else if (settingType == SettingType.GAUGEMAP)
                return BuildCopyCommand<ICollection<ColorRange>>(propertyName, settingType);
            else if (settingType == SettingType.GAUGEMARKER)
                return BuildCopyCommand<GaugeMarkerSettings>(propertyName, settingType);

            return null;
        }

        protected RelayCommand BuildCopyCommand<T>(string propertyName, SettingType settingType)
        {
            return new RelayCommand(() => SettingClipboard.CopyToClipboard(CopyTask<T>(propertyName), settingType), () => SettingClipboard.IsEmpty);
        }

        protected T CopyTask<T>(string propertyName)
        {
            if (this is ViewModelElement viewModel && viewModel.Source.IsPropertyType<T>(propertyName))
            {
                if (typeof(T).IsArray)
                {
                    var value = viewModel.GetSourceValue<T>(propertyName);
                    return (T)((ICloneable)value).Clone();
                }
                else
                    return viewModel.GetSourceValue<T>(propertyName);

            }
            else
                return this.GetPropertyValue<T>(propertyName);
        }

        public RelayCommand GetPasteCommand(string propertyName, SettingType settingType)
        {
            if (!IsSettingSupported(settingType) || SettingClipboard.IsEmpty || !SettingProperties.TryGetValue(propertyName, out var binding) || binding.Type != settingType)
                return null;

            if (settingType == SettingType.COLOR)
                return BuildPasteCommand<Color>(binding);
            else if (settingType == SettingType.SIZE || settingType == SettingType.POS)
                return BuildPasteCommand<float[]>(binding);
            else if (settingType == SettingType.FONT)
                return BuildPasteCommand<FontSetting>(binding);
            else if (settingType == SettingType.VALUEMAP)
                return BuildPasteCommand<ICollection<KeyValuePair<string, string>>>(binding);
            else if (settingType == SettingType.GAUGEMAP)
                return BuildPasteCommand<ICollection<ColorRange>>(binding);
            else if (settingType == SettingType.GAUGEMARKER)
                return BuildPasteCommand<GaugeMarkerSettings>(binding);

            return null;
        }

        protected RelayCommand BuildPasteCommand<T>(SettingPropertyBinding binding)
        {
            return new RelayCommand(() => PasteTask<T>(binding),
                    () => { return !SettingClipboard.IsEmpty && SettingClipboard.IsType(binding.Type); });
        }

        protected void PasteTask<T>(SettingPropertyBinding binding)
        {
            bool isViewModel = this is IViewModelBaseExtension;
            var viewModel = this as IViewModelBaseExtension;

            if (isViewModel && viewModel.SourceObject.IsPropertyType<T>(binding.Name))
                viewModel.SetModelValue(SettingClipboard.PasteFromClipboard<T>(), null, null, binding.Name);
            else
            {
                this.SetPropertyValue(binding.Name, SettingClipboard.PasteFromClipboard<T>());
                if (isViewModel)
                    viewModel.UpdateAction();
            }

            if (isViewModel && binding.Notifications.Count > 0)
            {
                foreach (var notification in binding.Notifications)
                    viewModel.NotifyPropertyChanged(notification);
            }
        }
    }
}
