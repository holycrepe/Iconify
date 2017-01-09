namespace FoldMyIcons.Models.Favorites
{
    using System;
    using System.Runtime.Serialization;
    using Puchalapalli.IO;

    [DataContract]
    public class FavoritePath
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Path { get; set; }
        [DataMember]
        public FavoritePathType Type { get; set; }
        [DataMember]
        public string IconPath { get; set; }
        public FavoritePath() {}
        public FavoritePath(string path, FavoritePathType type, string iconPath = null) 
            : this(type.GetNameFromPath(path), path, type, iconPath) { }
        public FavoritePath(string name, string path, FavoritePathType type, string iconPath=null)
        {
            Name = name;
            Path = path;
            Type = type;
            IconPath = iconPath;
        }

        public static string GetNameFromPath(string path, FavoritePathType type)
        {
            
            switch (type)
            {
                case FavoritePathType.Folders:
                    return Paths.GetFileName(path);
                case FavoritePathType.Icons:
                    return Paths.GetFileNameWithoutExtension(path);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        public override string ToString()
        {
            return $"[{Type}]: {Name}: {nameof(Path)}: {Path}";
        }
    }
}