namespace FoldMyIcons.Properties.Settings.AppSettings
{
    using Puchalapalli.Application.Settings.BaseSettings;
    using Puchalapalli.Utilities.Debug;

    public static partial class AppSettings
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
        static AppSettings() {
            if (_settings != null)
            {
                DebugUtils.DEBUG.Break();
            }
            if (BaseSettings.CONSTRUCTOR_ACTION == BaseSettingsConstructor.Load)
            {
                Load();
            }
            else if (BaseSettings.CONSTRUCTOR_ACTION == BaseSettingsConstructor.Default)
            {
                AppSetting = new AppSettingsBase();
            }
            DebugUtils.DEBUG.Noop();
        }

        public static void Load() { AppSetting = AppSettingsBase.Load(); }
        public static void Save() {
            AppSetting.Save();
        }
    }
}
