// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.LibSettings
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using PostSharp.Patterns.Model;
    using Puchalapalli.Application.Settings.AppSettings;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Puchalapalli.WPF.TelerikUI.Themes;
    using Puchalapalli.WPF.Themes.Xaml;


    public sealed partial class LibSettings
    {
        [DataContract(Namespace = "")]
        [DebuggerDisplay("{DebuggerDisplay(1)}")]
        [NotifyPropertyChanged]
        public class LibThemeSettings : BaseThemeSettings
        {
            #region Overrides of BaseThemeSettings
            [SafeForDependencyAnalysis]
            protected override TelerikTheme Default => TelerikThemes.VisualStudio2013.Blue;
            #endregion

            public LibThemeSettings() : base()
            {
            }

            public LibThemeSettings(string name)
                : base(name)
            {
            }
        }
    }
}
