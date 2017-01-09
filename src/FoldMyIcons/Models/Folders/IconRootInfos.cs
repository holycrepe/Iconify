namespace FoldMyIcons.Folders
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using Alphaleonis.Win32.Filesystem;
    using PostSharp.Patterns.Model;
    using Properties;
    using Properties.Arguments;
    using Properties.Settings.LibSettings;

    [NotifyPropertyChanged]
    public class IconRootInfos : IconRoots<DirectoryInfo>
    {
        [SafeForDependencyAnalysis]
        public override ObservableCollection<string> Labels
        {
            get { return LibSettings.LibSetting?.Directories?.Roots?.Labels; }
            set
            {
                var roots = LibSettings.LibSetting?.Directories?.Roots;
                if (roots != null)
                    roots.Labels = value;
            }
        }
        public IconRootInfos()
        {
            if (ApplicationArguments.Options?.Roots != null)
                ApplicationArguments.Options.Roots.PropertyChanged += DirectoriesOnPropertyChanged;
            else            
                Debugger.Break();            
            if (LibSettings.LibSetting?.Directories?.Roots != null)
                LibSettings.LibSetting.Directories.Roots.PropertyChanged += DirectoriesOnPropertyChanged;
            else
                Debugger.Break();
            Refresh();
        }
        
        private void DirectoriesOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(e.PropertyName);
        }

        public void Refresh()
        {
            foreach (var type in IconRootTypes.Main)
            {
                var value = Refresh(type);
                Set(type, value);
            }
        }
        DirectoryInfo Refresh(IconRootType type)
        {
            if (!type.IsMain())
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            var path = ApplicationArguments.Options?.Roots?.Get(type) ?? LibSettings.LibSetting?.Directories?.Roots?.Get(type);
            return string.IsNullOrWhiteSpace(path)
                ? null
                : new DirectoryInfo(path);
        }

        public bool IsActionRoot(DirectoryInfo current)
            => this.Action != null && current.FullName.StartsWith(this.Action.FullName);
        public DirectoryInfo GetActionRoot(DirectoryInfo current)
            => IsActionRoot(current)
                ? this.Action
                : current.Root;
    }
}