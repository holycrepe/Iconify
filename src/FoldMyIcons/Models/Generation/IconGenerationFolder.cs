namespace FoldMyIcons.Models.Generation
{
    using System.Collections.Generic;
    using Alphaleonis.Win32.Filesystem;
    using Puchalapalli.Extensions.Collections;
    using Puchalapalli.IO.AlphaFS.Info;

    public class IconGenerationFolder
    {
        public double Priority { get; }
        public double[] Priorities { get; }
        public IconGenerationRoot Root { get; }
        public IconGenerationSource Source { get; }
        public IconGenerationPath Path { get; }
        public DirectoryInfo FolderSource { get; }
        public RelativePath Directory { get; }

        public IconGenerationFolder(IconGenerationPath path, double priority, DirectoryInfo folderSource)
        {
            Path = path;
            Priority = priority;
            FolderSource = folderSource;
            Priorities = Path.Priorities.ConcatArray(Priority);
            Root = Path.Root;
            Source = Path.Source;
            Directory = Path.Directory;
        }

        public override string ToString()
        {
            return $"{{ #{Priority}: Source = {FolderSource.FullName}, Path = {Path} }}";
        }

        public override bool Equals(object value)
        {
            var type = value as IconGenerationFolder;
            return (type != null) && EqualityComparer<RelativePath>.Default.Equals(type.Directory, Directory);
        }

        public override int GetHashCode()
        {
            int num = 0x7a2f0b42;
            return (-1521134295 * num) + EqualityComparer<RelativePath>.Default.GetHashCode(Directory);
        }
    }
}