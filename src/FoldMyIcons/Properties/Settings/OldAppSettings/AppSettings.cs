namespace FoldMyIcons.Properties.Settings.AppSettings
{ 
    using Puchalapalli.Application.Settings.BaseSettings;
    
    using static Puchalapalli.Application.Settings.BaseSettings.BaseSettings;
    using static Puchalapalli.Utilities.Debug.DebugUtils;
    public sealed partial class AppSettings
    {
        static AppSettingsBase _settings;
        public static AppSettingsBase AppSetting
        {
            get { return _settings ?? (_settings = AppSettingsBase.Load()); }
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
        static AppSettings()
        {
            if (_settings != null)
            {
                DEBUG.Break();
            }
            if (CONSTRUCTOR_ACTION == BaseSettingsConstructor.Load)
            {
                Load();
            }
            else if (CONSTRUCTOR_ACTION == BaseSettingsConstructor.Default)
            {
                AppSetting = new AppSettingsBase();
            }
            DEBUG.Noop();
        }

        public static void Load() { AppSetting = AppSettingsBase.Load(); }
        public static void Save()
        {
            AppSetting.Save();
        }
    }
}
