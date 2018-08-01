using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    public class TextImporter : IImporter
    {
        public string Filename { get; set; }

        public ActionItem[] GetAllItems()
        {
            var m = new PimData.Mappers.ActionItemDiskMapper(Filename).LoadAll();
            foreach (var i in m.Where(a => string.IsNullOrEmpty(a.Context)))
            {
                i.Context = "inbox";
            }
            return m.ToArray();
        }
    }
}
