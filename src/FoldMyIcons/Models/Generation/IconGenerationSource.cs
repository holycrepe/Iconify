namespace FoldMyIcons.Models.Generation
{
    using System.Collections.Generic;
    using Alphaleonis.Win32.Filesystem;
    using Puchalapalli.Extensions.Collections;

    public class IconGenerationSource
    {
        public double Priority { get; }
        public double[] Priorities { get; }
        public DirectoryInfo Directory { get; }
        public IconGenerationRoot Root { get; }

        public IconGenerationSource(IconGenerationRoot root, double priority, DirectoryInfo directory)
        {
            Root = root;
            Priority = priority;
            Priorities = Root.Priorities.ConcatArray(Priority);
            Directory = directory;
        }

        public override string ToString()
        {
            return $"{{ #{Priority}: {Directory} }}";
        }

        public override bool Equals(object value)
        {
            var type = value as IconGenerationSource;
            return (type != null) && EqualityComparer<DirectoryInfo>.Default.Equals(type.Directory, Directory);
        }

        public override int GetHashCode()
        {
            int num = 0x7a2f0b42;
            return (-1521134295 * num) + EqualityComparer<DirectoryInfo>.Default.GetHashCode(Directory);
        }
    }
}