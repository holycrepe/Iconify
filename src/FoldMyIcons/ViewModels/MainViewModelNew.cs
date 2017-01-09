namespace FoldMyIcons.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.OleDb;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using Alphaleonis.Win32.Filesystem;
    using Commands;
    using Folders;
    using JetBrains.Annotations;
    using PostSharp.Patterns.Model;
    using Properties.Settings.AllSettings;
    using Properties.Settings.LibSettings;
    using Puchalapalli.Extensions.Collections;
    using Puchalapalli.Extensions.DateTime;
    using Puchalapalli.Extensions.Primitives;
    using Puchalapalli.Infrastructure.InfoReporters;
    using Puchalapalli.Infrastructure.Media.Icons;
    using Puchalapalli.Infrastructure.Media.Icons.Folders;
    using Puchalapalli.IO;
    using Puchalapalli.IO.AlphaFS.Extensions;
    using Puchalapalli.IO.AlphaFS.Info;
    using Puchalapalli.IO.Folders;
    using Puchalapalli.Utilities.Threading.Parallel;
    using Puchalapalli.WPF.Controls.FileSystemExplorer;
    using Puchalapalli.WPF.Infrastructure.InfoReporters;
    using Puchalapalli.WPF.Interactivity.Commands;
    using Puchalapalli.WPF.Styles.Extensions;
    using Telerik.Windows.Controls;
    using static Properties.Settings.LibSettings.LibSettings;
    using DirectoriesIO = Puchalapalli.IO.AlphaFS.Info.Directories;

    [NotifyPropertyChanged]
    public class MainViewModelNew : INotifyPropertyChanged
    {
        public const double MAX_VERBOSITY = 10;
        const int MANUAL_VERBOSITY = 6;
        const int MAX_DEGREE_OF_PARALLELISM = 12;
        static readonly string[] IconNameTemplates = new[]
        {
            @"{0}.icl",
            @"{0}.ico",

            @"{0}\{0}.icl",
            @"{0}\{0}.ico",

            @"..\{0}.icl",
            @"..\{0}.ico",

            @"{1}.icl",
            @"{1}.ico",

            @"{1}\{1}.icl",
            @"{1}\{1}.ico",

            @"..\{1}.icl",
            @"..\{1}.ico",

            @"folder.icl",
            @"folder.ico"
        };

        public WorkerStopwatch Stopwatch { get; } = new WorkerStopwatch();
        public double Total { get; set; } = 100;
        public double Progress { get; set; } = 50;
        public bool IsActive { get; set; } = true;

        #region Infrastructure

        private readonly BackgroundWorker bgwCommand;


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
                    ? bgwCommand?.IsBusy == false
                    : _mayRunCommand;
            }
            set
            {
                _mayRunCommand = value;
                OnPropertyChanged();
            }
        }

        public int Updates { get; set; } = 0;
        public Stopwatch Elapsed { get; set; }
        public ExplorerItem SelectedDirectory { get; set; }
        public ExplorerItem SelectedIcon { get; set; }
        #endregion State

        public IconRootInfos Roots { get; set; } = new IconRootInfos();
        public static MainViewModel Current { get; set; }

        public MainViewModelNew()
        {
            Current = this;
            bgwCommand = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            bgwCommand.DoWork += bgwCommand_DoWork;
            bgwCommand.ProgressChanged += bgwCommand_ProgressChanged;
            bgwCommand.RunWorkerCompleted += bgwCommand_RunWorkerCompleted;
            OnPropertyChanged(nameof(MayRunCommand));
        }

        public void Initialize(ListBoxInfoReporter infoReporter)
        {
            InfoReporter = infoReporter;
            SaveSettings = new RelayCommand(() => SaveSettingsImpl(false));
            SetIconTree = new RelayCommand(() => RunCommand(FolderIconCommand.SetIconTree), () => this.MayRunCommand);
            SetIconsAuto = new RelayCommand(() => RunCommand(FolderIconCommand.SetIconsAuto), () => this.MayRunCommand);
            ApplyFolderIcons = new RelayCommand(() => RunCommand(FolderIconCommand.ApplyFolderIcons), () => this.MayRunCommand);
            GenerateSidecarFiles = new RelayCommand(() => RunCommand(FolderIconCommand.GenerateSidecarFiles), () => this.MayRunCommand);
            FixAttributes = new RelayCommand(() => RunCommand(FolderIconCommand.FixAttributes), () => this.MayRunCommand);
            SaveIconToSidecar = new RelayCommand(SaveIconToSidecarImpl, () => this.MayRunCommand);
            OnPropertyChanged(nameof(MayRunCommand));
        }

        public void SaveSettingsImpl(bool includeWindowPlacement=false)
        {
            Directories.DirectoryExplorer = SelectedDirectory?.HierarchyPath;
            Directories.IconExplorer = SelectedIcon?.HierarchyPath;
            AllSettings.SaveAllSettings(includeWindowPlacement);
        }
        #region Background Worker


        private void bgwCommand_RunWorkerCompleted(object sender = null, RunWorkerCompletedEventArgs e = null)
        {
            //var bgw = (BackgroundWorker)sender;
            //var result = e.Result;
            OnPropertyChanged(nameof(MayRunCommand));
            Elapsed.Stop();
            ReportStatus(new ReportedStatus($"Operation Complete: {Updates} Updates Made in {Elapsed.FormatFriendly()}", foreground: "#FFDED3D3", background: "#FF007ACC"));
            //throw new NotImplementedException();
        }

        private void bgwCommand_ProgressChanged(object sender, ProgressChangedEventArgs e)
            => bgwCommand_ProgressChanged(e.ProgressPercentage, (ReportedStatus)e.UserState);

        private void bgwCommand_ProgressChanged(int progressPercentage, ReportedStatus status)
        {
            Updates++;
            Stopwatch.ReportProgress(status.Status, status.Title, status.Text, Updates);
            if (this.Settings.Verbosity >= status.Verbosity)
                ReportStatus(status);
        }
        private void bgwCommand_DoWork(object sender, DoWorkEventArgs e)
            => bgwCommand_DoWork((FolderIconCommandArguments)e.Argument);
        private void bgwCommand_DoWork(FolderIconCommandArguments arguments)
        {
            var parent = arguments.Directory.Parent;

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

        Brush HexToBrush(string hex)
            => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

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
                bgwCommand.ReportProgress(progressPercentage, message);
            }
            else
            {
                bgwCommand_ProgressChanged(progressPercentage, message);
            }
        }
        #endregion Reporting
        #region Commands

        #region Commands: Properties
        public RelayCommand SaveSettings { get; set; }
        public RelayCommand OpenDirectoryIconInBrowser { get; set; }
        public RelayCommand<string> OpenIconInBrowser { get; set; }
        public RelayCommand<string> OpenDirectoryInBrowser { get; set; }

        public RelayCommand SaveIconToSidecar { get; set; }
        public RelayCommand ApplyFolderIcons { get; set; }

        public RelayCommand GenerateSidecarFiles { get; set; }

        public RelayCommand FixAttributes { get; set; }

        public RelayCommand SetIconTree { get; set; }

        public RelayCommand SetIconsAuto { get; set; }

        #endregion Commands: Properties
        #region Commands: Explore File


        private void TextBlock_ExploreFile(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock != null)
            {
                var path = textBlock.DataContext as string;
                if (string.IsNullOrWhiteSpace(path))
                    Debugger.Break();
                else
                    FileSystemExplorerControl.StartExplorer(path, File.Exists(path));
            }
        }

        #endregion Commands: Explore File

        #region Commands: Single


        private void SaveIconToSidecarImpl()
        {
            var directory = SelectedDirectory;
            var icon = IconReference.FromResource(directory.FullPath, SelectedIcon.FullPath);
            var directoryInfo = new DirectoryInfo(directory.FullPath);
            var infoFile = new FolderInfoFile(directoryInfo);
            var info = infoFile.Object;
            info.Icon.Main = icon.Resource;
            infoFile.Save();
            directory.Image.Refresh();
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
            Elapsed = System.Diagnostics.Stopwatch.StartNew();
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
                bgwCommand.RunWorkerAsync(arguments);
                OnPropertyChanged(nameof(MayRunCommand));
            }
            else
            {
                MayRunCommand = false;
                bgwCommand_DoWork(arguments);
                bgwCommand_RunWorkerCompleted();
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
                               $"Setting Folder Icon To <<LINK:{finalIcon.Resource}||{finalIcon.Icon.FullName}::500>> for <<LINK:{current.FullName}::800>> [<<LINK:{icon.Info}>>]");
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
                verified &= FolderInfoFile.GetFile(thisFile.Directory.FullName).VerifyHiddenSystem(!previewMode);
                if (!verified)
                    ReportProgress(0, $"*** Fixing desktop.ini attributes for <<LINK:{current.FullName}::1200>> [<<LINK:{icon.Info}>>]");
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
        bool SetIconAutoSingleImpl(DirectoryInfo current, FolderIconCommandArguments options)
        {
            if (!current.Exists && options.CreateDirectories)
                current.Create();
            var paths = GetRelativePaths(current).ToArray();
            var label = paths.FirstOrDefault()?.SubPath;            
            var altLabel = Roots.GetAlternateRootLabels(label).FirstOrDefault();            
            var labels = string.IsNullOrWhiteSpace(label) ? new string[0] : Paths.Split(label);
            var name = labels.FirstOrDefault(x => x.ToUpperInvariant() != x && x.Length > 4);
            var icon = GetCurrentIcon(current);

            if (icon.IsEmpty || !icon.Exists)
            {
                ReportError($"Could not find Folder Icon for <<LINK:{current.FullName}::800>>");
                return false;
            }
            var newIcon = icon.ChangeDirectory(current);
            var ini = new DesktopIniParser(current) {IconResource = newIcon};
            var depth = current.GetDepth(options.Root);
            var verbosity = Math.Min(MAX_VERBOSITY - 1 , depth + 1);
            if (options.LastIcon.FullName != icon.FullName)
                verbosity = Math.Min(verbosity, 1);
            if (label?.IndexOf(Paths.DirectorySeparatorChar) == -1)
                verbosity = Math.Min(verbosity, MANUAL_VERBOSITY - 2);
            if (altLabel != null)
                verbosity = Math.Min(verbosity, MANUAL_VERBOSITY - 1);
            if (name == null || newIcon.FullName.Contains(name))
                verbosity = Math.Min(verbosity, MANUAL_VERBOSITY - 1);
            if (newIcon.FullName.Contains("..\\"))
                verbosity = Math.Min(verbosity, 2);
            ReportProgress(0, new ReportedStatus($"Setting Folder Icon To <<LINK:{newIcon.Resource}||{newIcon.FullName}::500>> for <<LINK:{current.FullName}::800>> [<<LINK:{icon.Info}>>]", options.Command, current.FullName, newIcon.Resource, verbosity: verbosity));
            var success = ini.Save();
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
            var infoFile = new FolderInfoFile(current);
            var info = infoFile.Object;
            var iconInfo = infoFile.Object.Icon;
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

        bool GetCurrentIconFromIni(DirectoryInfo current, IconReference currentIcon, IconReference parentIcon, FolderIconInfo iconInfo, out IconReference finalIcon)
        {
            if (currentIcon.Resource != parentIcon.Resource && !currentIcon.IsEmpty)
            {
                finalIcon = currentIcon;
                return true;
            }
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

        IEnumerable<DirectoryInfo> _getRootPaths(DirectoryInfo current)
        {
            yield return Roots.Icons;
            yield return Roots.Content;
            yield return Roots.GetActionRoot(current);
            yield return Roots.Action;
            yield return current.GetExistingPath();
        }

        IEnumerable<DirectoryInfo> GetRootPaths(DirectoryInfo current)
            => _getRootPaths(current).NotNull().Distinct();
        IEnumerable<RelativePath> GetRelativePaths(DirectoryInfo current)
        {
            //TODO: Remove ToArray()
            var roots = GetRootPaths(current)
                .NotNull()
                .DistinctPaths()
                .ToArray();
            RelativePath relative=null;
            var debug = IsDebug(current);
            foreach (var root in roots)
            {
                if (!Roots.IsActionRoot(current))
                {
                    relative = current.ParseRelativePath(Roots.GetActionRoot(current), root);
                    yield return relative;
                }
                relative = current.ParseRelativePath(Roots.Action, root);
                yield return relative;
            }
            if (relative == null)
                yield break;
            var label = relative.SubPath;
            //TODO: Remove ToArray()
            var newLabels = Roots.GetAlternateRootLabels(label).ToArray();
            foreach (var newLabel in newLabels)
                foreach (var root in roots)
                {
                    relative = root.CreateRelativePath(newLabel);
                    yield return relative;
                }
        }

        DesktopIniParser ResolveCurrentIcon(DirectoryInfo current)
            => GetCurrentIconFromRoots(current) ?? GetCurrentIconFromDirectoryTree(current);

        IEnumerable<RelativePath> GetCurrentIconFoldersFromDirectory(FolderTraversalHistory folders)
        {
            var debug = IsDebug(folders);
            while (folders.Up())
            {
                //TODO: Remove ToArray() and strPaths
                var paths = GetRelativePaths(folders).ToArray();
                var strPaths = paths
                    //.Where(p=>Paths.Exists(p.FullName))
                    .Select(p => p.FullName).ToArray();
                foreach (var path in paths)
                    yield return path;
                //if (folders.IsRoot()) break;
            }
        }

        IEnumerable<DirectoryInfo> GetRootFoldersFromDirectory(DirectoryInfo current)
        {
            var folderIconInfo = FolderIconInfo.GetExisting(current);
            return GetRootFoldersFromDirectory(folderIconInfo);
        }

        IEnumerable<DirectoryInfo> GetRootFoldersFromDirectory(FolderIconInfo iconInfo)
        {
            var allRoots = iconInfo.GetRoots();
            var folderIconInfo = FolderIconInfo.Get(iconInfo);

            //TODO: Remove ToArray() and strPaths
            var roots = new [] { iconInfo.Directory }
                .Concat(iconInfo
                .GetRoots()
                .Select(x => DirectoriesIO.GetInfo(Directories.Roots.Library, x)))                
                .ToArray();
            var strPaths = roots
                //.Where(p=>Paths.Exists(p.FullName))
                .Select(p => p.FullName)
                .ToArray();
            return roots;
        }

        IEnumerable<RelativePath> GetCurrentIconFoldersFromName(DirectoryInfo current)
        {
            //TODO: Remove ToArray() and strRoots
            var roots = GetRootFoldersFromDirectory(current)
                .DistinctPaths()
                .ToArray();
            var strRoots = roots
                //.Where(p=>Paths.Exists(p.FullName))
                .Select(p => p.FullName)
                .ToArray();

            //TODO: Remove ToArray() and strPaths
            var paths = roots
                .SelectMany(x => GetCurrentIconFoldersFromDirectory(x))
                .DistinctPaths()
                .ToArray();
            var strPaths = paths
                //.Where(p=>Paths.Exists(p.FullName))
                .Select(p => p.FullName)
                .ToArray();

            return paths;
        }
        IEnumerable<IconReference> GetAllCurrentIconsFromName(DirectoryInfo current)
        {
            
            //TODO: Remove ToArray() and strPaths
            var paths = GetCurrentIconFoldersFromName(current)
                .DistinctPaths()
                .ToArray();
            var strPaths = paths
                //.Where(p=>Paths.Exists(p.FullName))
                .Select(p => p.FullName).ToArray();
            foreach (var path in paths)
                foreach (var name in IconNameTemplates)
                {
                    var iconName = string.Format(name, path.Name, current.Name);
                    //var iconName1 = Paths.ResolveRelativePath(iconName, path.FullName);
                    //var iconName2 = Path.Combine(iconName);
                    yield return new IconReference(path.FullName, iconName);
                }
        }

        IEnumerable<IconReference> GetCurrentIconsFromName(DirectoryInfo current)
        {
            //TODO: Remove paths and strPaths
            var paths = GetCurrentIconFoldersFromName(current)
                .DistinctPaths()
                .ToArray();
            var strPaths = paths
                //.Where(p=>Paths.Exists(p.FullName))
                .Select(p => p.FullName).ToArray();
            
            //TODO: Remove ToArray()
            var icons = GetAllCurrentIconsFromName(current)
                .DistinctPaths()
                .ToArray();
            
            //TODO: Remove ToArray()
            var existingIcons = icons.Where(x => x.Exists).ToArray();
            return existingIcons;
        }

        IconReference GetCurrentIconFromName(DirectoryInfo current)
            => GetCurrentIconsFromName(current).FirstOrDefault();
        IconReference GetCurrentIcon(DirectoryInfo current)
        {
            var icons = GetCurrentIconsFromName(current).ToList();
            var ini = ResolveCurrentIcon(current);
            if (ini != null && !icons.ContainsPath(ini.Icon))
                icons.Add(ini.Icon);
            //TODO: Remove ToArray()
            var finalIcons =icons.Where(x => x.Exists)
                .OrderByDescending(x => x.Icon.GetDepth())
                .ToArray();
            if (!finalIcons.Any())
                return IconReference.Empty;
            var longest = finalIcons.FirstOrDefault();
            var name = longest?.FullName;
            return longest;
        }
        DesktopIniParser GetCurrentIconFromRoots(DirectoryInfo current)
        {
            string label;
            while (true)
            {
                var paths = GetRelativePaths(current);
                foreach (var path in paths)
                {
                    label = path.SubPath;
                    if (string.IsNullOrWhiteSpace(label))
                        continue;
                    var ini = new DesktopIniParser(Paths.Combine(path.FullName, "desktop.ini"));
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
            var relative = current.CreateRelativePath(this.Roots.GetActionRoot(current).FullName);
            var label = relative.SubPath;
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
            var testFileExtensions = new[] { ".ico", ".icl" };

            var roots = iconInfo
                .GetRoots()
                .Select(x => Path.Combine(LibSetting.Directories.Library, x))
                .ToArray();

            foreach (var name in testFileNames)
                foreach (var directory in roots.Concat(testDirectories))
                    foreach (var extension in testFileExtensions)
                    {
                        var testPath = string.IsNullOrEmpty(name)
                            ? directory + name + extension
                            : Path.Combine(directory, name + extension);
                        var testFile = new FileInfo(testPath);
                        var message = current.FullName.Suffix(": ").PadRight(50) + testFile?.FullName;

                        if (testFile.Exists && testFile.FullName != parentIcon.Icon?.FullName)
                        {
                            Debug.WriteLine("*** " + message);
                            return IconReference.FromFile(testFile);
                        }

                        // Debug.WriteLine("    " + message);
                    }
            Debug.WriteLine("xxx " + current.FullName.Suffix(": ").PadRight(50) + parentIcon.Icon?.FullName);
            return IconReference.Empty;
        }

        #endregion Helpers: Get Current Icon

        #endregion Helpers
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}