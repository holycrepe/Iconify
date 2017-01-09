namespace FoldMyIcons.Properties.Settings.LibSettings
{
    #region Usings

    using System.ComponentModel;
    using Puchalapalli.Application;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Puchalapalli.Helpers.Utils;
    using Puchalapalli.Utilities.Debug;

    #endregion

    public sealed partial class LibSettings
    {
        #region Constructor
        static LibSettings _settings;
        public static LibSettings LibSetting
        {
            get { return _settings ?? (_settings = LibSettings.Load()); }
            set
            {
                if (_settings == null)
                {
                    _settings = value;
                }
                else
                {
                    _settings.Load(value);
                }
            }
        }
        static LibSettings()
        {
            if (MainApp.DesignMode)
            {                
                //DEBUG.Break();
                return;
            }
            if (_settings != null)
            {
                DebugUtils.DEBUG.Break();
            }
            if (CONSTRUCTOR_ACTION == BaseSettingsConstructor.Load)
            {
                LoadInstance();
            }
            else if (CONSTRUCTOR_ACTION == BaseSettingsConstructor.Default)
            {
                LoadInstance(new LibSettings());
            }
            
            DebugUtils.DEBUG.Noop();
        }

        #endregion

        #region Load/Save
        public static LibSettings LoadInstance()
            => LoadInstance(Load());
        public static LibSettings LoadInstance(LibSettings libSetting)
            => BindInstance(LibSetting = libSetting);
        public static LibSettings BindInstance()
            => BindInstance(Load());
        public static LibSettings BindInstance(LibSettings libSetting)
        {            
            return libSetting;
        }

        public static void SaveInstance() { LibSetting.Save(); }        

        #endregion
    }
}
