using AssimilationSoftware.Maroon.Mappers.Text;
using System.Collections.Generic;

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
