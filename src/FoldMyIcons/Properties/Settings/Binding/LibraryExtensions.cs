namespace FoldMyIcons.Properties.Settings.Binding
{
    using Puchalapalli.Application.Settings.Binding;
    using LibSettings;
    using static LibSettings.LibSettings;


    public class LibExtension : SettingBindingExtension<LibSettings>
    {
        public LibExtension() : base() { }
        public LibExtension(string path) : base(path) { }
        protected override LibSettings Value => LibSetting;
    }
}