﻿using CFIT.AppLogger;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using System;
using System.Diagnostics;

namespace PilotsDeck.UI.DeveloperUI
{
    public partial class NotifyIconViewModel : ObservableObject
    {
        [RelayCommand]
        public static void ToggleWindow()
        {
            try
            {
                if (App.CloseReceived || App.DeveloperView == null)
                    return;

                if (App.DeveloperView.IsVisible)
                    App.DeveloperView.Hide(enableEfficiencyMode: false);
                else
                    App.DeveloperView.Show(disableEfficiencyMode: true);
            }
            catch (Exception ex)
            {
                App.DeveloperView.Close();
                Logger.LogException(ex);
            }
        }

        [RelayCommand]
        public static void OpenProfileManager()
        {
            try
            {
                Process.Start(new ProcessStartInfo(@$"{App.PLUGIN_PATH}\ProfileManager.exe") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        [RelayCommand]
        public static void ExitApplication()
        {
            App.DoShutdown();
        }
    }
}
