using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ProfileManager
{
    public partial class ViewProfileMapper : UserControl
    {
        public enum Filter
        {
            ALL = 0,
            MAPPED,
            UNMAPPED,
            CHANGED
        }

        public class ProfileFilter(Filter index, string name)
        {
            public Filter Index { get; set; } = index;
            public string Name { get; set; } = $"{name} Profiles";
        }

        public static readonly List<ProfileFilter> profileFilters = [ new(Filter.ALL, "All"), new(Filter.MAPPED, "Mapped"), new(Filter.UNMAPPED, "Unmapped"), new(Filter.CHANGED, "Changed")  ];
        protected bool StartUpCompleted { get; set; } = false;

        public ProfileController ProfileController { get; protected set; } = new();
        public List<ProfileListItem> ProfileListItems { get; protected set; } = [];
        public IEnumerable<ProfileListItem> FilteredItems { get; protected set; } = [];

        protected DispatcherTimer ViewUpdateTimer { get; set; }

        protected Brush BrushDefault { get; set; }

        protected Brush BrushBlack { get; } = new SolidColorBrush(Colors.Black);
        protected Brush BrushGreen { get; } = new SolidColorBrush(Colors.Green);
        protected Brush BrushRed { get; } = new SolidColorBrush(Colors.Red);
        protected Brush BrushOrange { get; } = new SolidColorBrush(Colors.Orange);
        protected Brush BrushBlue { get; } = SystemColors.HighlightBrush;

        public ViewProfileMapper()
        {
            try
            { 
                InitializeComponent();
                BrushDefault = BtnProfileSave.BorderBrush;

                SelectProfileFilter.ItemsSource = new ObservableCollection<ProfileFilter>(profileFilters);
                SelectProfileFilter.SelectedValue = Filter.ALL;

                LoadProfileData();

                ViewUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(200)
                };
                ViewUpdateTimer.Tick += ViewUpdateTick;
                ViewUpdateTimer.Start();

                MainWindow.Instance.Top = 16;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected const double heightScalar = 3;
        protected const double staticHeight = 48;

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (ProfileListView.Children.Count > 0)
            {
                double minsize = double.MaxValue;
                foreach (var child in ProfileListView.Children)
                {
                    if (child is ProfileListItem && (child as ProfileListItem).ActualHeight < minsize)
                        minsize = (child as ProfileListItem).ActualHeight;
                }


                if (minsize != double.MaxValue && (minsize * heightScalar) + staticHeight != this.MaxHeight)
                    this.MaxHeight = (minsize * heightScalar) + staticHeight;
            }
        }

        protected void LoadProfileData()
        {
            try
            {
                ProfileListView.Children.Clear();
                ProfileListItems.Clear();
                ProfileController.Load();

                if (ProfileController.IsLoaded)
                {
                    foreach (var manifest in ProfileController.ProfileManifests)
                        ProfileListItems.Add(new ProfileListItem(manifest, UpdateAllViews));

                    List<string> deviceFilters = ["All Decks"];
                    deviceFilters.AddRange(ProfileController.DeviceInfos.Select(d => d.Name));
                    SelectDeckFilter.ItemsSource = new ObservableCollection<string>(deviceFilters);
                    SelectDeckFilter.SelectedIndex = 0;

                    RedrawListView();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void UpdateAllViews()
        {
            foreach (var item in ProfileListItems)
                item.UpdateView();
        }

        protected void RedrawListView()
        {
            string name = TextboxNameFilter?.Text?.ToLowerInvariant() ?? "";

            if (SelectProfileFilter.SelectedIndex == (int)Filter.MAPPED)
                FilteredItems = ProfileListItems.Where(m => m.ViewModel.IsMappedProfile && m.ViewModel.ProfileName.ToLowerInvariant().Contains(name));
            else if (SelectProfileFilter.SelectedIndex == (int)Filter.UNMAPPED)
                FilteredItems = ProfileListItems.Where(m => !m.ViewModel.IsMappedProfile && m.ViewModel.ProfileName.ToLowerInvariant().Contains(name));
            else if (SelectProfileFilter.SelectedIndex == (int)Filter.CHANGED)
                FilteredItems = ProfileListItems.Where(m => m.ViewModel.IsChanged && m.ViewModel.ProfileName.ToLowerInvariant().Contains(name));
            else
                FilteredItems = ProfileListItems.Where(m => m.ViewModel.ProfileName.ToLowerInvariant().Contains(name));

            if (SelectDeckFilter.SelectedIndex != 0 && !string.IsNullOrEmpty(SelectDeckFilter.SelectedValue?.ToString()))
                FilteredItems = FilteredItems.Where(i => i.ViewModel.DeckName.Equals(SelectDeckFilter.SelectedValue.ToString(), StringComparison.InvariantCultureIgnoreCase));

            ProfileListView.Children.Clear();
            foreach (var item in FilteredItems)
                ProfileListView.Children.Add(item);
        }

        protected void CallOnAllItems(Action<ProfileListItem> method)
        {
            foreach (var profileItem in ProfileListItems)
                method(profileItem);
        }

        protected void CallOnAllItems(Func<ProfileListItem, bool> filter, Action<ProfileListItem> method)
        {
            foreach (var profileItem in ProfileListItems.Where(filter))
                method(profileItem);
        }

        protected static void SetLabel(TextBlock labelControl, FrameworkElement control, Brush brush, Visibility visibility, string text = null)
        {
            labelControl.Foreground = brush;
            labelControl.Visibility = visibility;
            if (text != null)
                labelControl.Text = text;

            if (control != null)
                control.Visibility = visibility;
        }

        protected void UpdateLabels()
        {
            CountProfileDisplayed.Text = FilteredItems.Count().ToString();

            if (ProfileController.HasError)
                SetLabel(LabelLoadError, ImageLoadError, BrushRed, Visibility.Visible);
            else
                SetLabel(LabelLoadError, ImageLoadError, BrushBlack, Visibility.Collapsed);

            int count = ProfileController.ProfileMappings.Where(m => !m.IsProfileNever).Count();
            if (count != 0)
                SetLabel(CountProfileMappings, null, BrushGreen, Visibility.Visible, count.ToString());
            else
                SetLabel(CountProfileMappings, null, BrushBlack, Visibility.Visible, "0");

            count = ProfileController.CountMappingsChanged + ProfileController.CountMappingsRemoved;
            if (count != 0)
                SetLabel(CountMappingsChangedRemoved, LabelMappingsChangedRemoved, BrushOrange, Visibility.Visible, count.ToString());
            else
                SetLabel(CountMappingsChangedRemoved, LabelMappingsChangedRemoved, BrushBlack, Visibility.Collapsed, "0");

            count = ProfileController.CountManifestsChanged + ProfileController.CountManifestsRemoved;
            if (count != 0)
                SetLabel(CountManifestsChangedRemoved, LabelManifestsChangedRemoved, BrushOrange, Visibility.Visible, count.ToString());
            else
                SetLabel(CountManifestsChangedRemoved, LabelManifestsChangedRemoved, BrushBlack, Visibility.Collapsed, "0");

            count = ProfileController.CountMappingsUnmatched;
            if (count != 0)
                SetLabel(CountMappingsUnmatched, LabelMappingsUnmatched, BrushRed, Visibility.Visible, count.ToString());
            else
                SetLabel(CountMappingsUnmatched, LabelMappingsUnmatched, BrushBlack, Visibility.Collapsed, "0");
        }

        protected static void ToggleButtonState(Button button, bool btnState)
        {
            button.IsHitTestVisible = btnState;
            button.Opacity = (btnState ? 1 : 0.5);
        }

        protected static void SetButtonStyle(Button button, Brush brush, double thickness, string log = null, Image image = null, string file = null)
        {
            if (!string.IsNullOrWhiteSpace(log))
                Logger.Debug(log);

            button.BorderBrush = brush;
            button.BorderThickness = new Thickness(thickness);

            if (image != null && !string.IsNullOrEmpty(file))
                Tools.SetButtonImage(image, file);
        }

        protected void UpdateButtons()
        {
            ToggleButtonState(BtnProfileSave, IsSaveStateValid());
            ToggleButtonState(BtnProfileRefresh, !ProfileController.AppsRunning);

            if (ProfileController.HasChanges && BtnProfileSave.BorderBrush != BrushOrange)
            {
                SetButtonStyle(BtnProfileSave, BrushOrange, 2, "Setting unsaved State on BtnProfileSave", ImgProfileSave, "file-earmark-excel-fill");
                SetButtonStyle(BtnProfileRefresh, BrushRed, 2, "Setting warning State on BtnProfileRefresh");
            }
            else if (!ProfileController.HasChanges && BtnProfileSave.BorderBrush == BrushOrange)
            {
                SetButtonStyle(BtnProfileSave, BrushDefault, 1, "Setting normal State on BtnProfileSave", ImgProfileSave, "file-earmark-check");
            }

            if (ProfileController.HasError && BtnProfileRefresh.BorderBrush != BrushBlue)
            {
                SetButtonStyle(BtnProfileRefresh, BrushBlue, 2, "Setting warning State on BtnProfileRefresh");
            }
            else if (!ProfileController.HasError && !ProfileController.HasChanges && BtnProfileRefresh.BorderBrush != BrushDefault)
            {
                SetButtonStyle(BtnProfileRefresh, BrushDefault, 1, "Setting normal State on BtnProfileRefresh");
            }
        }

        protected void SetSaveHighlight()
        {
            SetButtonStyle(BtnProfileSave, BrushGreen, 2, "Setting saved State on BtnProfileSave", ImgProfileSave, "file-earmark-check");
            CallOnAllItems(m => m.UpdateBorder());
            
            var timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (sender, args) =>
            {
                SetButtonStyle(BtnProfileSave, BrushDefault, 1);
                timer.Stop();
            };
            timer.Start();
        }

        protected void ViewUpdateTick(object sender, EventArgs e)
        {
            UpdateButtons();
            UpdateLabels();
            if (!StartUpCompleted)
                StartUpCompleted = true;
        }

        public bool IsSaveStateValid()
        {
            return ProfileController.HasChanges && !ProfileController.HasError && ProfileController.IsLoaded && !ProfileController.AppsRunning;
        }

        private void BtnProfileSave_Click(object sender, RoutedEventArgs e)
        {
            if (!StartUpCompleted)
                return;

            if (!IsSaveStateValid())
            {
                Logger.Error($"Save Requested while Controller is in a Bad Sate! (Changes: {ProfileController.HasChanges} | Errors: {ProfileController.HasError} | Loaded: {ProfileController.IsLoaded} | Apps: {ProfileController.AppsRunning})");
                return;
            }

            bool doLoad = ProfileController.CountManifestsRemoved > 0 || ProfileController.CountMappingsRemoved > 0;
            ProfileController.SaveChanges();

            if (doLoad)
            {
                Logger.Debug("Deleted Manifests/Mappings - Reload after Save");
                LoadProfileData();
            }

            SetSaveHighlight();
        }

        private void BtnProfileRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!StartUpCompleted)
                return;

            if (ProfileController.AppsRunning)
            {
                Logger.Error($"Refresh clicked while App running!");
                return;
            }

            bool doLoad = true;
            if (ProfileController.HasChanges)
            {
                Logger.Warning($"Refresh clicked with unsaved Changes");
                var result = MessageBox.Show("There are unsaved Changes to your Profiles!\r\nContinue with Refresh?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    doLoad = false;
            }

            if (doLoad)
                LoadProfileData();
        }

        private void SelectFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!StartUpCompleted)
                return;

            RedrawListView();
        }

        public void Window_Closing()
        {
            if (IsSaveStateValid())
            {
                Logger.Warning($"Close requested with unsaved Changes");
                var result = MessageBox.Show("There are unsaved Changes to your Profiles!\r\nSave before closing?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                    ProfileController.SaveChanges();
            }
        }

        private void TextboxNameFilter_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RedrawListView();
        }
    }
}
