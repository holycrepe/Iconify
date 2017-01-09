// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.LibSettings
{
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
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
        public class LibToggleSettings : BaseSubSettings
        {
            #region Primary Members

            [DataMember]
            public bool Async { get; set; } = true;
            [DataMember]
            public bool PreviewMode { get; set; } = true;
            [DataMember]
            public bool AutoExpand { get; set; } = true;
            [DataMember]
            public bool Recursive { get; set; } = true;
            [DataMember]
            public bool ReportChanges { get; set; } = true;
            [DataMember]
            public bool ResetCaches { get; set; } = true;

            public LibToggleSettings() : this($"Initializing.{nameof(LibToggleSettings)}") { }
            public LibToggleSettings(string name) : base(name) { }

            #endregion
            
            #region Derived Members
            protected override object[] DebuggerDisplayProperties => new object[]
            {
                nameof(Async), Async,
                nameof(PreviewMode), PreviewMode,
                nameof(AutoExpand), AutoExpand,
                nameof(Recursive), Recursive
            };
            

            #endregion
        }
    }
}
