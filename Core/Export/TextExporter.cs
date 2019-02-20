using AssimilationSoftware.Maroon.Mappers.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Export
{
    public class TextExporter : IExporter
    {
        public string Filename { get; set; }

        public void Export(List<Maroon.Model.ActionItem> items)
        {
            var m = new ActionItemDiskMapper(Filename);
            m.SaveAll(items);
        }
    }
}
