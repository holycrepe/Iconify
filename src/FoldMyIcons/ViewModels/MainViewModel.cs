namespace FoldMyIcons.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Alphaleonis.Win32.Filesystem;
    using Commands;
    using Folders;
    using JetBrains.Annotations;
    using Models.Favorites;
    using Models.Generation;
    using PostSharp.Patterns.Model;
    using Properties.Settings.AllSettings;
    using Properties.Settings.LibSettings;
    using Puchalapalli.Aspects.Diagnostics;
    using Puchalapalli.Extensions.Collections;
    using Puchalapalli.Extensions.DateTime;
    using Puchalapalli.Extensions.Primitives;
    using Puchalapalli.Infrastructure.Classes.Comparer;
    using Puchalapalli.Infrastructure.Media.Icons;
    using Puchalapalli.Infrastructure.Media.Icons.Extensions;
    using Puchalapalli.Infrastructure.Media.Icons.Folders;
    using Puchalapalli.Infrastructure.Operations.Progress;
    using Puchalapalli.Infrastructure.Reporting.Reports;
    using Puchalapalli.IO;
    using Puchalapalli.IO.AlphaFS.Extensions;
    using Puchalapalli.IO.AlphaFS.Info;
    using Puchalapalli.IO.Common.Search;
    using Puchalapalli.IO.Folders;
    using Puchalapalli.WPF.Controls.Dialogs;
    using Puchalapalli.WPF.Controls.FileSystemExplorer;
    using Puchalapalli.WPF.Infrastructure.InfoReporters;
    using Puchalapalli.WPF.Interactivity.Commands;
    using static Properties.Settings.LibSettings.LibSettings;
    using DirectoriesIO = Puchalapalli.IO.Info.Directories;

    [NotifyPropertyChanged]
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        public const double MAX_VERBOSITY = 12;
        const int NEW_ICON_VERBOSITY = 3;
        const int MANUAL_VERBOSITY = NEW_ICON_VERBOSITY + 4;        
        //const int MAX_DEGREE_OF_PARALLELISM = 12;
        private const double MINIMUM_TIMING_INTERVAL = 3D;

        private static readonly string[] IconExtensions = new[]
        {
            "ico",
            "icl"
        };

        /// <summary>
        /// <para>
        /// Each template is a format specification that may reference any of the following placeholders:
        /// </para>  
        /// {0}: Icon Extension without leading period (ico or icl)<br />
        /// {1}: Directory Name of the Current Directory
        /// {2}: Directory Name of the Icon Generation Source/Folder
        /// </summary>
        private static readonly Dictionary<double, IconGenerationTemplates> IconNameTemplates = new Dictionary
            <double, IconGenerationTemplates>
            {
                [1] = new IconGenerationTemplates
                {
                    @"folder.{0}"
                },
                [2] = new IconGenerationTemplates("{1}")
                {
                    @"{1}.{0}",
                    @"{1}\{1}.{0}"
                },
                [3] = new IconGenerationTemplates("{2}")
                {
                    @"{2}.{0}",
                    @"{2}\{2}.{0}"
                }
            };

        public WorkerStopwatch Stopwatch { get; } = new WorkerStopwatch();
        public double Total { get; set; } = 100;
        public double Progress { get; set; } = 50;
        public bool IsActive { get; set; } = true;

        #region Infrastructure

        private readonly BackgroundWorker _bgwCommand;


        // ReSharper disable once InconsistentNaming
        private ListBoxInfoReporter InfoReporter;
        [SafeForDependencyAnalysis]

        #endregion Infrastructure

        #region Settings

        public LibSettings Settings
            => LibSetting;
        public LibToggleSettings Toggles
            => Settings.Toggles;
        public LibDirectorySettings Directories
            => Settings.Directories;

        #endregion Settings

        #region State

        private bool _mayRunCommand = true;

        [SafeForDependencyAnalysis]
        public bool MayRunCommand
        {
            get
            {
                return Toggles.Async
                    ? _bgwCommand?.IsBusy == false
                    : _mayRunCommand;
            }
            set
            {
                _mayRunCommand = value;
                OnPropertyChanged();
            }
        }

        public int Updates { get; set; } 
        //public Stopwatch Elapsed { get; set; }
        public ExplorerItem SelectedDirectory { get; set; }
        public ExplorerItem SelectedIcon { get; set; }
        #endregion State

        public IconRootInfos Roots { get; set; } = new IconRootInfos();
        // ReSharper disable once MemberCanBePrivate.Global, UnusedAutoPropertyAccessor.Global
        public static MainViewModel Current { get; set; }

        public MainViewModel()
        {
            Current = this;
            // ReSharper disable once UseObjectOrCollectionInitializer
            _bgwCommand = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            _bgwCommand.DoWork += bgwCommand_DoWork;
            _bgwCommand.ProgressChanged += OnCommandWorkerProgressChanged;
            _bgwCommand.RunWorkerCompleted += OnCommandWorkerCompleted;
            OnPropertyChanged(nameof(MayRunCommand));
        }

        public void Initialize(ListBoxInfoReporter infoReporter)
        {
            InfoReporter = infoReporter;
            OpenFavorite = new RelayCommand<FavoritePath>(OpenFavoriteImpl);
            EditFavorite = new RelayCommand<FavoritePath>(EditFavoriteImpl);
            DeleteFavorite = new RelayCommand<FavoritePath>(DeleteFavoriteImpl);
            AddFavorite = new RelayCommand<FavoritePathType?>(AddFavoriteImpl);
            SaveSettings = new RelayCommand(() => SaveSettingsImpl());
            ResetCaches = new RelayCommand(ResetCachesImpl);
            SetIconTree = new RelayCommand(() => RunCommand(FolderIconCommand.SetIconTree), () => MayRunCommand);
            SetIconsAuto = new RelayCommand(() => RunCommand(FolderIconCommand.SetIconsAuto), () => MayRunCommand);
            ApplyFolderIcons = new RelayCommand(() => RunCommand(FolderIconCommand.ApplyFolderIcons), () => MayRunCommand);
            GenerateSidecarFiles = new RelayCommand(() => RunCommand(FolderIconCommand.GenerateSidecarFiles), () => MayRunCommand);
            FixAttributes = new RelayCommand(() => RunCommand(FolderIconCommand.FixAttributes), () => MayRunCommand);
            SaveIconToSidecar = new RelayCommand(SaveIconToSidecarImpl, () => MayRunCommand);
            OnPropertyChanged(nameof(MayRunCommand));
        }

        void ResetCachesImpl()
        {
            FolderInfoCache.Clear();
            FolderIconCache.Clear();
            IconReferenceCache.Clear();
        }
        public void SaveSettingsImpl(bool includeWindowPlacement=false)
        {
            Directories.DirectoryExplorer = SelectedDirectory?.HierarchyPath;
            Directories.IconExplorer = SelectedIcon?.HierarchyPath;
            AllSettings.SaveAllSettings(includeWindowPlacement);
        }
        #region Background Worker


        private void OnCommandWorkerCompleted(object sender = null, RunWorkerCompletedEventArgs e = null)
        {
            //var bgw = (BackgroundWorker)sender;
            //var result = e.Result;
            OnPropertyChanged(nameof(MayRunCommand));
            ReportStatus(new ReportedStatus($"Operation Complete: " +
                                            $"{Updates} Updates Made in {Stopwatch.Elapsed.FormatFriendly()}" +
                                            $", at {Updates / Stopwatch.Elapsed.TotalSeconds:F2} " +
                                            $"updates/sec",
                                            foreground: "#FFDED3D3", 
                                            background: "#FF007ACC"));
            Stopwatch.ReportComplete();
            //throw new NotImplementedException();
        }

        private void OnCommandWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
            => OnCommandWorkerProgressChanged(e.ProgressPercentage, (ReportedStatus)e.UserState);

        // ReSharper disable once UnusedParameter.Local
        private void OnCommandWorkerProgressChanged(int progressPercentage, ReportedStatus status)
        {
            Updates++;
            Stopwatch.ReportProgress(status.Status, status.Title, status.Text, Updates);
            if (Settings.Verbosity >= status.Verbosity)
                ReportStatus(status);
        }
        private void bgwCommand_DoWork(object sender, DoWorkEventArgs e)
            => bgwCommand_DoWork((FolderIconCommandArguments)e.Argument);
        private void bgwCommand_DoWork(FolderIconCommandArguments arguments)
        {
            var parent = arguments.Directory.Parent;
            Roots.Refresh();
            switch (arguments.Command)
            {
                case FolderIconCommand.SetIconTree:
                case FolderIconCommand.SetIconsAuto:
                case FolderIconCommand.ApplyFolderIcons:
                    if (Toggles.ResetCaches)
                        ResetCachesImpl();
                    break;
            }
            switch (arguments.Command)
            {
                case FolderIconCommand.GenerateSidecarFiles:
                    var parentIcon = DesktopIniParser.GetIcon(parent.FullName);
                    GenerateSidecarFilesImpl(arguments.Directory, parentIcon, arguments.PreviewMode, arguments.Recursive);
                    break;
                case FolderIconCommand.SetIconTree:
                    SetIconTreeImpl(arguments);
                    break;
                case FolderIconCommand.SetIconsAuto:
                    SetIconAutoImpl(arguments);
                    break;
                case FolderIconCommand.FixAttributes:
                case FolderIconCommand.ApplyFolderIcons:
                    var parentInfo = FolderIconInfo.Get(parent.FullName);
                    ApplyFolderIconsImpl(arguments.Directory, parentInfo, arguments.PreviewMode, arguments.Command == FolderIconCommand.FixAttributes, arguments.Recursive);
                    break;
            }
        }


        #endregion Background Worker

        #region Reporting        
        //public void ReportResult(TResult result) { Result = result; }

        public void ReportStart()
            => Stopwatch.ReportStart();
        public void ReportStart(int maximum)
            => ReportStart(maximum, "Loading Directories...");
        public void ReportStart(int maximum, string title, bool reset=false)
            => Stopwatch.ReportStart(maximum, title, reset);

        public void ReportEnqueued(int additional)
            => Stopwatch.ReportEnqueued(additional);
        public void ReportProgress(ReportedStatus state)
            => ReportProgress(Stopwatch.GetProgress(1), state);

        void ReportError(string message)
            => ReportProgress(0, new ReportedStatus(message, foreground: "#FFDED3D3", background: "#FFFF3333"));

        void ReportStatus(ReportedStatus status)
            => ReportStatus(status.ToElement());
        void ReportStatus(ReportedStatusElement status)
        {
            InfoReporter.ReportStatus(status);
        }

        void ClearStatus()
            => InfoReporter.ListBox.Items.Clear();


        void ReportProgress(int progressPercentage, ReportedStatus message)
        {
            if (LibSetting.Toggles.Async)
            {
                _bgwCommand.ReportProgress(progressPercentage, message);
            }
            else
            {
                OnCommandWorkerProgressChanged(progressPercentage, message);
            }
        }
        #endregion Reporting
        #region Commands

        #region Commands: Properties
        public RelayCommand SaveSettings { get; set; }
        public RelayCommand ResetCaches { get; set; }
        public RelayCommand OpenDirectoryIconInBrowser { get; set; }
        public RelayCommand<string> OpenIconInBrowser { get; set; }
        public RelayCommand<string> OpenDirectoryInBrowser { get; set; }
        public RelayCommand<FavoritePath> OpenFavorite { get; set; }
        public RelayCommand<FavoritePath> EditFavorite { get; set; }
        public RelayCommand<FavoritePath> DeleteFavorite { get; set; }
        public RelayCommand<FavoritePathType?> AddFavorite { get; set; }
        public RelayCommand SaveIconToSidecar { get; set; }
        public RelayCommand ApplyFolderIcons { get; set; }

        public RelayCommand GenerateSidecarFiles { get; set; }

        public RelayCommand FixAttributes { get; set; }

        public RelayCommand SetIconTree { get; set; }

        public RelayCommand SetIconsAuto { get; set; }

        #endregion Commands: Properties
        #region Commands: Explore File


