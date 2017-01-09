namespace FoldMyIcons
{
    //using System.IO;
    using System;
    using System.Diagnostics;
    using Puchalapalli.Application;
    using Puchalapalli.Infrastructure.Reporting;
    using Puchalapalli.Infrastructure.Reporting.Reports;
    using Puchalapalli.Utilities.Debug;
    using Puchalapalli.WPF.Themes;
    using ViewModels;

    public class Startup
    {

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [STAThread]
        [DebuggerNonUserCode]
        public static void Main()
        {
            MyTheme.IsDark = false;
            Debug.WriteLine($"Loading Dummy Identifier for {Puchalapalli.WPF.Assets.Properties.Identifier.Name}");
            ReportedStatus.DEFAULT_STATUS = "Setting Folder Icon...";
            ReportedStatus.MAX_VERBOSITY = MainViewModel.MAX_VERBOSITY;
            LOGGER.LogLocation("Creating FoldMyIcons.App");
            App app;
            UI.App = app = new App();
            //LOGGER.LogLocation("Calling app.InitializeComponent()");
            //app.InitializeComponent();
            LOGGER.LogLocation("Calling app.Run()");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            //var mover = new InterVolumeMover(oldPath, newPath);
            //var result = mover.Move();
            //var isSuccess = result.IsSuccess();
            //TODO: Update NotifyIcon Code
            //App.NotifyIcon = new NotifyIcon();
            app.Run();
            //Debugger.Break();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            //TODO: Update NotifyIcon Code
            //App.NotifyIcon?.Dispose();
        }

        static void CurrentDomain_UnhandledException(object sender,
    UnhandledExceptionEventArgs e)
        {
            //TODO: Update NotifyIcon Code
            //App.NotifyIcon?.Dispose();
            // .... Remove Notification icon here
        }
    }
}