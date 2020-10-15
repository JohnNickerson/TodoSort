using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AssimilationSoftware.Maroon.Interfaces;
using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Data
{
    public class ActionItemJsonMapper : IMapper<ActionItem>
    {
        private readonly string _filename;

        public ActionItemJsonMapper(string filename)
        {
            _filename = filename;
        }

        public ActionItem Load(Guid id)
        {
            var allItems = LoadAll();
            return allItems.FirstOrDefault(i => i.ID == id);
        }

        public IEnumerable<ActionItem> LoadAll()
        {
            var allItems = JsonSerializer.Deserialize(File.ReadAllText(_filename), typeof(List<ActionItem>));
            return allItems as IEnumerable<ActionItem>;
        }

        public void Save(ActionItem item)
        {
            var allItems = LoadAll().ToList();
            allItems.Add(item);
            SaveAll(allItems);
        }

        public void SaveAll(IEnumerable<ActionItem> items)
        {
            File.WriteAllText(_filename, JsonSerializer.Serialize(items, typeof(List<ActionItem>)));
        }

        public void Delete(ActionItem item)
        {
            var allItems = LoadAll().Where(i => i.ID != item.ID).ToList();
            SaveAll(allItems);
        }
    }
}
