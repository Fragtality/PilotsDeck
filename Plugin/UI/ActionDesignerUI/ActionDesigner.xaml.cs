using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.UI.ActionDesignerUI.TreeViews;
using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using PilotsDeck.UI.ActionDesignerUI.Views;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PilotsDeck.UI.ActionDesignerUI
{
    [ObservableObject]
    public partial class ActionDesigner : Window
    {
        public virtual ActionMeta Action { get; }
        public virtual ViewModelAction ModelAction { get; }
        protected virtual DispatcherTimer ShutdownMonitor { get; }
        protected virtual TreeItemData CurrentItem { get { return ModelAction.CurrentItem; } }

        protected virtual ActionTreeView TreeViewElements { get; }
        [ObservableProperty]
        protected bool _VisibilityElements = true;
        public virtual RelayCommand ToggleElementsCommand { get; }
        protected virtual ActionTreeView TreeViewCommands { get; }
        [ObservableProperty]
        protected bool _VisibilityCommands = true;
        public virtual RelayCommand ToggleCommandsCommand { get; }
        protected virtual double TreeSizeOffset { get; set; } = 144.0;
        protected virtual double ContentOffset { get; set; } = 56.0;

        public ActionDesigner(ActionMeta action)
        {
            InitializeComponent();
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            Action = action;
            ModelAction = new(action, this);
            this.DataContext = ModelAction;

            ShutdownMonitor = new()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalCheckUiClose)
            };
            ShutdownMonitor.Tick += CheckShutdown;
            ShutdownMonitor.Start();

            if (!string.IsNullOrWhiteSpace(Action.Title))
                this.Title = $"Action Designer - '{Action.Title}' ({Action.Context.ToUpperInvariant()})";
            else
                this.Title = $"Action Designer - {Action.Context.ToUpperInvariant()}";

            ToggleElementsCommand = new RelayCommand(() => VisibilityElements = !VisibilityElements);
            ToggleCommandsCommand = new RelayCommand(() => VisibilityCommands = !VisibilityCommands);
            TreeViewElements = new ActionTreeView(ModelAction, ModelAction.Elements);
            TreeViewCommands = new ActionTreeView(ModelAction, ModelAction.Commands);
            
            InitializeTreeViews();
            InitializeSubscribtions();
            ResizeContent();
        }

        protected virtual void InitializeSubscribtions()
        {
            this.Activated += Window_Activated;
            this.Loaded += (_, _) => { ModelAction.NotifyPropertyChanged(nameof(CurrentItem)); ResizeTrees(); ResizeContent();  };
            this.SizeChanged += (_, _) => { ResizeTrees(); ResizeContent(); };
            this.Closing += Window_Closing;
            this.PropertyChanged += Self_PropertyChanged;

            ModelAction.PropertyChanged += ModelAction_PropertyChanged;
            CombobBoxType.SelectionChanged += CombobBoxType_SelectionChanged;
            ModelAction.ItemsType.CollectionChanged += (_, _) => { CombobBoxType.SelectedIndex = ModelAction.GetSelectionIndex(); };
            ModelAction.SubscribeProperty(nameof(ModelAction.IsTreeRefreshNeeded), () => { TreeViewElements.ViewModelCollection.RefreshNames(); TreeViewCommands.ViewModelCollection.RefreshNames(); });

            PanelElementRoot.MouseLeftButtonUp += (_, _) => ModelAction.SetElementRootSelection();
            PanelElementRoot.MouseEnter += (_, _) => LabelAddNewElement.Visibility = Visibility.Visible;
            PanelElementRoot.MouseLeave += (_, _) => LabelAddNewElement.Visibility = Visibility.Hidden;
        }

        protected virtual void InitializeTreeViews()
        {
            TreeViewElements.AddToPanel(ControlElements);
            TreeViewCommands.AddToPanel(ControlCommands);
        }

        public virtual void CheckShutdown(object sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested || !App.ActiveDesigner.ContainsKey(Action.Context))
            {
                CloseDesigner();
                this.Close();
            }
        }

        protected virtual void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseDesigner();
        }

        protected virtual void CloseDesigner()
        {
            ShutdownMonitor.Stop();
            ModelAction.Clipboard.StopMonitor();
            App.ActiveDesigner.Remove(Action.Context);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        protected virtual void Window_Activated(object sender, EventArgs e)
        {
            Topmost = true;
            Focus();
            Topmost = false;
        }

        protected virtual void CombobBoxType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentItem?.IsElementTree() == true || CurrentItem?.IsElementAdd() == true || ModelAction.CheckStateTemplate())
            {
                ButtonAdd.CommandParameter = CombobBoxType.SelectedValue;
                e.Handled = true;
            }
        }

        protected virtual void ModelAction_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(ViewModelAction.CurrentItem))
            {
                SetView();
                if (CurrentItem?.IsCommandType() == true)
                    ButtonAdd.CommandParameter = CurrentItem.DeckCommandType;
            }
        }

        protected virtual void Self_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisibilityElements))
            {
                if (!VisibilityElements)
                {
                    TreeViewElements.ClearSelection();
                    if (CurrentItem.IsElementTree())
                        ModelAction.CurrentItem = TreeItemData.CreateNone();
                }
                ResizeTrees();
            }

            if (e.PropertyName == nameof(VisibilityCommands))
            {
                if (!VisibilityCommands)
                {
                    TreeViewCommands.ClearSelection();
                    if (CurrentItem.IsCommandTree())
                        ModelAction.CurrentItem = TreeItemData.CreateNone();
                }
                ResizeTrees();
            }
        }

        protected virtual void SetView()
        {
            ElementView.Content = null;

            if (CurrentItem.IsElementAdd())
            {
                TreeViewElements.ClearSelection();
                TreeViewCommands.ClearSelection();
            }
            else if (CurrentItem.IsElementTree())
                TreeViewCommands.ClearSelection();
            else if (CurrentItem.IsCommandTree())
                TreeViewElements.ClearSelection();

            if (CurrentItem.IsElement())
                ElementView.Content = new ViewElement(CurrentItem.GetModel<ViewModelElement>(), this);
            else if (CurrentItem.IsManipulator())
                ElementView.Content = new ViewManipulator(CurrentItem.GetModel<ViewModelManipulator>(), this);
            else if (CurrentItem.IsCondition())
                ElementView.Content = new ViewCondition(CurrentItem.GetModel<ViewModelCondition>(), this);
            else if (CurrentItem.IsCommand())
                ElementView.Content = new ViewCommand(CurrentItem.GetModel<ViewModelCommand>(), this);
            else if (CurrentItem.IsCommandType())
                ElementView.Content = new ViewCommandType(CurrentItem.DeckCommandType, ModelAction, this);

            ResizeContent();
            if (CurrentItem.IsElementTree())
                TreeViewElements.ViewModelCollection.RefreshFocus = true;
            else if (CurrentItem.IsCommandTree())
                TreeViewCommands.ViewModelCollection.RefreshFocus = true;
        }

        protected virtual void ResizeTrees()
        {
            try
            {
                if (VisibilityElements && VisibilityCommands)
                {
                    ControlElements.Height = (MainGrid.ActualHeight - TreeSizeOffset) / 2.0;
                    ControlCommands.Height = (MainGrid.ActualHeight - TreeSizeOffset) / 2.0;
                }
                else if (VisibilityElements)
                {
                    ControlElements.Height = MainGrid.ActualHeight - TreeSizeOffset;
                    ControlCommands.Height = 0;
                }
                else
                {
                    ControlElements.Height = 0;
                    ControlCommands.Height = MainGrid.ActualHeight - TreeSizeOffset;
                }
            }
            catch { }
        }

        protected virtual void ResizeContent()
        {
            try
            {
                if (ElementView.Content != null)
                {
                    ElementView.Height = MainGrid.ActualHeight - ContentOffset;
                }
            }
            catch { }
        }

        protected virtual void NotifyUpdateNotification(string propertyName)
        {
            PropertyChanged.Invoke(this, new(propertyName));
        }

        protected virtual void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error("---- ACTION DESIGNER CRASH ----");
            Logger.LogException(e.Exception);
            e.Handled = true;
            this.Close();
        }
    }
}
