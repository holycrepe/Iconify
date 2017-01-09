namespace FoldMyIcons.Properties.Settings.AppSettings
{
    using AllSettings;
    using Puchalapalli.Application.Settings.WindowSettings;

    public class WindowSettings : WindowSettingsBase<WindowSettings>
    {
        public override void LoadWindowPlacements()        
            => AllSettings.LoadWindowPlacements();
        

        public override void SaveWindowPlacements()        
            => AllSettings.SaveAllSettings();
                
    }
}
