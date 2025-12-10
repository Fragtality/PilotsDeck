using CFIT.AppLogger;
using PilotsDeck.Plugin;
using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI
{
    public partial class DialogImage : Window
    {
        protected PropertyInspectorModel Model { get; set; }
        public Window ParentWindow { get; set; }
        public string ImageResult { get; protected set; } = "";
        public List<string> ImageVariants { get; } = [];
        protected string ImageFilter { get; set; } = "";
        protected SelectableImage LastSelectedImage { get; set; } = null;
        protected bool Refreshing { get; set; } = false;
        protected SolidColorBrush InactiveBrush { get; set; }

        public DialogImage(string selectedImage = "", Window parent = null)
        {
            InitializeComponent();
            ImageResult = selectedImage;
            Model = new();
            ParentWindow = parent;
            if (ColorConverter.ConvertFromString("#DBDBDB") is Color color)
                InactiveBrush = new SolidColorBrush(color);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Refreshing = true;
            int index = 0;
            int selectedIndex = 0;
            Dictionary<string, string> selectedDict = Model.ImageDictionary.Values.First();
            ComboImageDir.Items.Clear();
            foreach (var dir in Model.ImageDictionary)
            {
                ComboImageDir.Items.Add(dir.Key);
                if (GetImageDir(ImageResult) == dir.Key)
                {
                    selectedIndex = index;
                    selectedDict = dir.Value;
                }
                index++;
            }
            ComboImageDir.SelectedIndex = selectedIndex;
            FillImagePanel(selectedDict, out SelectableImage selectedImage);
            if (selectedImage != null)
            {
                selectedImage.BringIntoView();
                LastSelectedImage = selectedImage;
                FillImageVariants();
            }
            Refreshing = false;

            if (ParentWindow != null)
            {
                this.Top = ParentWindow.Top + (ParentWindow.ActualHeight / 2.0) - (this.ActualHeight / 2.0);
                this.Left = ParentWindow.Left + (ParentWindow.ActualWidth / 2.0) - (this.ActualWidth / 2.0);
            }
        }

        protected virtual void FillImageVariants()
        {
            ImageVariants.Clear();

            try
            {
                string directory = Path.GetDirectoryName(LastSelectedImage.ImageFile);
                var baseFile = Path.GetFileNameWithoutExtension(LastSelectedImage.ImageFile);
                var fileInfos = (new DirectoryInfo(directory).EnumerateFiles($"{baseFile}@*{AppConfiguration.ImageExtension}", PropertyInspectorModel.GetEnumerationOptions()));

                var bitmap = Img.GetBitmapFromFile(LastSelectedImage.ImageFile);
                ImageVariants.Add($"{LastSelectedImage.ImageFile}: {(int)bitmap.Width} x {(int)bitmap.Height}");
                foreach (var info in fileInfos)
                {
                    bitmap = Img.GetBitmapFromFile(info.FullName);
                    ImageVariants.Add($"{Path.Join(directory, info.Name).Replace('\\', '/')}: {(int)bitmap.Width} x {(int)bitmap.Height}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            UpdateLabelWithVariants();
        }

        protected virtual void UpdateLabelWithVariants()
        {
            StringBuilder sb = new();

            foreach (var variant in ImageVariants)
            {
                sb.Append(variant[(variant.LastIndexOf(':') + 1)..].Trim());
                sb.Append("\r\n");
            }

            LabelFileInfo.Content = sb.ToString();
        }

        public static string GetImageDir(string file)
        {    
            int idx = file.LastIndexOf('/');
            if (idx != -1)
                file = file[0..(idx + 1)];

            if (file.StartsWith("Images/") == true)
                file = file.Replace("Images/", "/");

            if (file.Length > 1 && file.EndsWith('/'))
                file = file[0..(file.Length -1)];

            return file;
        }

        private void FillImagePanel(Dictionary<string, string> imageDict, out SelectableImage selected)
        {
            PanelImages.Children.Clear();
            selected = null;
            if (imageDict == null)
                return;

            foreach (var img in imageDict)
            {
                if (!string.IsNullOrWhiteSpace(ImageFilter) && !img.Value.ToLower().Contains(ImageFilter.ToLower()))
                    continue;

                var bitmap = Img.GetBitmapFromFile(img.Value);
                if (bitmap != null)
                {
                    var image = new SelectableImage();
                    if (bitmap.Width == bitmap.Height)
                        image.Width = 72;
                    else
                        image.Width = bitmap.Width;
                    
                    image.Source = bitmap;
                    image.ImageFile = img.Value;
                    image.MouseLeftButtonUp += ImageFile_OnMouseLeftButtonUp;
                    var border = new Border
                    {
                        Child = image,
                        BorderThickness = new Thickness(2.5),
                        BorderBrush = InactiveBrush
                    };
                    if (!string.IsNullOrEmpty(ImageResult) && ImageResult == img.Value)
                    {
                        border.BorderBrush = SystemColors.HighlightBrush;
                        LabelFileName.Content = img.Value;
                        selected = image;
                    }
                    image.Border = border;
                    PanelImages.Children.Add(border);
                }
            }
        }

        private void ComboImageDir_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Refreshing)
                return;

            if (ComboImageDir.SelectedValue is string dir && Model.ImageDictionary.TryGetValue(dir, out Dictionary<string, string> imageDict))
            {
                FillImagePanel(imageDict, out _);
            }
        }

        private void ImageFile_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is SelectableImage image && image.ImageFile != ImageResult)
            {
                ImageResult = image.ImageFile;
                LabelFileName.Content = image.ImageFile;
                image.Border.BorderBrush = SystemColors.HighlightBrush;

                LastSelectedImage?.Border.BorderBrush = InactiveBrush;
                LastSelectedImage = image;
                FillImageVariants();
            }
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ImageResult))
                DialogResult = true;
            else
                DialogResult = false;

            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InputSearch_KeyUp(object sender, KeyEventArgs e)
        {
            ImageFilter = InputSearch.Text;
            ComboImageDir_SelectionChanged(null, null);
        }
    }

    public class SelectableImage : Image
    {
        public string ImageFile { get; set; }
        public Border Border { get; set; }
    }
}
