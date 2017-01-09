// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.AllSettings
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using FoldMyIcons.Properties.Settings.AppSettings;
    using PostSharp.Patterns.Model;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Puchalapalli.Infrastructure.ContextManagers;
    using Puchalapalli.Serialization.Attributes;
    using LibSettings;
    using Serialization.Namespaces;
    using static LibSettings.LibSettings;

    [Serializable]
    [XmlSerializerAssembly("wUAL.Serializers")]
    [XmlInclude(typeof(AppSettingsBase)),
        XmlInclude(typeof(LibSettings))]
    [XmlRoot(nameof(AllSettings))]
    [DataContract(Name = nameof(AllSettings), Namespace = Namespaces.Default)]
    [Namespace(Prefix = "", Uri = Namespaces.Default),
        Namespace(Prefix = Prefixes.Puchalapalli, Uri = Namespaces.Puchalapalli)]
    [KnownType(typeof(AppSettingsBase)),
        KnownType(typeof(LibSettings))]
    [DebuggerDisplay("{DebuggerDisplay(1)}")]
    [NotifyPropertyChanged]
    public partial class AllSettingsBase : BaseSettings
    {
#if SETTINGS_USE_ALL_SETTINGS
        [DataMember(Order = 4)]
        //[XmlElement(Order=4)]
        [XmlIgnore]
        [SafeForDependencyAnalysis]
        public AppSettingsBase App
        {
            get { return AppSettings.AppSetting; }
            set { AppSettings.AppSetting = value; }
        }
        [DataMember(Order = 3)]
        //[XmlElement(Order=3)]
        [XmlIgnore]
        [SafeForDependencyAnalysis]
        public LibSettings Lib
        {
            get { return LibSettings.LibSetting; }
            set { LibSettings.LibSetting = value; }
        }
        [DataMember(Order = 2)]
        //[XmlElement(Order=2)]
        [XmlIgnore]
        [SafeForDependencyAnalysis]
        public ToggleSettingsBase Toggle
        {
            get { return ToggleSettings.Toggles; }
            set { ToggleSettings.Toggles = value; }
        }
        [DataMember(Order = 1)]
        [XmlElement(Order = 1)]
        [SafeForDependencyAnalysis]
        public MySettingsBase My
        {
            get { return MySettings.MySetting; }
            set { MySettings.MySetting = value; }
        }
#else
        [DataMember(Order = 4)]
        //[XmlElement(Order=4)]
        [XmlIgnore]
        public AppSettingsBase App { get; set; }
        [DataMember(Order = 2)]
        //[XmlElement(Order=2)]
        [XmlIgnore]
        public LibSettings Lib { get; set; }
#endif
        [XmlIgnore]
        public static ContextManagers SaveInProgress { get; set; } = new ContextManagers();

        [IgnoreAutoChangeNotification]
        protected override object[] DebuggerDisplayProperties => new object[] {
            nameof(App), App,
            nameof(Lib), Lib
        };

        public AllSettingsBase() {
            //App = new AppSettingsBase();
            //Lib = new LibSettings();
            //Toggle = new ToggleSettingsBase();
            //My = new MySettingsBase();
        }
        public static AllSettingsBase New()
            => new AllSettingsBase(AppSettings.AppSetting, LibSettings.LibSetting);
        public static void SaveAllSettings()
        {
            if (!SaveInProgress)
                using (SaveInProgress.On)
                    New().Save();
            return;
        }
        public AllSettingsBase(AppSettingsBase app, LibSettings lib)
        {
            App = app;
            Lib = lib;
        }
        public static AllSettingsBase Load()
            => Load<AllSettingsBase>();
    }
}
