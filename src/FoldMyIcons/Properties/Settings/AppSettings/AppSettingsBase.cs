// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.AppSettings
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using PostSharp.Patterns.Model;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Puchalapalli.Application.Settings.WindowSettings;
    using Serialization.Namespaces;

    [Serializable]
    [XmlSerializerAssembly("wUAL.Serializers")]
    [XmlInclude(typeof(WindowPlacementEntry))]
    [XmlRoot("App")]
    [DataContract(Name = "App", Namespace = Namespaces.Default)]
    [KnownType(typeof(WINDOWPLACEMENT))]
    //[KnownType(typeof(RECT))]
    //[KnownType(typeof(POINT))]
    [DebuggerDisplay("{DebuggerDisplay(1)}")]
    [NotifyPropertyChanged]
    public partial class AppSettingsBase : BaseSettings
    {
        [DataMember(EmitDefaultValue = false, Order = 3)]
        [XmlIgnore]
        [SafeForDependencyAnalysis]
        public Dictionary<string, WINDOWPLACEMENT?> Placements
        {
            get
            {
                return WindowPlacements.Placements;
            }
            set
            {
                if (value != null)
                    WindowPlacements.Placements = value;
            }
        } 
            
        [XmlElement(ElementName= nameof(Placements), Order=3)]
        public List<WindowPlacementEntry> PlacementsList { get; set; }
            = new List<WindowPlacementEntry>();
        [DataMember(EmitDefaultValue = false, Order = 1)]
        [XmlElement(Order = 1)]
        public int IconExplorerWidth { get; set; } = 400;
        [DataMember(EmitDefaultValue = false, Order = 2)]
        [XmlElement(Order = 2)]
        public int IconViewerWidth { get; set; } = 500;
        [DataMember(EmitDefaultValue = false, Order = 3)]
        [XmlElement(Order = 3)]
        public int StatusRowHeight { get; set; } = 100;


        [IgnoreAutoChangeNotification]
        protected override object[] DebuggerDisplayProperties => new object[] {
            nameof(IconExplorerWidth), IconExplorerWidth,
            nameof(IconViewerWidth), IconViewerWidth,
            nameof(StatusRowHeight), StatusRowHeight,
            nameof(Placements), Placements
        };

        public AppSettingsBase()
        {

        }
        public static AppSettingsBase Load()
            => Load<AppSettingsBase>();
        public void Load(AppSettingsBase other)
        {
            if (other != null)
            {
                this.Placements = other.Placements;
                this.PlacementsList = other.PlacementsList;
                this.IconExplorerWidth = other.IconExplorerWidth;
                this.IconViewerWidth = other.IconViewerWidth;
            }
        }
    }
}
