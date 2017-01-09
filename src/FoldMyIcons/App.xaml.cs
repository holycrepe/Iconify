namespace FoldMyIcons
{
    using System.Windows;
    using System.Windows.Forms;
    using Puchalapalli.Utilities.Debug;
    using Telerik.Windows.Automation.Peers;
    using Application = System.Windows.Application;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Public Fields
        public static NotifyIcon NotifyIcon;
        #endregion Public Fields

        public App()
        {
            AutomationManager.AutomationMode = AutomationMode.Disabled;
            InitializeComponent();
            LOGGER.LogLocation("Instantiated App");

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            LOGGER.LogLocation();
            base.OnStartup(e);
            // here you take control
        }
    }
}
