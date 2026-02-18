using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AssimilationSoftware.TodoSort.WpfGui.Properties {
    
    
    internal sealed partial class Settings {
        
        private static Settings defaultInstance = new Settings();
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        static Settings()
        {
            Reload();
        }

        public static void Reload()
        {
            try
            {
                string settingsFile = "appsettings.json";
                string machineFile = $"appsettings.{Environment.MachineName}.json";
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(settingsFile, optional: true, reloadOnChange: true)
                    .AddJsonFile(machineFile, optional: true, reloadOnChange: true);
                Debug.WriteLine("Builder initialised.");

                IConfigurationRoot configuration = builder.Build();
                Debug.WriteLine("Configuration built.");

                defaultInstance = new();
                configuration.GetSection("Default").Bind(defaultInstance);
                Debug.WriteLine("Bound to singleton instance.");
            }
            catch (System.Exception ex)
            {
                // Default configuration.
                Console.WriteLine($"Configuration file error: {ex.Message}. Using default config.");
                defaultInstance = new();
            }
        }

        public void Save()
        {
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), $"appsettings.{Environment.MachineName}.json");
            System.Text.StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine("  \"Default\":");
            json.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(Default, Newtonsoft.Json.Formatting.Indented));
            json.AppendLine("}");
            File.WriteAllText(jsonPath, json.ToString());
        }

        private string _todo;
        public string Todo {
            get {
                return _todo;
            }
            set {
                _todo = value;
            }
        }
        
        private bool _reconfigure;
        public bool Reconfigure {
            get {
                return _reconfigure;
            }
            set {
                _reconfigure = value;
            }
        }
        
        private string[] _recentFiles = new string[] { } ;
        public string[] RecentFiles {
            get {
                return _recentFiles;
            }
            set {
                _recentFiles = value;
            }
        }
        
        private bool _maskNsfwItems;
        public bool MaskNsfwItems {
            get {
                return _maskNsfwItems;
            }
            set {
                _maskNsfwItems = value;
            }
        }
    }
}
