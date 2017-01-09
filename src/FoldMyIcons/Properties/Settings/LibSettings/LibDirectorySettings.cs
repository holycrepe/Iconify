// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.LibSettings
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Folders;
    using Models.Favorites;
    using PostSharp.Patterns.Model;
    using Puchalapalli.Application;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Serialization.Namespaces;
    using File = Alphaleonis.Win32.Filesystem.File;

    public sealed partial class LibSettings
    {
        [DataContract(Namespace = Namespaces.Default)]
        [DebuggerDisplay("{DebuggerDisplay(1)}")]
        [NotifyPropertyChanged]
        public sealed class LibDirectorySettings : BaseSubSettings
        {
            #region Primary Members
            [DataMember]
            public string IconExplorer { get; set; } 
            [DataMember]
            public string DirectoryExplorer { get; set; }
            [DataMember]
            public string Icon { get; set; } 
            [DataMember]
            public string Library { get; set; }

            [DataMember]
            public IconRootFolders Roots { get; set; }

            [DataMember]
            public FavoritePaths FavoriteFolders { get; set; }
            [DataMember]
            public FavoritePaths FavoriteIcons { get; set; }
            public LibDirectorySettings() : this($"Initializing.{nameof(LibDirectorySettings)}") { }

            public LibDirectorySettings(string name) : base(name)
            {
                Initialize();
            }

            #endregion
            
            #region Derived Members
            protected override object[] DebuggerDisplayProperties => new object[]
            {
                nameof(IconExplorer), IconExplorer,
                nameof(DirectoryExplorer), DirectoryExplorer,
                nameof(Icon), Icon,
                nameof(Library), Library,
                nameof(Roots), Roots
            };

            #endregion

            #region Methods
            #region Methods: Initialize

            protected override void InitializeEmptyValues(SettingsInitializationTrigger trigger)
            {
                if (Roots == null)
                    Roots = new IconRootFolders();
                if (FavoriteFolders == null)
                    FavoriteFolders = new FavoritePaths();
                if (FavoriteIcons == null)
                    FavoriteIcons = new FavoritePaths();
            }
            #endregion Methods: Initialize
            #region Methods: Favorites
            public void AddFavorite(FavoritePath favorite)
            {
                var favorites = GetFavorites(favorite.Type);
                favorites.Add(favorite);
            }
            public void DeleteFavorite(FavoritePath favorite)
            {
                var favorites = GetFavorites(favorite.Type);
                favorites.Remove(favorite);
            }
            public FavoritePaths GetFavorites(FavoritePathType type)
            {
                switch (type)
                {
                    case FavoritePathType.Folders:
                        return FavoriteFolders;
                    case FavoritePathType.Icons:
                        return FavoriteIcons;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
            #endregion Methods: Favorites
            #region Methods: Serialization Events
            [OnDeserializing]
            void OnDeserializing(StreamingContext context)
                => OnPopulating(SettingsInitializationTrigger.Deserializer);

            [OnDeserialized]
            void OnDeserialized(StreamingContext context)
                => OnPopulated(SettingsInitializationTrigger.Deserializer);
            #endregion Methods: Serialization Events
            #endregion Methods
        }
    }
}
