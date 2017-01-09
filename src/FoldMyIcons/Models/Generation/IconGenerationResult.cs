namespace FoldMyIcons.Models.Generation
{
    using System;
    using System.Collections.Generic;
    using Alphaleonis.Win32.Filesystem;
    using Puchalapalli.Infrastructure.Media.Icons;
    using Puchalapalli.IO.AlphaFS.Info;

    public class IconGenerationResult
    {
        public static IconGenerationResult Uninitialized => new IconGenerationResult(null, double.MaxValue, IconReference.Uninitialized);
        public static IconGenerationResult Empty => new IconGenerationResult(null, double.MaxValue, IconReference.Empty);
        public IconGenerationFolder Folder { get; }
        public double Priority { get; }
        public IconReference Icon { get; }
        public double[] Priorities { get; }
        public IconGenerationRoot Root { get; }
        public IconGenerationSource Source { get; }
        public IconGenerationPath Path { get; }
        public DirectoryInfo FolderSource { get; }
        public RelativePath Directory { get; }

        public IconGenerationResult(IconGenerationFolder folder, double priority, IconReference icon)
        {
            Folder = folder;
            Priority = priority;
            Icon = icon;
            if (Folder == null)
            {
                Priorities = new[] { Priority };
                return;
            }
            FolderSource = Folder.FolderSource;
            Root = Folder.Root;
            Source = Folder.Source;
            Path = Folder.Path;
            Directory = Folder.Directory;
            Priorities = new [] { Root.Priority, Folder.Priority, Path.Priority, Priority, Source.Priority };
        }

        public override string ToString()
        {
            return $"{{ #{string.Join(".", Priorities)}: {Icon.Info}, Folder = {Folder} }}";
        }

        public override bool Equals(object value)
        {
            var type = value as IconGenerationResult;
            return (type != null) && EqualityComparer<string>.Default.Equals(type.Icon.FullName, Icon.FullName);
        }

        public override int GetHashCode()
        {
            int num = 0x7a2f0b42;
            return (-1521134295 * num) + EqualityComparer<string>.Default.GetHashCode(Icon.FullName);
        }
    }
}