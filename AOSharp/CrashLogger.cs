using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Serilog;

namespace AOSharp
{
    /// <summary>
    /// Comprehensive crash logger for AOSharp application
    /// </summary>
    public static class CrashLogger
    {
        private static readonly string CrashLogDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory,
            "CrashLogs");

        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize the crash logger - call this early in application startup
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                // Ensure crash log directory exists
                Directory.CreateDirectory(CrashLogDirectory);

                // Handle unhandled exceptions in the main UI thread
                Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

                // Handle unhandled exceptions in background threads
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                // Handle task exceptions
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                _isInitialized = true;
                Log.Information("Crash logger initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize crash logger");
            }
        }

        /// <summary>
        /// Handle unhandled exceptions in the UI thread
        /// </summary>
        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                LogCrash(e.Exception, "UI Thread Exception");
                
                // Show user-friendly error dialog
                ShowCrashDialog(e.Exception);
                
                // Mark as handled to prevent application termination (optional)
                e.Handled = true;
            }
            catch (Exception logEx)
            {
                // If logging fails, at least try to write to a basic file
                WriteEmergencyLog(e.Exception, logEx);
            }
        }

        /// <summary>
        /// Handle unhandled exceptions in background threads
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception exception)
                {
                    LogCrash(exception, "Background Thread Exception");
                    
                    // For background thread exceptions, we can't prevent termination
                    if (e.IsTerminating)
                    {
                        Log.Fatal("Application is terminating due to unhandled exception");
                    }
                }
            }
            catch (Exception logEx)
            {
                WriteEmergencyLog(e.ExceptionObject as Exception, logEx);
            }
        }

        /// <summary>
        /// Handle unobserved task exceptions
        /// </summary>
        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                LogCrash(e.Exception, "Unobserved Task Exception");
                
                // Mark as observed to prevent application termination
                e.SetObserved();
            }
            catch (Exception logEx)
            {
                WriteEmergencyLog(e.Exception, logEx);
            }
        }

        /// <summary>
        /// Log detailed crash information
        /// </summary>
        private static void LogCrash(Exception exception, string crashType)
        {
            var timestamp = DateTime.Now;
            var crashId = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            // Log to Serilog
            Log.Fatal(exception, "CRASH DETECTED [{CrashType}] ID: {CrashId}", crashType, crashId);

            // Create detailed crash report file
            var crashFileName = $"crash_{timestamp:yyyyMMdd_HHmmss}_{crashId}.txt";
            var crashFilePath = Path.Combine(CrashLogDirectory, crashFileName);

            var crashReport = BuildCrashReport(exception, crashType, crashId, timestamp);
            
            File.WriteAllText(crashFilePath, crashReport, Encoding.UTF8);
            
            Log.Information("Crash report saved to: {CrashFilePath}", crashFilePath);
        }

        /// <summary>
        /// Build comprehensive crash report
        /// </summary>
        private static string BuildCrashReport(Exception exception, string crashType, string crashId, DateTime timestamp)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("AOSharp Crash Report");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"Crash ID: {crashId}");
            sb.AppendLine($"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Crash Type: {crashType}");
            sb.AppendLine();

            // Application information
            sb.AppendLine("APPLICATION INFORMATION:");
            sb.AppendLine("-".PadRight(40, '-'));
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                sb.AppendLine($"Application: {assembly.GetName().Name}");
                sb.AppendLine($"Version: {assembly.GetName().Version}");
                sb.AppendLine($"Location: {assembly.Location}");
                sb.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Failed to get application info: {ex.Message}");
            }
            sb.AppendLine();

            // System information
            sb.AppendLine("SYSTEM INFORMATION:");
            sb.AppendLine("-".PadRight(40, '-'));
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($"CLR Version: {Environment.Version}");
            sb.AppendLine($"Machine Name: {Environment.MachineName}");
            sb.AppendLine($"User: {Environment.UserName}");
            sb.AppendLine($"Processor Count: {Environment.ProcessorCount}");
            sb.AppendLine($"Working Set: {Environment.WorkingSet:N0} bytes");
            sb.AppendLine();

            // Exception details
            sb.AppendLine("EXCEPTION DETAILS:");
            sb.AppendLine("-".PadRight(40, '-'));
            AppendExceptionDetails(sb, exception, 0);

            return sb.ToString();
        }

        /// <summary>
        /// Recursively append exception details including inner exceptions
        /// </summary>
        private static void AppendExceptionDetails(StringBuilder sb, Exception exception, int level)
        {
            var indent = new string(' ', level * 2);
            
            sb.AppendLine($"{indent}Exception Type: {exception.GetType().FullName}");
            sb.AppendLine($"{indent}Message: {exception.Message}");
            sb.AppendLine($"{indent}Source: {exception.Source}");
            
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                sb.AppendLine($"{indent}Stack Trace:");
                var stackLines = exception.StackTrace.Split('\n');
                foreach (var line in stackLines)
                {
                    sb.AppendLine($"{indent}  {line.Trim()}");
                }
            }

            if (exception.Data.Count > 0)
            {
                sb.AppendLine($"{indent}Data:");
                foreach (var key in exception.Data.Keys)
                {
                    sb.AppendLine($"{indent}  {key}: {exception.Data[key]}");
                }
            }

            if (exception.InnerException != null)
            {
                sb.AppendLine($"{indent}Inner Exception:");
                AppendExceptionDetails(sb, exception.InnerException, level + 1);
            }
            
            sb.AppendLine();
        }

        /// <summary>
        /// Show user-friendly crash dialog
        /// </summary>
        private static void ShowCrashDialog(Exception exception)
        {
            try
            {
                var message = $"AOSharp has encountered an unexpected error and needs to close.\n\n" +
                             $"Error: {exception.Message}\n\n" +
                             $"A detailed crash report has been saved to the CrashLogs folder.\n" +
                             $"Please report this issue to the developers.";

                MessageBox.Show(message, "AOSharp - Unexpected Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // If we can't show a dialog, just ignore it
            }
        }

        /// <summary>
        /// Emergency logging when normal logging fails
        /// </summary>
        private static void WriteEmergencyLog(Exception originalException, Exception loggingException)
        {
            try
            {
                var emergencyFile = Path.Combine(CrashLogDirectory, $"emergency_crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var content = $"EMERGENCY CRASH LOG\n" +
                             $"Timestamp: {DateTime.Now}\n" +
                             $"Original Exception: {originalException}\n" +
                             $"Logging Exception: {loggingException}\n";
                
                File.WriteAllText(emergencyFile, content);
            }
            catch
            {
                // If even emergency logging fails, there's nothing more we can do
            }
        }

        /// <summary>
        /// Manually log an exception (for use in try-catch blocks)
        /// </summary>
        public static void LogException(Exception exception, string context = "Manual Log")
        {
            try
            {
                LogCrash(exception, context);
            }
            catch (Exception logEx)
            {
                WriteEmergencyLog(exception, logEx);
            }
        }
    }
}
