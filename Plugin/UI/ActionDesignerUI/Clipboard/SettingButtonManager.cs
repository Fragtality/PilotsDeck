using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PilotsDeck.UI.ActionDesignerUI.Clipboard
{
    public class SettingButtonManager
    {
        protected virtual DispatcherTimer CopyMonitor { get; }
        protected virtual ICopyPasteSettings SettingInterface { get; }
        protected virtual List<SettingButtonBinding> ButtonBindings { get; } = [];

        public SettingButtonManager(ICopyPasteSettings @interface)
        {
            SettingInterface = @interface;
            CopyMonitor = new()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            CopyMonitor.Tick += CheckCopy;
        }

        protected virtual void CheckCopy(object sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested)
            {
                StopMonitor();
            }
            else
            {
                foreach (var binding in ButtonBindings)
                    binding.UpdateBinding(SettingInterface);
            }
        }

        public virtual void StartMonitor()
        {
            CopyMonitor?.Start();
        }

        public virtual void StopMonitor()
        {
            CopyMonitor?.Stop();
            ButtonBindings?.Clear();
        }

        public virtual void BindParent(FrameworkElement element)
        {
            element.Loaded += (_, _) => StartMonitor();
            element.Unloaded += (_, _) => StopMonitor();
        }

        public virtual void BindButton(Button button, string propertyName, SettingType type)
        {
            if (SettingInterface.IsSettingSupported(type))
                ButtonBindings.Add(new(button, propertyName, type));
            else
            {
                button.Command = null;
                button.IsEnabled = false;
            }
        }
    }
}
