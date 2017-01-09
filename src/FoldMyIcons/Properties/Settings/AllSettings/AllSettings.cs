#define SETTINGS_USE_ALL_SETTINGS_X

namespace FoldMyIcons.Properties.Settings.AllSettings
{
    using System.Collections.Generic;
    using FoldMyIcons.Properties.Settings.AppSettings;
    using Puchalapalli.Application.Settings.BaseSettings;
    using Puchalapalli.Utilities.Debug;
    using LibSettings;
    using Puchalapalli.Application.Settings.WindowSettings;
    using static LibSettings.LibSettings;

    public static partial class AllSettings
    {
#if SETTINGS_USE_ALL_SETTINGS
        public static readonly bool USE_ALL_SETTINGS = true;
        public static readonly bool SAVE_ALL_SETTINGS = true;
#else
        public static readonly bool USE_ALL_SETTINGS = false;
        public static readonly bool SAVE_ALL_SETTINGS = true;
#endif
        public static AllSettingsBase Settings { get; set; }
        = new AllSettingsBase();
        static AllSettings() {
            // LoadAllSettings();
            DebugUtils.DEBUG.Noop();
        }
        public static void LoadWindowPlacements()
        {
            if (USE_ALL_SETTINGS)
            {
                if (Settings == null)
                {
                    LoadAllSettings();
                }
                else
                {
                    LoadWindowPlacementFromFile();
                }
            }
            if (AppSettings.AppSetting == null)
            {
                AppSettings.Load();
            }
            else
            {
                LoadWindowPlacementFromFile();
            }            
        }
        static AppSettingsBase LoadAppSettings()
            => AllSettingsBase.Load()?.App ?? AppSettingsBase.Load();
        static Dictionary<string, WINDOWPLACEMENT?> LoadAppWindowPlacements()
            => AllSettingsBase.Load()?.App?.Placements ?? AppSettingsBase.Load()?.Placements;
        static void LoadWindowPlacementFromFile()
        {
            if (USE_ALL_SETTINGS)
            {
                //Settings = AllSettingsBase.Load();
                AppSettings.AppSetting = Settings.App;
            }
            else if (AppSettings.AppSetting == null)
            {
                AppSettings.AppSetting = LoadAppSettings();
            }
            else
            {
                AppSettings.AppSetting.Placements = LoadAppWindowPlacements();
            }            
        }
        public static void SaveWindowPlacement()
        {
            foreach (var setting in WindowSettings.Instances)
            {
                setting.SaveWindowState();
            }
        }
        public static void LoadAllSettings()
        {
            var settings = (USE_ALL_SETTINGS || SAVE_ALL_SETTINGS) ? AllSettingsBase.Load() : null;

            BaseSettings.CONSTRUCTOR_ACTION = BaseSettingsConstructor.None;
            AppSettings.AppSetting = settings?.App ?? AppSettingsBase.Load();
            LibSettings.LibSetting = settings?.Lib ?? LibSettings.BindInstance();
        }

        public static void SaveAllSettings(bool includeWindowPlacement=true)
        {
            if (includeWindowPlacement)
                SaveWindowPlacement();
            if (USE_ALL_SETTINGS || SAVE_ALL_SETTINGS)
            {
                Settings.App = AppSettings.AppSetting;
                Settings.Lib = LibSettings.LibSetting;
                AllSettingsBase.SaveAllSettings();
                
                //Settings.Toggle = ToggleSettings.ToggleSetting;
                //Settings.My = MySettings.MySetting;
                //Settings.Save();
            }

            if (!USE_ALL_SETTINGS)
            {
                AppSettings.Save();
                LibSettings.SaveInstance();
            }
        }
    }
}
