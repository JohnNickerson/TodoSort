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
        private string todoPath;

        public string TodoPath { get => Environment.ExpandEnvironmentVariables(todoPath); set => todoPath = value; }

        public static FolderSettings LoadFrom(string path)
        {
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
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
                return result;
            }
            else
            {
                return new FolderSettings { TodoPath = "todo.txt" };
            }
        }

        public static void SaveTo(string path, FolderSettings tosave)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(string.Format("TodoPath={0}", tosave.TodoPath));
            File.WriteAllText(path, output.ToString());
        }
    }
}
