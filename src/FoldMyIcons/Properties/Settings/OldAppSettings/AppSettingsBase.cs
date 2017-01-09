using System;

// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.AppSettings
{
    using System.Xml.Serialization;
    using PostSharp.Patterns.Model;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Diagnostics;

    using Puchalapalli.Application.Settings;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Puchalapalli.WPF.Settings.WindowSettings;

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
        //[DataMember(EmitDefaultValue = false, Order = 2)]
        //[XmlIgnore]
        //[SafeForDependencyAnalysis]
        //public Dictionary<string, WINDOWPLACEMENT?> Placements
        //{
        //    get
        //    {
        //        return WindowPlacements.Placements;
        //    }
        //    set
        //    {
        //        if (value != null)
        //            WindowPlacements.Placements = value;
        //    }
        //}

        //[XmlElement(ElementName = "Placements", Order = 2)]
        //public List<WindowPlacementEntry> PlacementsList { get; set; }
        //    = new List<WindowPlacementEntry>();

        [DataMember]
        public bool EnableAsync { get; set; } = true;
        [DataMember]
        public bool PreviewMode { get; set; } = true;
        [DataMember]
        public bool AutoExpand { get; set; } = true;
        [DataMember]
        public bool Recursive { get; set; } = true;

        [DataMember(EmitDefaultValue = false, Order = 1)]
        public int StatusRowHeight { get; set; } = 150;

        //[DataMember(EmitDefaultValue = false, Name = nameof(GridViewColumns))]
        //Dictionary<string, string[]> _gridViewColumns { get; set; } = new Dictionary<string, string[]>();

        //public Dictionary<string, string[]> GridViewColumns
        //    => _gridViewColumns ?? (_gridViewColumns = new Dictionary<string, string[]>());

        [IgnoreAutoChangeNotification]
        protected override object[] DebuggerDisplayProperties => new object[] {
            // nameof(Placements), Placements,
            nameof(StatusRowHeight), StatusRowHeight
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
                //this.Placements = other.Placements;
                //this.PlacementsList = other.PlacementsList;
                this.StatusRowHeight = other.StatusRowHeight;
                //this._gridViewColumns = other.GridViewColumns;
            }
        }
    }
}
