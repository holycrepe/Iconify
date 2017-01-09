namespace FoldMyIcons.Commands
{
    using Alphaleonis.Win32.Filesystem;
    using Properties.Settings.LibSettings;
    using Puchalapalli.Infrastructure.Media.Icons;

    public class FolderIconCommandArguments
    {
        public FolderIconCommand Command { get; set; }
        public DirectoryInfo Directory { get; set; }
        public DirectoryInfo[] Directories { get; set; }
        public LibSettings Settings { get; set; }
        public IconReference LastIcon { get; set; } = IconReference.Empty;

        public LibSettings.LibToggleSettings Toggles
            => Settings.Toggles;

        public string Root
            => Settings.Directories.Roots.GetActionRoot(this.Directory);
        public bool CreateDirectories { get; set; }
        public bool PreviewMode => Toggles.PreviewMode;
        public bool Recursive => Toggles.Recursive;
        public int Verbosity
            => Settings.Verbosity;
        public FolderIconCommandArguments(FolderIconCommand command, string directory, LibSettings settings, bool createDirectories=false)
        {
            this.Command = command;
            this.Directory = new DirectoryInfo(directory);
            this.Settings = settings;
            this.Directories = new [] {this.Directory};
            CreateDirectories = createDirectories;
        }
    }
}