using System;
using System.IO;
using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
using System.Linq;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    public class TextImporter : IImporter
    {
        public string Filename { get; set; }

        public ActionItem[] GetAllItems()
        {
            var m = new ActionItemDiskMapper(Filename).LoadAll();
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
            return m.ToArray();
        }

        public bool IsValid => File.Exists(Filename);
    }
}
