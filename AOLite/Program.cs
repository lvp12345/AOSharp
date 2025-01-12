using Serilog.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Newtonsoft.Json;

namespace AOLite
{
    internal class Program
    {
        private class CommandLineOptions
        {
            [Value(index: 0, Required = true, HelpText = "Path to AO folder.")]
            public string AOPath { get; set; }

            [Value(index: 1, Required = true, HelpText = "Config file to be used.")]
            public string Path { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(options => Run(options));
        }

        private static void Run(CommandLineOptions options)
        {
            if (!File.Exists(options.Path))
            {
                Console.WriteLine($"Config file not found at '{options.Path}', read the instructions.");
                Console.ReadLine();
                return;
            }

            Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(options.Path));
            Logger logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger();

            Client.Start(new ClientConfig
            {
                Credentials = new Credentials(config.Username, config.Password),
                CharacterName = config.Character,
                Dimension = Dimension.RubiKa,
                AOPath = options.AOPath,
                Plugins = config.Plugins
            }, logger);

            Console.ReadLine();
        }
    }

    public class Config
    {
        public string Username;
        public string Password;
        public string Character;
        public List<string> Plugins;
    }
}