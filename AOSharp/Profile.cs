using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using AOSharp.Bootstrap.IPC;
using EasyHook;
using Serilog;

namespace AOSharp
{
    public class Profile : INotifyPropertyChanged
    {
        public string Name { get; set; }

        public ObservableCollection<string> EnabledPlugins { get; set; }

        [JsonIgnore]
        public bool _isActive;

        [JsonIgnore]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        [JsonIgnore]
        public bool _isInjected;

        [JsonIgnore]
        public bool IsInjected
        {
            get => _isInjected;
            set
            {
                _isInjected = value;
                OnPropertyChanged("IsInjected");
            }
        }

        [JsonIgnore]
        public Process Process { get; set; }

        [JsonIgnore]
        private IPCClient _ipcClient;

        public event PropertyChangedEventHandler PropertyChanged;

        public Profile()
        {
            IsActive = false;
            EnabledPlugins = new ObservableCollection<string>();
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool Inject(IEnumerable<string> plugins)
        {
            try
            {
                // Get the full path to the bootstrap DLL in the same directory as the current executable
                string bootstrapPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "AOSharp.Bootstrap.dll");

                Log.Information("Attempting to inject bootstrap from: {BootstrapPath}", bootstrapPath);

                if (!System.IO.File.Exists(bootstrapPath))
                {
                    Log.Error("Bootstrap DLL not found at: {BootstrapPath}", bootstrapPath);
                    return false;
                }

                // Additional validation
                Log.Information("Target process ID: {ProcessId}", Process.Id);
                Log.Information("Target process name: {ProcessName}", Process.ProcessName);
                Log.Information("Target process main module: {MainModule}", Process.MainModule?.FileName ?? "Unknown");
                Log.Information("Current process architecture: {CurrentArchitecture}", Environment.Is64BitProcess ? "x64" : "x86");
                Log.Information("Operating system architecture: {OSArchitecture}", Environment.Is64BitOperatingSystem ? "x64" : "x86");

                // Check if target process is still running
                if (Process.HasExited)
                {
                    Log.Error("Target process {ProcessId} has exited", Process.Id);
                    return false;
                }

                // Check EasyHook dependencies
                string easyHookPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "EasyHook.dll");
                Log.Information("EasyHook.dll path: {EasyHookPath}, exists: {EasyHookExists}",
                    easyHookPath, System.IO.File.Exists(easyHookPath));

                // Use original injection method - EasyHook resolves the DLL from current directory
                RemoteHooking.Inject(Process.Id, "AOSharp.Bootstrap.dll", string.Empty, Process.Id.ToString(CultureInfo.InvariantCulture));
                Log.Information("Bootstrap injection successful for process {ProcessId}", Process.Id);

                IPCClient pipe = new IPCClient(Process.Id.ToString());
                Log.Information("Connecting to IPC pipe for process {ProcessId}", Process.Id);
                pipe.Connect();
                Log.Information("IPC pipe connected successfully");

                Log.Information("Sending plugin assemblies: {PluginCount} plugins", plugins.Count());
                pipe.Send(new LoadAssemblyMessage()
                {
                    Assemblies = plugins
                });
                Log.Information("Plugin assemblies sent successfully");

                pipe.OnDisconnected += (e) =>
                {
                    Log.Information("IPC pipe disconnected for process {ProcessId}", Process.Id);
                    _ipcClient = null;
                    IsInjected = false;
                };

                _ipcClient = pipe;
                IsInjected = true;
                Log.Information("Injection completed successfully for process {ProcessId}", Process.Id);

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to inject bootloader for process {ProcessId}. Exception details:", Process.Id);
                Log.Error("Exception Type: {ExceptionType}", e.GetType().FullName);
                Log.Error("Exception Message: {ExceptionMessage}", e.Message);
                if (e.InnerException != null)
                {
                    Log.Error("Inner Exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        e.InnerException.GetType().FullName, e.InnerException.Message);
                }
                Log.Error("Stack Trace: {StackTrace}", e.StackTrace);

                // Also use the crash logger for detailed reporting
                CrashLogger.LogException(e, "Injection Process");

                return false;
            }
        }

        public void Eject()
        {
            if (_ipcClient == null)
                return;

            //Breaking the pipe will cause the bootstrapper to unload itself and any loaded plugins
            _ipcClient.Disconnect();
        }
    }
}
