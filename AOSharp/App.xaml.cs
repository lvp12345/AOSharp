using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Serilog;

namespace AOSharp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Initialize crash logger as early as possible
            CrashLogger.Initialize();

            Log.Information("AOSharp application starting up");

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("AOSharp application shutting down");
            Log.CloseAndFlush();

            base.OnExit(e);
        }
    }
}
