namespace FoldMyIcons.Models.Generation
{
    using System.Collections.Generic;
    using Puchalapalli.Extensions.Collections;
    using Puchalapalli.IO.AlphaFS.Info;

    public class IconGenerationPath
    {
        public double Priority { get; }
        public double[] Priorities { get; }
        public RelativePath Directory { get; }
        public IconGenerationRoot Root { get; }
        public IconGenerationSource Source { get; }

        public IconGenerationPath(IconGenerationSource source, double priority, RelativePath directory)
        {
            Source = source;
            Root = Source.Root;
            Priority = priority;
            Priorities = Source.Priorities.ConcatArray(Priority);
            Directory = directory;
        }

        public override string ToString()
        {
            return $"{{ #{Priority}: {Directory.FullName}, Source = {Source} }}";
        }

        public override bool Equals(object value)
        {
            var type = value as IconGenerationPath;
            return (type != null) && EqualityComparer<RelativePath>.Default.Equals(type.Directory, Directory);
        }

        public override int GetHashCode()
        {
            int num = 0x7a2f0b42;
            return (-1521134295 * num) + EqualityComparer<RelativePath>.Default.GetHashCode(Directory);
        }
    }
}