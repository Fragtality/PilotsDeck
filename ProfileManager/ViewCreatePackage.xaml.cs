using CFIT.AppLogger;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ProfileManager
{
    public partial class ViewCreatePackage : UserControl
    {
        protected PackageController PackageController { get; }
        protected ViewModelManifest ModelManifest { get; set; }

        public ViewCreatePackage()
        {
            InitializeComponent();
            GridOpenDirectory.Visibility = Visibility.Visible;
            GridPackageEditor.Visibility = Visibility.Collapsed;

            PackageController = new();
            ButtonFindPath.Command = new RelayCommand(CommandFindPath);
            ButtonLoadPath.Command = new RelayCommand(CommandLoadPath);

            SetNonEmptyValidation(TextTitle);
            SetNonEmptyValidation(TextVersion);
            SetNonEmptyValidation(TextAircraft);
            SetNonEmptyValidation(TextAuthor);
            SetNonEmptyValidation(TextPluginVersion);
        }

        protected static void SetNonEmptyValidation(TextBox textBox)
        {
            textBox.GetBindingExpression(TextBox.TextProperty).ParentBinding.ValidationRules.Add(new NonEmptyRule());
        }

        protected void CommandFindPath()
        {
            using var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            openFolderDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            openFolderDialog.AutoUpgradeEnabled = true;
            openFolderDialog.ShowNewFolderButton = true;
            openFolderDialog.ShowPinnedPlaces = true;
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextPackagePath.Text = openFolderDialog.SelectedPath;
        }

        protected void CommandLoadPath()
        {
            string path = TextPackagePath?.Text;
            if (string.IsNullOrWhiteSpace(path) || !System.IO.Path.Exists(path))
                return;

            PackageController.LoadPackagePath(path);
            ModelManifest = new(PackageController.Manifest);
            this.DataContext = ModelManifest;
            TextPath.Text = path;

            ButtonOpenFolder.Command = new RelayCommand(() => { Process.Start(new ProcessStartInfo(PackageController.PathPackage) { UseShellExecute = true }); });
            ButtonSaveManifest.Command = new RelayCommand(() => { PackageController.SaveManifest(); });
            ButtonCreatePackage.Command = new RelayCommand(CreatePackage);

            GridOpenDirectory.Visibility = Visibility.Collapsed;
            GridPackageEditor.Visibility = Visibility.Visible;

            TextPath.Focus();
            TextPath.CaretIndex = TextPath.Text.Length;
        }

        protected void CreatePackage()
        {
            if (PackageController.CheckVersionExists())
            {
                if (MessageBox.Show($"A Package File for Version {ModelManifest.VersionPackage} already exists!\r\nAre you sure you want to override the existing Package?",
                                    "Package File already exists",
                                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)
                    == MessageBoxResult.Yes)
                PackageController.CreatePackage();
            }
            else
                PackageController.CreatePackage();
        }
    }

    public class NonEmptyRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                if (value is not string text)
                    return new ValidationResult(false, $"Value is not a string!");
                if (string.IsNullOrWhiteSpace(text))
                    return new ValidationResult(false, $"Value is empty!");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return new ValidationResult(false, $"{ex.GetType().Name}: {ex.Message}");
            }
            return ValidationResult.ValidResult;
        }
    }
}
