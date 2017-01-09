namespace FoldMyIcons
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;
    using Folders;
    using JetBrains.Annotations;
    using PostSharp.Patterns.Model;
    using Puchalapalli.Infrastructure.Media.Icons.Folders;
    using Puchalapalli.WPF.Controls.Buttons;
    using Puchalapalli.WPF.Controls.Editors.Collections;
    using Puchalapalli.WPF.Controls.FileSystemExplorer;
    using Puchalapalli.WPF.Infrastructure.InfoReporters;
    using Puchalapalli.WPF.Interactivity.Commands;
    using Puchalapalli.WPF.TelerikUI.Controls.Trees.Extensions;
    using Puchalapalli.WPF.TelerikUI.Themes;
    using Puchalapalli.WPF.Themes;
    using Puchalapalli.WPF.Utilities.Execution;
    using Telerik.Windows.Controls;
    using ViewModels;
    using static Properties.Settings.LibSettings.LibSettings;
    using static Properties.Arguments.ApplicationArguments;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [NotifyPropertyChanged]
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            VisualStudio2013Palette.LoadPreset(FileSystemExplorerControl.VisualStudio2013ColorVariation);
            InitializeComponent();
            //DirectoryExplorer.PropertyChanged += DirectoryExplorerOnPropertyChanged;
            //var binding = DirectoryExplorer.GetBindingExpression(FileSystemExplorerListControl.SelectedItemProperty);
            //DirectoryExplorer.SourceUpdated += (s,e) => DirectoryExplorerOnTargetUpdated(s,e,false);
            //DirectoryExplorer.TargetUpdated += (s, e) => DirectoryExplorerOnTargetUpdated(s, e, true);
            InitializeViewModel();
            LoadSettings();
        }

        //private void DirectoryExplorerOnTargetUpdated(object sender, DataTransferEventArgs e, bool target)
        //{
        //    var propertyName = e.Property.Name;

        //    switch (propertyName)
        //    {
        //        case nameof(DirectoryExplorer.SelectedItem):
        //            Debug.WriteLine("{1} {0} {1}", propertyName, DirectoryExplorer.SelectedItem,
        //                "=========================");
        //            var binding = DirectoryExplorer.GetBindingExpression(FileSystemExplorerListControl.SelectedItemProperty);
        //            if (target)
        //            {
        //                this.ViewModel.SelectedDirectory = DirectoryExplorer.SelectedItem;
        //                binding?.UpdateSource();
        //            }
        //            //this.ViewModel.OnPropertyChanged(nameof(ViewModel.SelectedDirectory));
        //            break;
        //    }
        //}

        //private void DirectoryExplorerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    var propertyName = e.PropertyName;
        //    switch (propertyName)
        //    {
        //        case nameof(DirectoryExplorer.SelectedItem):
        //            Debug.WriteLine("{1} {0} {1}", propertyName, DirectoryExplorer.SelectedItem,
        //                "-------------------------");
        //            var binding = DirectoryExplorer.GetBindingExpression(FileSystemExplorerListControl.SelectedItemProperty);
        //            this.ViewModel.OnPropertyChanged(nameof(ViewModel.SelectedDirectory));
        //            break;
        //    }
        //}

        #region Initialization

        private void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            ViewModel.Settings.Theme.Current = TelerikThemes.VisualStudio2013.Blue;
            AppearanceManager.RegisterComponent<SwitchButton>();
            AppearanceManager.RegisterComponent<CollectionEditor>();
            AppearanceManager.RegisterSource<RadBreadcrumb>();
            LoadTheme();
        }
        void InitializeViewModel()
        {
            ViewModel.Initialize(new ListBoxInfoReporter(RadListStatus));
            ViewModel.OpenDirectoryIconInBrowser = new RelayCommand(OpenDirectoryIconInBrowserImpl, () => !string.IsNullOrWhiteSpace(ViewModel.SelectedDirectory?.IconPath));
            ViewModel.OpenIconInBrowser = IconExplorer.OpenPath;
            ViewModel.OpenDirectoryInBrowser = DirectoryExplorer.OpenPath;
        }
        #endregion Initialization
        #region Theming

        void UpdateTheme()
        {
            LoadTheme();
            ViewModel.Settings.Save();
        }

        void LoadTheme()
            => ViewModel.Settings.Theme.Current.Load();

        #endregion END: Theming
        void OpenDirectoryIconInBrowserImpl()
        {
            IconExplorer.SelectItemByPath(DirectoryExplorer.SelectedItem.IconPath);
        }
        #region Interfaces: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        private void LoadSettings()
        {
#pragma warning disable 4014
            LoadSavedDirectory(Options.Directory ?? LibSetting.Directories.DirectoryExplorer, DirectoryExplorer);
            LoadSavedDirectory(Options.Icon ?? LibSetting.Directories.IconExplorer, IconExplorer);
#pragma warning restore 4014
        }

        private async Task LoadSavedDirectory(string directory, FileSystemExplorerListControl control, int attempt=0)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return;
            if (attempt == 0)
            {
                control.Loaded += async (s, e) => await Timers.RunTask(() => LoadSavedDirectory(directory, control, attempt + 1), 1, true).ConfigureAwait(true);
                return;
            }
            for (attempt = 1; attempt < 10; attempt++)
            {
                await control.SelectItemByPath(directory).ConfigureAwait(true);
                if (control.SelectedItem?.HierarchyPath == directory)
                {
                    return;
                }
                await Task.Delay(3000).ConfigureAwait(true);
            }
            // Debugger.Break();
        }
        private void SaveSettings(bool includeWindowPlacement = false)
        {
            ViewModel.SaveSettingsImpl(includeWindowPlacement);
        }
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SaveSettings(true);
        }

        private void SetRootFolder_OnClick(object sender, RoutedEventArgs e)
        {
            RadButtonSetRootFolders.IsOpen = false;
            var element = sender as FrameworkElement;
            var tag = element?.Tag as IconRootType?;
            if (tag == null)
                return;
            var type = tag.Value;
            var item = DirectoryExplorer.SelectedItem;
            Options.Roots.Set(type, null);
            //if (Default.Roots == null)
            //    Default.Roots = new IconRootFolders();
            LibSetting.Directories.Roots.Set(type, item.IsFileSystemEntry ? item.FullPath : null);
            SaveSettings();
        }

        private void ScrollIntoViewAsync(object sender, RoutedEventArgs e)
        {
            DirectoryExplorer.ScrollIntoViewAsync();
        }

        private void ExpandItemIntoView(object sender, RoutedEventArgs e)
        {
            DirectoryExplorer.ItemsControl.ExpandItemIntoView(DirectoryExplorer.SelectedItem);
        }

        private void SelectItemByPath(object sender, RoutedEventArgs e)
        {
            DirectoryExplorer.SelectItemByPath(DirectoryExplorer.SelectedItem?.FullPath);
        }

        private void ScrollItemToTop(object sender, RoutedEventArgs e)
        {
            DirectoryExplorer.ItemsControl.ScrollItemToTop(DirectoryExplorer.SelectedItem);
        }
        private void ScrollItemToTopAlt(object sender, RoutedEventArgs e)
        {
            DirectoryExplorer.ItemsControl.ScrollToTopOfView(DirectoryExplorer.SelectedItem);
        }
    }
}
