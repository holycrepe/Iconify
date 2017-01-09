namespace FoldMyIcons.Folders
{
    using Alphaleonis.Win32.Filesystem;
    using Puchalapalli.Extensions.Primitives;

    public class IconRootFolders : IconRoots<string>
    {
        public bool IsActionRoot(DirectoryInfo current)
            => current.FullName.StartsWith(this.Action);
        public string GetActionRoot(DirectoryInfo current)
            => IsActionRoot(current)
                ? this.Action
                : current.Root.FullName;
    }
}