/*
        private void TextBlock_ExploreFile(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                var path = textBlock.DataContext as string;
                if (string.IsNullOrWhiteSpace(path))
                    Debugger.Break();
                else
                    FileSystemExplorerControl.StartExplorer(path, File.Exists(path));
            }
        }
*/

        #endregion Commands: Explore File

        #region Commands: Favorites
        private void AddFavoriteImpl(FavoritePathType? pathType)
        {
            if (!pathType.HasValue)
                return;
            var type = pathType.Value;
            ExplorerItem item;
            switch (type)
            {
                case FavoritePathType.Folders:
                    item = SelectedDirectory;
                    break;
                case FavoritePathType.Icons:
                    item = SelectedIcon;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            var iconPath = item.IconPath;
            if (iconPath == null)
                Debugger.Break();
            var favorite = new FavoritePath(item.Title, item.FullPath, type, iconPath);
            Settings.Directories.AddFavorite(favorite);
        }
        private void OpenFavoriteImpl(FavoritePath favorite)
        {
            var path = favorite.Path;
            var type = favorite.Type;
            RelayCommand<string> command;
            switch (type)
            {
                case FavoritePathType.Folders:
                    command = OpenDirectoryInBrowser;
                    break;
                case FavoritePathType.Icons:
                    command = OpenIconInBrowser;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            command.Execute(path);
        }
        private void EditFavoriteImpl(FavoritePath favorite)
        {
            var type = favorite.Type;
            var typeTitle = type.ToString().TrimEnd('s');
            var prompt = new MyPrompt($"Enter new name for {typeTitle} Favorite '{favorite.Path}':", $"Edit Favorite {typeTitle}", favorite.Name);
            if (!prompt)
                return;
            favorite.Name = prompt;
        }
        private void DeleteFavoriteImpl(FavoritePath favorite)
        {
            Settings.Directories.DeleteFavorite(favorite);
        }
        #endregion Commands: Favorites
        #region Commands: Single


        private void SaveIconToSidecarImpl()
        {
            var directory = SelectedDirectory;
            var icon = IconReference.FromResource(directory.FullPath, SelectedIcon.FullPath);
            var directoryInfo = new DirectoryInfo(directory.FullPath);
            //TODO: Simplify Access to FolderInfoFile Constructor
            var infoFile = new FolderInfoFile(directoryInfo, SidecarType.Main, null);
            var info = infoFile.Object;
            info.Icon.Main = icon.Resource;
            infoFile.Save();
            directory.Image.Result.Refresh();
            directory.OnPropertyChanged(nameof(directory.SidecarExists));
            directory.OnPropertyChanged(nameof(directory.Sidecar));
        }

        #endregion Commands: Single
        #region Commands: Folder Icon Commands

        #region Commands: Folder Icon Commands: Run
        async void RunCommand(FolderIconCommand command)
        {
            if (!MayRunCommand)
            {
                ReportError("Cannot start new job - one is already in progress");
                return;
            }
            Updates = 0;
            Roots.RefreshLabelsRegex();
            //Elapsed = System.Diagnostics.Stopwatch.StartNew();
            ClearStatus();
            var arguments = new FolderIconCommandArguments(command,
                SelectedDirectory?.FullPath, Settings);
            ReportStart(0, $"Loading Subdirectories of {arguments.Directory.FullName}", true);
            if (arguments.Recursive)
            {
                arguments.Directories = await Task.Run(() => arguments.Directories
                    .Concat(
                            arguments
                                .Directory
                                .EnumerateDirectories(EnumerationOptions.Recursive.Directories)
                    )
                    .OrderBy(x => x.FullName.ToLowerInvariant())
                    .ToArray()).ConfigureAwait(true);
                InfoReporter.ReportTiming($"Loaded {arguments.Directories.Length} Subdirectories of {arguments.Directory.FullName} in {Stopwatch.Elapsed.FormatFriendly()}");
                ReportStart(arguments.Directories.Length, "Loaded All Directories", true);
            }
                        
            if (Toggles.Async)
            {
                _bgwCommand.RunWorkerAsync(arguments);
                OnPropertyChanged(nameof(MayRunCommand));
            }
            else
            {
                MayRunCommand = false;
                bgwCommand_DoWork(arguments);
                OnCommandWorkerCompleted();
                MayRunCommand = true;

            }
        }



        #endregion Commands: Folder Icon Commands: Run

        #region Commands: Folder Icon Commands: Implementations

        void ApplyFolderIconsImpl(DirectoryInfo current, IconReference parentIcon, bool previewMode, bool fixAttributesOnly = false, bool recursive = true)
        {
            var iniFile = new DesktopIniParser(current.FullName);
            var icon = iniFile.Icon;
            var iconInfo = FolderIconInfo.Get(current.FullName);
            //IconReference finalIcon;
            //var isNew = GetCurrentIcon(current, icon, parentIcon, iconInfo, out finalIcon);
            if (parentIcon.IsEmpty)
            {
                var parentDirectory = current.IsRoot()
                                          ? current
                                          : current.Parent;
                parentIcon = new DesktopIniParser(parentDirectory.FullName).Icon;
            }
            var finalIcon = GetCurrentIconFromSidecar(current, iconInfo, parentIcon);

            if (!fixAttributesOnly && !finalIcon.IsEmpty && iniFile.Icon.Resource != finalIcon.Resource)
            {
                ReportProgress(0,
                               $"<<LINK:{finalIcon.Resource}||{finalIcon.Icon.FullName}::500>>\t==>\t<<LINK:{current.FullName}::800>> [<<LINK:{icon.Info}>>]");
                //ReportStatus($"Setting Main Icon To [LINK:{icon.Info}::500] for [LINK:{current.FullName}::800] [Parent: [LINK:{parentIcon.Info}]]");
                if (!previewMode)
                {
                    iniFile.IconResource = finalIcon;
                    iniFile.Save();
                }
            }
            else
            {
                var thisFile = iniFile.Ini;
                var verified = thisFile.VerifyHiddenSystem(!previewMode);
                verified &= thisFile.Directory.VerifySystem(!previewMode);
                verified &= FolderInfoFile.GetFile(thisFile.Directory.FullName, SidecarType.Main).VerifyHiddenSystem(!previewMode);
                if (!verified)
                    ReportProgress(0,
                                   $"*** Fixing desktop.ini attributes for <<LINK:{current.FullName}::1200>> [<<LINK:{icon.Info}>>]");
            }
            if (!recursive)
                return;
            foreach (var child in current.EnumerateDirectoriesSafe())
                ApplyFolderIconsImpl(child, finalIcon.IsEmpty ? parentIcon : finalIcon, previewMode, fixAttributesOnly);
        }

        void SetIconTreeImpl(FolderIconCommandArguments options)
            => SetIconTreeImpl(options.Directory, options);
        void SetIconTreeImpl(DirectoryInfo current, FolderIconCommandArguments options)
        {            
            if (!current.Exists)
            {
                if (options.CreateDirectories)                
                    current.Create();                
                else                
                    Debugger.Break();                
            }
            var ini = ResolveCurrentIcon(current);
            if (!ini.Ini.Exists)
                return;
            if (ini.Directory.FullName == current.FullName)
            {
                throw new InvalidOperationException($"Cannot set Icon Tree from self for {current.FullName}");
            }
            var icon = ini.Icon.FullName;
            //TODO: Copy Icon To Target

        }

        void SetIconAutoImpl(FolderIconCommandArguments options)
            => options
                .Directories
                .ForEach(directory => SetIconAutoSingleImpl(directory, options));

        void SetIconAutoImpl(DirectoryInfo current, FolderIconCommandArguments options)
        {

            SetIconAutoSingleImpl(current, options);

            if (!options.Recursive)
                return;
            foreach (var child in current.EnumerateDirectories(EnumerationOptions.Directories))
                SetIconAutoImpl(child, options);

        }
        // ReSharper disable once UnusedMethodReturnValue.Local
        bool SetIconAutoSingleImpl(DirectoryInfo current, FolderIconCommandArguments options)
        {
            if (!current.Exists && options.CreateDirectories)
                current.Create();
            var paths = GetRelativePaths(current, Roots.Action, 100).ToArray();
            var label = paths.FirstOrDefault()?.Directory.SubPath;            
            var altLabel = Roots.GetAlternateRootLabels(label).FirstOrDefault();            
            var labels = string.IsNullOrWhiteSpace(label) ? new string[0] : Paths.Split(label);
            var name = labels.FirstOrDefault(x => x.ToUpperInvariant() != x && x.Length > 4);
            var result = GetCurrentIcon(current);
            var icon = result.Icon;

            if (icon.IsEmpty || !icon.Exists)
            {
                ReportError($"Could not find Folder Icon for <<LINK:{current.FullName}::800>>");
                return false;
            }
            var newIcon = icon.ChangeDirectory(current);
            var ini = new DesktopIniParser(current);
            var oldIcon = ini.Icon;
            
            ini.IconResource = newIcon;
            var depth = current.GetDepth(options.Root);
            var verbosity = Math.Min(MAX_VERBOSITY - 1 , depth + NEW_ICON_VERBOSITY);
            if (options.LastIcon.FullName != icon.FullName)
                verbosity = Math.Min(verbosity, NEW_ICON_VERBOSITY);
            if (label?.IndexOf(Paths.DirectorySeparatorChar) == -1)
                verbosity = Math.Min(verbosity, MANUAL_VERBOSITY - 2);
            if (altLabel != null)
                verbosity = Math.Min(verbosity, MANUAL_VERBOSITY - 1);
            if (name == null || newIcon.FullName.Contains(name))
                verbosity = Math.Min(verbosity, MANUAL_VERBOSITY - 1);
            if (newIcon.FullName.Contains("..\\"))
                verbosity = Math.Min(verbosity, NEW_ICON_VERBOSITY + 1);
            if (Settings.Toggles.ReportChanges)
            {
                var oldIconPath = ResolvedPath.Resolve(oldIcon.FullName);
                var newIconPath = ResolvedPath.Resolve(newIcon.FullName);
                if (!oldIconPath.Resolved.Equals(newIconPath.Resolved, StringComparison.OrdinalIgnoreCase))
                    verbosity = Math.Min(verbosity, verbosity > NEW_ICON_VERBOSITY ? 2 : 1);
            }
            var message = $"{ReportedStatusElement.GetLink(newIcon.FullName, newIcon.Resource, 700, 115)}\t==>\t{ReportedStatusElement.GetLink(current.FullName, width: 700)} [{ReportedStatusElement.GetLink(icon.Info)}]";
            
            ReportProgress(0, new ReportedStatus(message, options.Command, current.FullName, newIcon.Resource, verbosity: verbosity));
            var success = options.PreviewMode || ini.Save();
            if (!success)
            {
                ReportError($"Unable to save INI for {current.FullName}");
            }
            options.LastIcon = icon;
            return true;

        }
        void GenerateSidecarFilesImpl(DirectoryInfo current, IconReference parentIcon, bool previewMode, bool recursive = true)
        {
            var icon = DesktopIniParser.GetIcon(current.FullName);
            //TODO: Simplify Access to FolderInfoFile Constructor
            var infoFile = new FolderInfoFile(current, SidecarType.Main, null);
            var info = infoFile.Object;
            //var iconInfo = infoFile.Object.Icon;
            //IconReference finalIcon;
            //var isNew = GetCurrentIconFromIni(current, icon, parentIcon, iconInfo, out finalIcon);
            var finalIcon = icon;
            var isNew = (icon.Resource != parentIcon.Resource && !icon.IsEmpty);

            if (isNew)
            {
                ReportProgress(0, $"Setting Main Icon To <<LINK:{finalIcon.Info}::500>> for <<LINK:{current.FullName}::800>> [<<LINK:{parentIcon.Info}>>]");
                //ReportStatus($"Setting Main Icon To [LINK:{icon.Info}::500] for [LINK:{current.FullName}::800] [Parent: [LINK:{parentIcon.Info}]]");
                if (!previewMode)
                {
                    info.Icon.Main = finalIcon.Resource;
                    infoFile.Save();
                }
            }
            if (!recursive)
                return;
            foreach (var child in current.EnumerateDirectoriesSafe())
                GenerateSidecarFilesImpl(child, finalIcon.IsEmpty ? parentIcon : finalIcon, previewMode);
        }
        #endregion Commands: Folder Icon Commands: Implementations
        #endregion Commands: Folder Icon Commands


        #endregion Commands

        #region Helpers


        #region Helpers: Get Current Icon

/*
        bool GetCurrentIconFromIni(DirectoryInfo current, IconReference currentIcon, IconReference parentIcon, FolderIconInfo iconInfo, out IconReference finalIcon)
        {
            if (currentIcon.Resource != parentIcon.Resource && !currentIcon.IsEmpty)
            {
                finalIcon = currentIcon;
                return true;
            }
            //TODO: Evaluate use of FolderIconInfo.Main vs FolderIconInfo.Icon/GetIconResource()
            if (!string.IsNullOrEmpty(iconInfo.Main))
            {
                finalIcon = currentIcon;
                return false;
            }
            finalIcon = GetCurrentIconFromDirectory(current, iconInfo, parentIcon);
            if (finalIcon.IsEmpty)
            {
                finalIcon = currentIcon;
                return false;
            }
            return true;
        }
*/

        IEnumerable<IconGenerationSource> GetRootPathsGenerator(DirectoryInfo current, IconGenerationRoot root)
        {
            yield return new IconGenerationSource(root, 5, Roots.Icons);
            yield return new IconGenerationSource(root, 2, Roots.Content);
            yield return new IconGenerationSource(root, 4, Roots.GetActionRoot(current));
            yield return new IconGenerationSource(root, 3, Roots.Action);
            yield return new IconGenerationSource(root, 1, current.GetExistingPath());
        }

        IEnumerable<IconGenerationSource> GetRootPaths(DirectoryInfo current, IconGenerationRoot root)
            => GetRootPathsGenerator(current, root).Where(x=>x.Directory != null).Distinct();
        IEnumerable<IconGenerationPath> GetRelativePaths(DirectoryInfo current, DirectoryInfo rootDirectory, int priority)
        {
            //var debug = IsDebug(current);
            var generationRoot = new IconGenerationRoot(priority, rootDirectory);

            //TODO: xRemove ToArray()
            var roots = GetRootPaths(current, generationRoot)
                .Distinct()
                .ToArray()
                ;
            RelativePath relative=null;

            foreach (var root in roots)
            {
                if (!Roots.IsActionRoot(current))
                {
                    var actionRoot = Roots.GetActionRoot(current);
                    relative = current.ParseRelativePath(actionRoot, root.Directory);
                    yield return new IconGenerationPath(root, 1, relative);
                }
                if (Roots.Action != null)
                {
                    relative = current.ParseRelativePath(Roots.Action, root.Directory);
                    yield return new IconGenerationPath(root, 2, relative);
                }
            }
            if (relative == null)
                yield break;
            var label = relative.SubPath;

            var newLabels = Roots.GetAlternateRootLabels(label);
                //.ToArray()
                
            foreach (var newLabel in newLabels)
                foreach (var root in roots)
                {
                    relative = root.Directory.CreateRelativePath(newLabel);
                    yield return new IconGenerationPath(root, 3, relative);
                }
        }

        DesktopIniParser ResolveCurrentIcon(DirectoryInfo current)
            => GetCurrentIconFromRoots(current, 100) ?? GetCurrentIconFromDirectoryTree(current);

        bool IsDebug(DirectoryInfo current)
            => false;// current?.FullName?.Contains("DEBUG_OFF") ?? false;
        IEnumerable<IconGenerationFolder> GetCurrentIconFoldersFromDirectory(FolderTraversalHistory folders, int priority)
        {
            //var debug = IsDebug(folders);
            var depth = 0;
            while (folders.Up())
            {
                depth++;
                var paths = GetRelativePaths(folders, folders.Original, priority);
                    //.ToArray();
                //var strPaths = paths
                //    //.Where(p=>Paths.Exists(p.FullName))
                //    .Select(p => p.Directory.FullName).ToArray();
                foreach (var path in paths)
                    yield return new IconGenerationFolder(path, depth, folders);
                //if (folders.IsRoot()) break;
            }
        }

        [Obsolete]
        IEnumerable<DirectoryInfo> GetRootFoldersFromDirectory(DirectoryInfo current)
        {
            var folderIconInfo = FolderIconInfo.GetExisting(current);
            return GetRootFoldersFromDirectory(folderIconInfo);
        }

        IEnumerable<DirectoryInfo> GetRootFoldersFromDirectory(FolderIconInfo iconInfo)
        {
            //var debug = IsDebug(iconInfo);
            //var allRoots = iconInfo.GetRoots();
            //var folderIconInfo = FolderIconInfo.GetExisting(iconInfo);


            var iconRoots = iconInfo
                .GetRoots()
                .Select(x => DirectoriesIO.GetInfo(Directories.Roots.Library, x));
            var roots = new [] { iconInfo.Current }
                .Concat(iconRoots);
                //.ToArray()                
            //var strPaths = roots
            //    //.Where(p=>Paths.Exists(p.FullName))
            //    .Select(p => p.FullName)
            //    .ToArray();
            return roots;
        }
        private IEnumerable<IconGenerationFolder> GetCurrentIconFoldersFromRoots(DirectoryInfo[] roots)
        {
            var total = roots.Length;
            var paths = roots
                .SelectMany((x,index) => GetCurrentIconFoldersFromDirectory(x, total - index))
                .Distinct();
                //.ToArray();
            //var strPaths = paths
            //    //.Where(p=>Paths.Exists(p.FullName))
            //    .Select(p => p.FullName)
            //    .ToArray();

            return paths;
        }
        
        [Obsolete]
        IEnumerable<IconGenerationFolder> GetCurrentIconFoldersFromName(DirectoryInfo current)
        {
            //var debug = IsDebug(current);
            
            var roots = GetRootFoldersFromDirectory(current).DistinctPaths().ToArray();
            //var strRoots = roots
            //    //.Where(p=>Paths.Exists(p.FullName))
            //    .Select(p => p.FullName)
            //    .ToArray();

            var paths = GetCurrentIconFoldersFromRoots(roots);
            return paths;
        }

        IEnumerable<IconGenerationFolder> GetCurrentIconFoldersFromName(FolderIconInfo iconInfo)
        {
            var roots = GetRootFoldersFromDirectory(iconInfo).DistinctPaths().ToArray();
            var paths = GetCurrentIconFoldersFromRoots(roots);
            return paths;
        }


        [Timing(MINIMUM_TIMING_INTERVAL)]
        private static IEnumerable<IconGenerationResult> GetAllCurrentIconsFromPaths(DirectoryInfo current,
                                                                              IconGenerationFolder[] paths)
            => from path in paths
               from extension in IconExtensions
               from kvp in IconNameTemplates
               let names = kvp.Value.Generate(extension, current.Name, path.Path.Directory.Name)
               where names.Valid
               from name in names
               let icon = IconReferenceCache.Get(path.Path.Directory.FullName, name)
               select new IconGenerationResult(path, kvp.Key, icon);

/*
        [Obsolete]
        IEnumerable<IconGenerationResult> GetAllCurrentIconsFromName(DirectoryInfo current)
        {
            var paths = GetCurrentIconFoldersFromName(current).Distinct().ToArray();
            //var strPaths = paths
            //    //.Where(p=>Paths.Exists(p.FullName))
            //    .Select(p => p.Directory.FullName).ToArray();
            return GetAllCurrentIconsFromPaths(current, paths);
        }
*/

        IEnumerable<IconGenerationResult> GetAllCurrentIconsFromName(FolderIconInfo iconInfo)
        {
            var paths = GetCurrentIconFoldersFromName(iconInfo).Distinct().ToArray();
            return GetAllCurrentIconsFromPaths(iconInfo.Current, paths);
        }
        [UsedImplicitly]
        [SuppressMessage("ReSharper", "UnusedVariable")]
        IEnumerable<IconGenerationResult> GetCurrentIconsFromName_DebugSteps(DirectoryInfo current)
        {
            //var debug = IsDebug(current);
            //TODO: xRemove folderIconInfo
            var folderIconInfo = FolderIconInfo.GetExisting(current);

            var sidecarIcon = ResolveCurrentSidecarIcon(folderIconInfo, current);
            if (sidecarIcon != null)
            {
                return new[] { sidecarIcon };
            }

            //TODO: xRemove roots and strRoots
            var roots = GetRootFoldersFromDirectory(folderIconInfo)
                .DistinctPaths()
                .ToArray();
            var strRoots = roots
                //.Where(p=>Paths.Exists(p.FullName))
                .OrderBy(p => p.FullName)
                .Select(p => p.FullName)
                .ToArray();

            //TODO: xRemove paths and strPaths
            var allPaths = GetCurrentIconFoldersFromRoots(roots)
                .OrderBy(x => x.Priorities, ArrayComparer<double>.Default)
                .ThenBy(x => x.Directory.FullName)
                .ToArray();
            var paths = allPaths
                .Distinct()
                .ToArray();
            var strPaths = paths
                .Select(p => p.Directory.FullName)
                .ToArray();

            //TODO: xReplace GetAllCurrentIconsFromPaths(current, paths) and above code with GetAllCurrentIconsFromName(current)
            //TODO: xRemove ToArray()
            var icons = GetAllCurrentIconsFromPaths(current, paths)
                .OrderBy(x => x.Priorities, ArrayComparer<double>.Default)
                .ThenBy(x => x.Directory.FullName)
                .Distinct()
                .ToArray();

            //TODO: xRemove ToArray()
            var existingIcons = icons
                .Where(x => x.Icon.Exists)
                .ToArray();
            return existingIcons;
        }
        IEnumerable<IconGenerationResult> GetCurrentIconsFromName(DirectoryInfo current)
        {
            //var debug = IsDebug(current);
            var folderIconInfo = FolderIconInfo.GetExisting(current);

            var sidecarIcon = ResolveCurrentSidecarIcon(folderIconInfo, current);
            if (sidecarIcon != null)
                return new[] {sidecarIcon};

            return GetAllCurrentIconsFromName(folderIconInfo)
                .OrderBy(x=>x.Priorities, ArrayComparer<double>.Default)
                .ThenBy(x => x.Directory.FullName)
                .Distinct()
                .Where(x => x.Icon.Exists);
        }
        IconGenerationResult ResolveCurrentSidecarIcon(FolderIconInfo iconInfo, DirectoryInfo current)
        {
            var icon = iconInfo.GetIconReference(current);
            return GetIconGenerationResultFromReference(icon, -100, checkIconExists:true);
        }
        IconGenerationResult ResolveCurrentGeneratedIcon(DirectoryInfo current)
        {
            var ini = ResolveCurrentIcon(current);
            if (ini == null)
                return null;
            var icon = ini.Icon;
            var directory = ini.Directory;
            return GetIconGenerationResultFromReference(icon, 100, directory);
        }

        private static IconGenerationResult GetIconGenerationResultFromReference(IconReference icon, double priority = 100, DirectoryInfo directory = null, bool checkIconExists = false)
        {
            if (!icon.Validate(checkIconExists))
                return null;
            //var debug = string.IsNullOrWhiteSpace(icon.BaseDirectory);
            var baseDirectory = string.IsNullOrWhiteSpace(icon.BaseDirectory)
                ? directory
                : DirectoriesIO.GetInfo(icon.BaseDirectory);
            if (directory == null)
            {
                directory = baseDirectory;
            }
            var root = new IconGenerationRoot(priority, baseDirectory);
            var source = new IconGenerationSource(root, root.Priority, baseDirectory);
            var relative = directory.ParseRelativePath(root.Directory);
            var path = new IconGenerationPath(source, root.Priority, relative);
            var folder = new IconGenerationFolder(path, root.Priority, relative.Directory);
            return new IconGenerationResult(folder, root.Priority, icon);
        }

/*
        IconGenerationResult GetCurrentIconFromName(DirectoryInfo current)
            => GetCurrentIconsFromName(current).FirstOrDefault();
*/
        IconGenerationResult GetCurrentIcon(DirectoryInfo current)
        {
            //var debug = IsDebug(current);

            var icons = GetCurrentIconsFromName(current).ToList();
            var hasForcedIcon = icons.Any(x => x.Priorities.All(y => y <= -100));
            if (!hasForcedIcon)
            {
                var icon = ResolveCurrentGeneratedIcon(current);
                if (icon != null && !icons.Contains(icon))
                    icons.Add(icon);
            }

            var finalIcons = icons
                .Where(x => x.Icon.Exists)
                .OrderBy(x => x.Priorities, ArrayComparer<double>.Default)
                .ThenByDescending(x => x.Icon.Icon.GetDepth())
                //.ToArray()
                ;
            return finalIcons.FirstOrDefault() ?? IconGenerationResult.Empty;
            //if (!finalIcons.Any())
            //    return IconGenerationResult.Empty;
            //var longest = finalIcons.FirstOrDefault();
            ////var name = longest?.Icon.FullName;
            //return longest;
        }
        DesktopIniParser GetCurrentIconFromRoots(DirectoryInfo current, int priority)
        {
            var root = current;
            while (true)
            {
                var paths = GetRelativePaths(current, root, priority);
                foreach (var path in paths)
                {
                    var label = path.Directory.SubPath;
                    if (string.IsNullOrWhiteSpace(label))
                        continue;
                    var ini = new DesktopIniParser(Paths.Combine(path.Directory.FullName, "desktop.ini"));
                    if (ini.Ini.Exists)
                        return ini;
                }
                if (current.IsRoot())
                    break;
                current = current.Parent;
            }

            //var roots = new[] { Roots.Icons, Roots.Content, Roots.Action, current.GetExistingPath() };
            //var relative = current.ParseRelativePath(this.Roots.Action);
            //label = relative.SubPath;
            //while (true)
            //{
            //    if (string.IsNullOrWhiteSpace(label))
            //        break;
            //    foreach (var root in roots)
            //    {
            //        if (root?.Exists != true)
            //            continue;
            //        var ini = new DesktopIniParser(Paths.Combine(root.FullName, label));
            //        if (ini.Ini.Exists)                    
            //            return ini;                    
            //    }
            //    label = Paths.GetDirectoryName(label);
            //}
            return null;
        }

        DesktopIniParser GetCurrentIconFromDirectoryTree(DirectoryInfo current)
        {
            //var relative = current.CreateRelativePath(Roots.GetActionRoot(current).FullName);
            //var label = relative.SubPath;
            while (true)
            {
                var ini = new DesktopIniParser(current);
                if (ini.Ini.Exists)
                    return ini;
                if (current.IsRoot())
                    return null;
                current = current.Parent;
            }
        }
        IconReference GetCurrentIconFromSidecar(DirectoryInfo current, FolderIconInfo iconInfo, IconReference parentIcon)
        {
            //TODO: Evaluate use of FolderIconInfo.Main vs FolderIconInfo.Icon/GetIconResource()
            if (!string.IsNullOrEmpty(iconInfo.Main))
                return iconInfo;

            var finalIcon = GetCurrentIconFromDirectory(current, iconInfo, parentIcon);
            return finalIcon.IsEmpty
                ? parentIcon
                : finalIcon;
        }
        static IconReference GetCurrentIconFromDirectory(DirectoryInfo current, FolderIconInfo iconInfo, IconReference parentIcon)
        {
            var baseDirectory = Path.Combine(LibSetting.Directories.Library, current.GetFullNameWithoutRoot());
            var testDirectories = new[]
            {
                current.FullName,
                //Path.Combine(baseDirectory, current.Name),
                baseDirectory
            };

            var testFileNames = new[] { current.Name, Path.Combine(current.Name, current.Name), "", "current" };

            var roots = iconInfo
                .GetRoots()
                .Select(x => Path.Combine(LibSetting.Directories.Library, x))
                .ToArray();

            foreach (var name in testFileNames)
                foreach (var directory in roots.Concat(testDirectories))
                    foreach (var extension in IconExtensions)
                    {
                        var testPath = string.IsNullOrEmpty(name)
                            ? $"{directory}{name}.{extension}"
                            : Path.Combine(directory, $"{name}.{extension}");
                        var testFile = new FileInfo(testPath);
                        var message = current.FullName.Suffix(": ").PadRight(50) + testFile.FullName;

                        if (!testFile.Exists || testFile.FullName == parentIcon.Icon?.FullName)
                            continue;
                        Debug.WriteLine("*** " + message);
                        return IconReference.FromFile(testFile);

                        // Debug.WriteLine("    " + message);
                    }
            Debug.WriteLine("xxx " + current.FullName.Suffix(": ").PadRight(50) + parentIcon.Icon?.FullName);
            return IconReference.Empty;
        }

        #endregion Helpers: Get Current Icon

        #endregion Helpers
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}