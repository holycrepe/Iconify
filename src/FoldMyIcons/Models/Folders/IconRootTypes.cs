using System.Linq;

namespace FoldMyIcons.Folders
{
    public static class IconRootTypes
    {
        public static readonly IconRootType[] Main =
        {
            IconRootType.Action,
            IconRootType.Content,
            IconRootType.Icons,
            IconRootType.Library
        };

        public static bool IsMain(this IconRootType value)
            => Main.Contains(value);
    }    
}