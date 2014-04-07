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
        public string TodoPath { get; set; }
        public string DonePath { get; set; }
        public string SomedayPath { get; set; }

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
                            case "donepath":
                                result.DonePath = setting[1];
                                break;
                            case "somedaypath":
                                result.SomedayPath = setting[1];
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
                return new FolderSettings { TodoPath = "todo.txt", SomedayPath = "someday.txt", DonePath = "done.txt" };
            }
        }

        public static void SaveTo(string path, FolderSettings tosave)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(string.Format("TodoPath={0}", tosave.TodoPath));
            if (tosave.SomedayPath != null)
            {
                output.AppendLine(string.Format("SomedayPath={0}", tosave.SomedayPath));
            }
            if (tosave.DonePath != null)
            {
                output.AppendLine(string.Format("DonePath={0}", tosave.DonePath));
            }
            File.WriteAllText(path, output.ToString());
        }
    }
}
