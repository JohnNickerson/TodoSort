
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;

namespace AssimilationSoftware.TodoSort.Core.Mappers
{
    public class TodoTxtFileMapper : IActionItemMapper
    {
        private string _filename;
        private List<ActionItem> _items;

        public TodoTxtFileMapper(string filename)
        {
            _filename = filename;
        }

        /// <summary>
        /// Reads a formatted file and returns it as a list of Items.
        /// </summary>
        /// <param name="filename">The full path to the file to load.</param>
        /// <returns>A list of items as represented by the file.</returns>
        public List<ActionItem> Deserialise(string filename)
        {
            return LoadAll();
        }

        /// <summary>
        /// Writes a list of items out to a file.
        /// </summary>
        /// <param name="filename">The full path of the file to write.</param>
        /// <param name="items">The items to write out.</param>
        /// <param name="includeinbox">True to add an "@inbox" context at the end, false to leave it out unless already present.</param>
        public void Serialise(string filename, List<ActionItem> items)
        {
            SaveAll(items);
        }

        public ActionItem Load(Guid id)
        {
            _items = LoadAll();
            var filtered = from i in _items where i.ID == id select i;
            if (filtered.Count() > 0)
            {
                return filtered.First();
            }
            else
            {
                return null;
            }
        }

        public List<ActionItem> LoadAll()
        {
            string[] items = (File.Exists(_filename) ? File.ReadAllLines(_filename) : new string[] { });
            var priorityparents = new Dictionary<ActionItem, Guid>();
            var projects = new Dictionary<ActionItem, Guid>();

            _items = new List<ActionItem>();
            string context = string.Empty;
            ActionItem curitem = new ActionItem(context, "(item out of order)");
            for (int x = 0; x < items.Length; x++)
            {
                if (items[x].StartsWith("@"))
                {
                    // New context.
                    context = items[x].Substring(1);
                }
                else if (items[x].Trim().StartsWith("#"))
                {
                    // Name.
                    string[] ts = items[x].Trim().Split(':');
                    if (ts.Length > 1)
                    {
                        string tag = ts[0].Replace("#", string.Empty).Trim();
                        // Process "special" tags like parent item IDs.
                        switch (tag.ToLower())
                        {
                            case "done-date":
                                curitem.DoneDate = DateTime.Parse(ts[1]);
                                break;
                            case "priority-parent":
                                priorityparents[curitem] = Guid.Parse(ts[1]);
                                break;
                            case "project":
                                projects[curitem] = Guid.Parse(ts[1]);
                                break;
                            case "id":
                                curitem.ID = Guid.Parse(ts[1]);
                                break;
                            default:
                                curitem.Tags.Add(tag, ts[1].Trim());
                                break;
                        }
                    }
                    else
                    {
                        // Malformed. Just treat it as a note.
                        curitem.Notes.Add(items[x].Trim());
                    }
                }
                else if (items[x].Trim().StartsWith("-"))
                {
                    // Note.
                    curitem.Notes.Add(items[x].Trim());
                }
                else
                {
                    // New item.
                    curitem = new ActionItem(context, items[x].Trim());
                    _items.Add(curitem);
                }
            }

            // Process references.
            for (int x = 0; x < _items.Count; x++)
            {
                if (projects.ContainsKey(_items[x]))
                {
                    _items[x].Project = (from i in _items where i.ID == projects[_items[x]] select i).FirstOrDefault();
                }

                if (priorityparents.ContainsKey(_items[x]))
                {
                    _items[x].PriorityParent = (from i in _items where i.ID == priorityparents[_items[x]] select i).FirstOrDefault();
                }
            }

            return _items;
        }

        public void Save(ActionItem item)
        {
            if (_items == null)
            {
                _items = LoadAll();
            }
            if (!_items.Contains(item))
            {
                _items.Add(item);
            }
            SaveAll(_items);
        }

        public void SaveAll(List<ActionItem> items)
        {
            StringBuilder file = new StringBuilder();
            foreach (string b in (from s in items orderby s.Context select s.Context).Distinct())
            {
                // Write out block.
                file.AppendLine(string.Format("@{0}", b));
                foreach (ActionItem i in (from t in items where t.Context == b select t))
                {
                    file.AppendLine(string.Format("\t{0}", i.Title.Trim()));
                    foreach (string note in i.Notes)
                    {
                        file.AppendLine(string.Format("\t\t{0}", note.Trim()));
                    }
                    foreach (string key in i.Tags.Keys)
                    {
                        file.AppendLine(string.Format("\t\t#{0}:{1}", key, i.Tags[key]));
                    }
                    if (i.DoneDate.HasValue)
                    {
                        file.AppendLine(string.Format("\t\t#done-date:{0:yyyy-MM-dd}", i.DoneDate.Value));
                    }
                    file.AppendLine(string.Format("\t\t#id:{0}", i.ID));
                    if (i.Project != null)
                    {
                        file.AppendLine(string.Format("\t\t#project:{0}", i.Project.ID));
                    }
                    if (i.PriorityParent != null)
                    {
                        file.AppendLine(string.Format("\t\t#priority-parent:{0}", i.PriorityParent.ID));
                    }
                }
            }

            File.WriteAllText(_filename, file.ToString());
        }

        public void SaveAll(List<ActionItem> items, SortType sort)
        {
            // Sort the list according to the sort type.
            List<ActionItem> sortedlist;
            switch (sort)
            {
                case SortType.Time:
                    sortedlist = (from i in items orderby i.DoneDate select i).ToList();
                    break;
                case SortType.Priority:
                    var unsorted = (from t in items select t).ToList();
                    sortedlist = new List<ActionItem>();
                    while (unsorted.Count > 0)
                    {
                        // Find every orphan node (or a random node from a circular reference).
                        var parents = new List<ActionItem>();
                        for (int i = 0; i < unsorted.Count; i++)
                        {
                            var elem = unsorted[i];
                            // if the item has a parent still to be recorded and we haven't seen that parent already (to avoid loops)...
                            while (elem.PriorityParent != null && unsorted.Contains(elem.PriorityParent) && !parents.Contains(elem))
                            {
                                elem = elem.PriorityParent;
                            }
                            if (!parents.Contains(elem))
                            {
                                parents.Add(elem);
                            }
                        }
                        sortedlist.AddRange(parents);
                        foreach (var p in parents)
                        {
                            unsorted.Remove(p);
                        }
                    }
                    break;
                case SortType.Alphanumeric:
                default:
                    sortedlist = (from t in items orderby t.Title select t).ToList();
                    break;
            }

            SaveAll(sortedlist);
        }
    }
}
