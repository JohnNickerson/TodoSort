namespace AssimilationSoftware.TodoSort.CLI
{
    public class FolderSettings
    {
        private string? todoPath;

        public string? TodoPath { get => Environment.ExpandEnvironmentVariables(todoPath ?? string.Empty); set => todoPath = value; }

        public static FolderSettings LoadFrom(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var lines = File.ReadAllLines(path);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<FolderSettings>(string.Join(' ', lines)) ?? new();
                }
                catch
                {
                    var result = new FolderSettings();
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
