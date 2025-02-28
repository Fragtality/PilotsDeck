using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Tools;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.UI;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public partial class ViewModelImage : ViewModelElement
    {
        protected virtual ViewModelElement ParentModel { get; }
        public virtual CommandWrapper SetImageCommand { get; }

        public ViewModelImage(ViewModelElement viewModel) : base(viewModel.Source, viewModel.ModelAction)
        {
            ParentModel = viewModel;
            ParentModel.SubscribeProperty(nameof(ParentModel.Color), () => NotifyPropertyChanged(nameof(BackgroundBrush)));
            SetImageCommand = new(ShowImageDialog);
            RefreshImageInfo();
        }        

        protected virtual void ShowImageDialog()
        {
            DialogImage dialog = new(Source.Image ?? "", ModelAction.WindowInstance);
            if (dialog.ShowDialog() == true)
            {
                Source.Image = dialog.ImageResult;
                UpdateAction();
                RefreshImageInfo();
                OnPropertyChanged(nameof(ImageSource));
                ModelAction.NotifyTreeRefresh();
            }
        }

        protected virtual void RefreshImageInfo()
        {
            if (ModelAction?.CurrentItem?.ElementID == null || ModelAction?.CurrentItem?.ElementID == -1
                || !ModelAction.Action.DisplayElements.ContainsKey(ModelAction.CurrentItem.ElementID))
            {
                ImageInfo = "";
                return;
            }

            var images = (ModelAction.Action.DisplayElements[ModelAction.CurrentItem.ElementID] as ElementImage)?.Image?.Images;
            var filenames = (ModelAction.Action.DisplayElements[ModelAction.CurrentItem.ElementID] as ElementImage)?.Image?.FileNames;

            if (images != null && filenames != null)
            {
                StringBuilder sb = new();
                foreach (var key in images.Keys)
                {
                    sb.AppendLine($"{filenames[key]}: {(int)images[key].Width} x {(int)images[key].Height}");
                }
                ImageInfo = sb.ToString();
            }
            else
                ImageInfo = "";
        }

        public virtual BitmapImage ImageSource
        {
            get
            {
                var img = Img.GetBitmapFromFile(Source.Image) ?? Img.GetBitmapFromFile(AppConfiguration.WaitImage);
                if (img?.Width == img?.Height)
                    ImageWidth = 72;
                else
                    ImageWidth = 100;
                OnPropertyChanged(nameof(ImageWidth));
                return img;
            }
        }

        public virtual int ImageWidth { get; protected set; }

        public virtual bool DrawImageBackground
        {
            get => GetSourceValue<bool>();
            set
            {
                SetModelValue<bool>(value);
                OnPropertyChanged(nameof(BackgroundBrush));
                ParentModel.NotifyPropertyChanged(nameof(ParentModel.ElementHasColor));
            }
        }


        public static SolidColorBrush DefaultBackground { get; } = new SolidColorBrush(System.Windows.Media.Color.FromRgb(219, 219, 219));
        public virtual SolidColorBrush BackgroundBrush { get => DrawImageBackground ? new SolidColorBrush(Color.Convert()) : DefaultBackground; }

        [ObservableProperty]
        protected string _ImageInfo;
    }
}
