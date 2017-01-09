// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.LibSettings
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using PostSharp.Patterns.Model;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Serialization.Namespaces;

    [DataContract(Name = "Lib", Namespace = Namespaces.Default)]
    [KnownType(typeof(LibDirectorySettings)),
        KnownType(typeof(LibToggleSettings))]
    [NotifyPropertyChanged]
    public sealed partial class LibSettings : BaseSettings
    {
        private LibThemeSettings _theme;

        #region Implementation

        [DataMember]
        [Display(Name = "Theme Settings", Description = "Application Theme Settings",
             GroupName = "Application Settings", Order = 1)]
        public LibThemeSettings Theme
        {
            get { return _theme ?? (_theme = new LibThemeSettings(nameof(Theme))); }
            set { _theme = value; }
        }
        [DataMember]
        public LibDirectorySettings Directories { get; set; }
        [DataMember]
        public LibToggleSettings Toggles { get; set; }

        [DataMember]
        public int Verbosity { get; set; } = 2;

        protected override object[] DebuggerDisplayProperties
            =>
                new object[]
                {
                    nameof(Directories),
                    Directories,
                    nameof(Toggles),
                    Toggles,
                    nameof(Theme),
                    Theme,
                    nameof(Verbosity),
                    Verbosity
                };

        #endregion

        #region Constructor

        public LibSettings()
        {
            Directories = new LibDirectorySettings(nameof(Directories));
            Toggles = new LibToggleSettings(nameof(Toggles));
            Theme = new LibThemeSettings(nameof(Theme));
        }

        #endregion
        #region Save/Load

        public static LibSettings Load()
            => Load<LibSettings>();
        public void Load(LibSettings other)
        {
            if (other != null)
            {
                this.Directories = other.Directories;
                this.Theme = other.Theme;
                this.Toggles = other.Toggles;
                this.Verbosity = other.Verbosity;
            }
        }
        #endregion
    }
}
