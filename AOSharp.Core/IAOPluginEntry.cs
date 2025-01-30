using Serilog;
using Serilog.Core;
using Logger = Serilog.Core.Logger;
using System;
using AOSharp.Core.Logging;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using SmokeLounge.AOtomation.Messaging.Exceptions;
using System.Reflection;
using Serilog.Events;
using System.IO;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;

namespace AOSharp.Core
{
    public interface IAOPluginEntry
    {
        void Init(string pluginDir);
        void Teardown();
    }

    public abstract class AOPluginEntry : IAOPluginEntry
    {
        protected DirectoryInfo PluginDataDirectory { get; private set; }
        protected FileInfo LogFile { get; private set; }
        protected FileInfo GlobalSettingsFile { get; private set; }
        protected FileInfo PlayerSettingsFile { get; private set; }
        protected string PluginDirectory { get; private set; }
        public Logger Logger;


        private string _verboseLogFormat = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {PluginName} ({CharacterName}): {Message:lj}{NewLine}{Exception}";
        private string _standardLogFormat = "[{Timestamp:HH:mm:ss}] {PluginName}: {Message:lj}{NewLine}{Exception}";
        private LoggingLevelSwitch _chatLoggingLevelSwitch;
        private LoggingLevelSwitch _fileLoggingLevelSwitch;
        private LoggingLevelSwitch _debugLoggingLevelSwitch;
        private string pluginName;
        private string characterName;

        protected LogLevel ChatLogLevel
        {
            get { return (LogLevel)_chatLoggingLevelSwitch.MinimumLevel; }
            set { _chatLoggingLevelSwitch.MinimumLevel = (LogEventLevel)value; }
        }

        protected LogLevel FileLogLevel
        {
            get { return (LogLevel)_fileLoggingLevelSwitch.MinimumLevel; }
            set { _fileLoggingLevelSwitch.MinimumLevel = (LogEventLevel)value; }
        }

        protected LogLevel DebugLogLevel
        {
            get { return (LogLevel)_debugLoggingLevelSwitch.MinimumLevel; }
            set { _debugLoggingLevelSwitch.MinimumLevel = (LogEventLevel)value; }
        }

        public void Init(string pluginDir)
        {
            PluginDirectory = pluginDir;
            pluginName = GetType().Name;
            characterName = DynelManager.LocalPlayer.Name;
            SetupDirectoryStructure();
            SetupLogging();
            LoadIPCMessages();
            Run();
        }

        private void SetupLogging()
        {
            _chatLoggingLevelSwitch = new LoggingLevelSwitch();
            _fileLoggingLevelSwitch = new LoggingLevelSwitch();
            _debugLoggingLevelSwitch = new LoggingLevelSwitch();

            var loggerConfig = new LoggerConfiguration();
            OnConfiguringLogger(loggerConfig);
            Logger = loggerConfig.CreateLogger();
        }

        private void SetupDirectoryStructure()
        {
            PluginDataDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AOSharp", pluginName));
            LogFile = new FileInfo(Path.Combine(PluginDataDirectory.FullName, characterName, "log.txt"));
            GlobalSettingsFile = new FileInfo(Path.Combine(PluginDataDirectory.FullName, "GlobalSettings.json"));
            PlayerSettingsFile = new FileInfo(Path.Combine(PluginDataDirectory.FullName, characterName, "PlayerSettings.json"));

            if (!PluginDataDirectory.Exists)
                PluginDataDirectory.Create();

            if (!LogFile.Directory.Exists)
                LogFile.Directory.Create();

            if (!GlobalSettingsFile.Directory.Exists)
                GlobalSettingsFile.Directory.Create();

            if (!PlayerSettingsFile.Directory.Exists)
                PlayerSettingsFile.Directory.Create();
        }

        public virtual void Run()
        {
            Run(PluginDirectory);
        }

        [Obsolete]
        public virtual void Run(string pluginDir)
        {

        }

        public virtual void Reset()
        {
        }

        public virtual void Teardown() 
        {
        }

        protected virtual void OnConfiguringLogger(LoggerConfiguration loggerConfig)
        {
            loggerConfig
                .Enrich.WithProperty("PluginName", pluginName)
                .Enrich.WithProperty("CharacterName", characterName)
                .WriteTo.Debug(outputTemplate: _verboseLogFormat, levelSwitch: _debugLoggingLevelSwitch)
                .WriteTo.File(LogFile.FullName, levelSwitch: _fileLoggingLevelSwitch, outputTemplate: _verboseLogFormat)
                .MinimumLevel.Verbose();

            if (Game.IsAOLite)
                loggerConfig.WriteTo.Console(levelSwitch: _chatLoggingLevelSwitch, outputTemplate: _verboseLogFormat);
            else
                loggerConfig.WriteTo.Chat(levelSwitch: _chatLoggingLevelSwitch, outputTemplate: _standardLogFormat);
        }

        private void LoadIPCMessages()
        {
            try
            {
                IPCChannel.LoadMessages(GetType().Assembly);
            }
            catch (ContractIdCollisionException e)
            {
                Logger.Error(e.Message);
            }
        }
    }
}
