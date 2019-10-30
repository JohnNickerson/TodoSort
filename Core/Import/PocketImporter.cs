using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AssimilationSoftware.Maroon.Mappers.Csv;
using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    /// <summary>
    /// A class to import TodoSort items from HTML, specifically exported from Pocket (https://getpocket.com/)
    /// </summary>
    public class PocketImporter : IImporter
    {
        public string Filename { get; set; }

        public ActionItem[] GetAllItems()
        {
            var result = new List<ActionItem>();
            // Open the file.
            if (!File.Exists(Filename))
            {
                Debug.WriteLine("File does not exist.");
                return new ActionItem[] { };
            }
            var lines = File.ReadLines(Filename);
            // Return every "<ul><li><a ...>" line before "<h1>Read Archive</h1>"
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("<h1>Read Archive</h1>"))
                {
                    break;
                }
                else if (line.Trim().StartsWith("<li>"))
                {
                    // Decode and add the item.
                    // eg <li><a href="https://boingboing.net/2019/10/25/watch-these-incredible-and-utt.html" time_added="1572133736" tags="">Watch these incredible and utterly dangerous moves banned from figure skati</a></li>
                    var el = new XmlDocument();
                    el.LoadXml(line);
                    var item = new ActionItem
                    {
                        Context = "pocket",
                        ID = Guid.NewGuid(),
                        ImportHash = line.CalculateHash(),
                        IsDeleted = false,
                        LastModified = DateTime.Now,
                        RevisionGuid = Guid.NewGuid(),
                        Tags = new Dictionary<string, string>(),
                        Title = el.FirstChild.FirstChild.InnerText
                    };
                    if (el.FirstChild.FirstChild.Attributes != null) item.Tags.Add("url", el.FirstChild.FirstChild.Attributes["href"].Value);
                    result.Add(item);
                }
            }

            return result.ToArray();
        }
    }
}
