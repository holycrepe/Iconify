// ReSharper disable InconsistentNaming

namespace FoldMyIcons.Properties.Settings.LibSettings
{
    #region Usings

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using PostSharp.Patterns.Model;
    using Puchalapalli.Extensions.Collections;

    #endregion

    public sealed partial class LibSettings
    {
        #region Fields and Properties

        #region Fields

        #region Fields: Public

        public static string AppPath = Assembly.GetExecutingAssembly().CodeBase;
        //public static readonly string[] ValidityDeterminingSettings = {nameof(LibSetting.CONNECTION), nameof(LibSetting.DIRECTORY) };

        #endregion

        #region Fields: Private

        private static List<string> arguments;

        #endregion

        #endregion

        #region  Properties

        #region Properties: Public
        

        //public static LibSettings LibSetting { get; } = (LibSettings) Synchronized(new LibSettings());

        #region Preview Mode
        public static bool PreviewMode { get; set; }

        public static string PreviewModeDescription
            => PreviewMode
                ? " in Preview Mode"
                : "";

        public static string StatusDescription
            => 
            //" " + (LibSetting.Toggles.IS_CONNECTED ? "Connected" : "Unconnected") + 
            PreviewModeDescription;

        #endregion

        public static List<string> Arguments
        {
            get { return arguments; }
            set
            {
                arguments = value;
                if (HasArgument("--preview", "-p"))
                {
                    PreviewMode = true;
                }
            }
        }

        #endregion

        #endregion

        #endregion

        public static bool HasArgument(params string[] args)
        {
            if (Arguments == null) {
                Debugger.Break();
                return false;
            }
            return Arguments.ContainsAny(args);
        }
    }
}
