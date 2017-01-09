namespace FoldMyIcons.Properties.Settings.Binding
{
    using AllSettings;
    using AppSettings;
    using Puchalapalli.Application.Settings.Binding;
    using static AllSettings.AllSettings;
    using static AppSettings.AppSettings;

    public class AppExtension : SettingBindingExtension<AppSettingsBase>
    {
        public AppExtension() : base() { }
        public AppExtension(string path) : base(path) { }
        protected override AppSettingsBase Value => AppSetting;
    }

    public class AllExtension : SettingBindingExtension<AllSettingsBase>
    {
        public AllExtension() : base() { }
        public AllExtension(string path) : base(path) { }
        protected override AllSettingsBase Value => AllSettings.Settings;
    }
}