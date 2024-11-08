using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI
{
    public class FolderSettings
    {
        private string? todoPath;

        public string? TodoPath { get => Environment.ExpandEnvironmentVariables(todoPath ?? string.Empty); set => todoPath = value; }
        public string? AccessCode { get; set; }

        public static FolderSettings LoadFrom(string path)
        {
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                // New: JSON serialisation mode
                try
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<FolderSettings>(string.Join(' ', lines)) ?? new();
                }
                catch
                {
                    // Backwards compatibility: old INI format.
                    var result = new FolderSettings();
                    foreach (string line in lines)
                    {
                        var setting = line.Split(new char[] { '=' }, 2);
                        if (setting.Length == 2)
                        {
                            switch (setting[0].Trim().ToLower())
                            {
                                case "todopath":
                                    result.TodoPath = setting[1];
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    SaveTo(path, result);
                    return result;
                }
            }
            else
            {
                return new FolderSettings { TodoPath = "todo.txt" };
            }
        }

        public static void SaveTo(string path, FolderSettings settings)
        {
            File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
