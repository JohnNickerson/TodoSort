using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.Core
{
    public class ViewModel
    {
        #region Fields
        List<ActionItem> todo_items;
        List<ActionItem> someday_items;
        List<ActionItem> done_items;

        IActionItemMapper todo_mapper;
        IActionItemMapper someday_mapper;
        IActionItemMapper done_mapper;

        // Track whether changes have been made to the "someday" file, to avoid rewriting it if possible.
        bool todo_changes;
        bool someday_changes;
        bool done_changes;

        string _searchTerm;
        #endregion

        public ViewModel(IActionItemMapper todo, IActionItemMapper done, IActionItemMapper someday)
        {
            todo_mapper = todo;
            done_mapper = done;
            someday_mapper = someday;

            todo_items = todo.LoadAll();
            someday_items = someday.LoadAll();
            done_items = done.LoadAll();

            todo_changes = false;
            someday_changes = false;
            done_changes = false;

            _searchTerm = string.Empty;
        }

        #region Events
        /// <summary>
        /// An event that indicates a property has changed value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the PropertyChanged event with given arguments.
        /// </summary>
        /// <param name="e"></param>
        public void RaisePropertyChanged(params string[] propnames)
        {
            foreach (string prop in propnames)
            {
                var e = new PropertyChangedEventArgs(prop);
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, e);
                }
            }
        }
        #endregion

        #region Methods
        public IEnumerable<ActionItem> GetContextItems(string context)
        {
            return from m in todo_items where m.Context.EndsWith(context.ToLower()) select m;
        }

        public void Save()
        {
            if (todo_changes)
            {
                todo_mapper.SaveAll(todo_items, SortType.Priority);
            }
            if (someday_changes)
            {
                someday_mapper.SaveAll(someday_items, SortType.Alphanumeric);
                someday_changes = false;
            }
            if (done_changes)
            {
                done_mapper.SaveAll(done_items, SortType.Time);
                done_changes = false;
            }
        }

        public void AddItem(ActionItem next)
        {
            todo_items.Add(next);
            todo_changes = true;
        }

        /// <summary>
        /// Mark an item as done.
        /// </summary>
        /// <param name="doneitem">The item to mark as done.</param>
        public void MarkDone(params ActionItem[] doneitems)
        {
            foreach (ActionItem doneitem in doneitems)
            {
                doneitem.DoneDate = DateTime.Now;
                doneitem.Context = string.Format("{0:yyyy-MM-dd}", doneitem.DoneDate);
                done_items.Add(doneitem);
                todo_items.Remove(doneitem);
                done_changes = true;
                todo_changes = true;
            }
        }

        /// <summary>
        /// Moves a list of items to the Someday list.
        /// </summary>
        /// <param name="selected"></param>
        public void Defer(params ActionItem[] selected)
        {
            List<ActionItem> to_defer = new List<ActionItem>(selected);
            while (to_defer.Count > 0)
            {
                ActionItem i = to_defer[0];
                i.Context = "someday";
                someday_items.Add(i);
                todo_items.Remove(i);
                someday_changes = true;
                todo_changes = true;

                //TODO: If this item was the project for another, defer that one too.
                //to_defer.AddRange(from a in todo_items where a.Project == i select a);
            }
        }
        
        /// <summary>
        /// Moves an item from the Someday list to the main list.
        /// </summary>
        /// <param name="actionItem"></param>
        public void Undefer(string context, params ActionItem[] selection)
        {
            List<ActionItem> to_undefer = new List<ActionItem>(selection);
            while (to_undefer.Count > 0)
            {
                ActionItem i = to_undefer[0];
                i.Context = context;
                todo_items.Add(i);
                someday_items.Remove(i);
                someday_changes = true;
                todo_changes = true;

                //TODO: If this item was the project for another, undefer that one, too.
                //to_undefer.AddRange(from a in someday_items where a.Project == i select a);
            }
        }

        public void Delete(params ActionItem[] selection)
        {
            foreach (ActionItem i in selection)
            {
                todo_items.Remove(i);
                todo_changes = true;
            }
        }

        /// <summary>
        /// Finds items in the main list that match a given search term.
        /// </summary>
        /// <param name="search">The search term to look for.</param>
        /// <returns>A list of matching items.</returns>
        public List<ActionItem> Search(string search)
        {
            return (from i in todo_items
                    where i.Title.ToLower().Contains(search.ToLower())
                        || string.Join(Environment.NewLine, i.Notes).ToLower().Contains(search.ToLower())
                        || string.Join(Environment.NewLine, (from k in i.Tags select k.Value)).ToLower().Contains(search.ToLower())
                    select i).ToList();
        }

        public List<ActionItem> GetProjectChildren(ActionItem actionItem)
        {
            return (from i in todo_items where i.Project == actionItem select i).ToList();
        }

        public List<ActionItem> GetTickleDueItems()
        {
            return (from i in someday_items where i.TickleDate <= DateTime.Now select i).ToList();
        }
        #endregion

        #region Properties

        public List<ActionItem> SomedayItems
        {
            get
            {
                return someday_items;
            }
        }

        public string SearchTerm
        {
            get
            {
                return _searchTerm;
            }
            set
            {
                _searchTerm = value;
                RaisePropertyChanged("SearchTerm", "SearchResults");
            }
        }

        public List<ActionItem> SearchResults
        {
            get
            {
                return this.Search(SearchTerm);
            }
        }
        #endregion

        public IEnumerable<string> GetContextNames(params string[] exclude)
        {
            return (from i in todo_items select i.Context).Distinct().Except(exclude);
        }

        public void ResetPriorityParents()
        {
            foreach (var i in todo_items)
            {
                i.PriorityParent = null;
                todo_changes = true;
            }
        }

        public void SetTag(ActionItem selected, string tagname, string value)
        {
            selected.Tags[tagname] = value;
            todo_changes = true;
        }

        public void SetParent(ActionItem child, ActionItem parent)
        {
            child.PriorityParent = parent;
            todo_changes = true;
        }
    }
}
