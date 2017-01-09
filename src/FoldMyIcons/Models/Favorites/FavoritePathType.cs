namespace FoldMyIcons.Models.Favorites
{
    using System;
    using Puchalapalli.IO;

    public enum FavoritePathType
    {
        Folders,
        Icons
    }

    public static class FavoritePathTypeExtensions
    {
        public static bool IsFile(this FavoritePathType type)
        {

            switch (type)
            {
                case FavoritePathType.Folders:
                    return false;
                case FavoritePathType.Icons:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string GetNameFromPath(this FavoritePathType type, string path)
            => type.IsFile()
                ? Paths.GetFileNameWithoutExtension(path)
                : Paths.GetFileName(path);
    }
}