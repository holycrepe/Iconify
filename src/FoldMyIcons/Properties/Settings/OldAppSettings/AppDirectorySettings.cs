// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.AppSettings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Alphaleonis.Win32.Filesystem;
    using Path = Alphaleonis.Win32.Filesystem.Path;
    using File = Alphaleonis.Win32.Filesystem.File;
    using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
    using Directory = Alphaleonis.Win32.Filesystem.Directory;
    using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
    using System.Xml.Serialization;
    using JetBrains.Annotations;
    using PostSharp.Patterns.Model;
    using System.ComponentModel;

    using static Puchalapalli.Utilities.Debug.DebugUtils;
    using System.Runtime.Serialization;
    using System.Diagnostics;
    using System.Linq;

    using Puchalapalli;
    using Puchalapalli.Application;
    using Puchalapalli.Application.Settings;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Puchalapalli.Extensions.Collections;
    using Puchalapalli.IO;

    using Serialization.Namespaces;
    using PureAttribute = System.Diagnostics.Contracts.PureAttribute;




    public sealed partial class AppSettings
    {
        [DataContract(Namespace = Namespaces.Default)]
        [DebuggerDisplay("{DebuggerDisplay(1)}")]
        [NotifyPropertyChanged]
        public class AppDirectorySettings : BaseSubSettings
        {
            #region Fields

            private string _clientCache;
            #endregion
            #region Primary
            [DataMember(EmitDefaultValue = false)]
            public string DirectoryBrowser { get; set; }
            [DataMember(EmitDefaultValue = false)]
            public string IconBrowser { get; set; }
            [DataMember(EmitDefaultValue = false)]
            public string IconLibrary { get; set; }
            [DataMember(EmitDefaultValue = false)]
            public IconRootFolders Roots { get; set; }

            public AppDirectorySettings() : this($"Initializing.{nameof(AppDirectorySettings)}") { }
            public AppDirectorySettings(string name) : base(name)
            {
                //this.ClientCache = MainApp.Data.GetLocalPath("UTorrentClientCache.xml");
            }

            #endregion

            #region Private

            const string DEFAULT_DIRECTORY_ROOT = "$";
            const string DEFAULT_DIRECTORY_NAME = "TORRENTS";
            const string DEFAULT_ADDED_SUBDIRECTORY = "ADDED";
            const string DEFAULT_SETTINGS_SUBDIRECTORY = "SETTINGS";

            #endregion

            #region Derived
            [IgnoreAutoChangeNotification]
            protected override object[] DebuggerDisplayProperties => new object[]
            {
                nameof(DirectoryBrowser), DirectoryBrowser,
                nameof(IconBrowser), IconBrowser,
                nameof(IconLibrary), IconLibrary,
                nameof(Roots), Roots
            };
            public static readonly string[] DirectoryValidityDeterminingSettings = { nameof(IconLibrary) };
            #endregion
        }
    }
}
