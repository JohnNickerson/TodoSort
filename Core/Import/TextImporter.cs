using AssimilationSoftware.PimData.Mappers;
using AssimilationSoftware.PimData.Model;
using System.Linq;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    public class TextImporter : IImporter
    {
        public string Filename { get; set; }

        public ActionItem[] GetAllItems()
        {
            var m = new ActionItemDiskMapper(Filename).LoadAll();
			foreach (var i in m.Where(a => string.IsNullOrEmpty(a.Context)))
			{
				i.Context = "inbox";
			}
            return m.ToArray();
        }
    }
}
