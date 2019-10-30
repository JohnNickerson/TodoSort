using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    public class TextFolderImporter : IImporter
    {
        public ActionItem[] GetAllItems()
        {
            var result = new List<ActionItem>();
            foreach (var f in Directory.GetFiles(Folder, "*.txt", SearchOption.AllDirectories))
            {
                try
                {
                    var m = new ActionItemDiskMapper(f).LoadAll().Where(a => !string.IsNullOrWhiteSpace(a.Title));
                    foreach (var h in m)
                    {
                        if (string.IsNullOrEmpty(h.ImportHash))
                        {
                            h.ImportHash = h.GenerateHash();
                            h.LastModified = DateTime.Now;
                        }
                    }

                    foreach (var i in m.Where(a => string.IsNullOrEmpty(a.Context)))
                    {
                        i.Context = "inbox";
                    }

                    result.AddRange(m);
                }
                catch
                {
                    // ignore
                }
            }

            return result.ToArray();
        }

        public string Folder { get; set; }
    }
}
