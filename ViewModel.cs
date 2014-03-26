using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort
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
        bool someday_changes;
        bool done_changes;
        #endregion

        public ViewModel(IActionItemMapper todo, IActionItemMapper done, IActionItemMapper someday)
        {
            todo_mapper = todo;
            done_mapper = done;
            someday_mapper = someday;

            todo_items = todo.LoadAll();
            someday_items = someday.LoadAll();
            done_items = done.LoadAll();
            
            someday_changes = false;
            done_changes = false;
        }

        #region Methods
        internal IEnumerable<ActionItem> GetContextItems(string context)
        {
            return from m in todo_items where m.Context.EndsWith(context) select m;
        }

        internal void Save()
        {
            todo_mapper.SaveAll((from i in todo_items orderby i.PriorityDepth select i).ToList());
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

        internal void AddItem(ActionItem next)
        {
            todo_items.Add(next);
        }

        /// <summary>
        /// Mark an item as done.
        /// </summary>
        /// <param name="doneitem">The item to mark as done.</param>
        internal void MarkDone(params ActionItem[] doneitems)
        {
            foreach (ActionItem doneitem in doneitems)
            {
                doneitem.DoneDate = DateTime.Now;
                doneitem.Context = string.Format("{0:yyyy-MM-dd}", doneitem.DoneDate);
                done_items.Add(doneitem);
                todo_items.Remove(doneitem);
                done_changes = true;
            }
        }

        /// <summary>
        /// Moves a list of items to the Someday list.
        /// </summary>
        /// <param name="selected"></param>
        internal void Defer(params ActionItem[] selected)
        {
            foreach (ActionItem i in selected)
            {
                i.Context = "someday";
                someday_items.Add(i);
                todo_items.Remove(i);
                someday_changes = true;
            }
        }
        
        /// <summary>
        /// Moves an item from the Someday list to the main list.
        /// </summary>
        /// <param name="actionItem"></param>
        internal void Undefer(params ActionItem[] selection)
        {
            foreach (ActionItem i in selection)
            {
                i.Context = "inbox";
                todo_items.Add(i);
                someday_items.Remove(i);
                someday_changes = true;
            }
        }

        internal void Delete(params ActionItem[] selection)
        {
            foreach (ActionItem i in selection)
            {
                todo_items.Remove(i);
            }
        }

        /// <summary>
        /// Finds items in the main list that match a given search term.
        /// </summary>
        /// <param name="search">The search term to look for.</param>
        /// <returns>A list of matching items.</returns>
        internal List<ActionItem> Search(string search)
        {
            return (from i in todo_items where i.Title.ToLower().Contains(search.ToLower()) select i).ToList();
        }

        internal List<ActionItem> GetProjectChildren(ActionItem actionItem)
        {
            return (from i in todo_items where i.Project == actionItem select i).ToList();
        }

        internal List<ActionItem> GetTickleDueItems()
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
        #endregion

        internal IEnumerable<string> GetContextNames(params string[] exclude)
        {
            return (from i in todo_items select i.Context).Distinct().Except(exclude);
        }

        internal void ResetPriorityParents()
        {
            foreach (var i in todo_items)
            {
                i.PriorityParent = null;
            }
        }

        internal void SetTag(ActionItem selected, string tagname, string value)
        {
            selected.Tags[tagname] = value;
        }
    }
}
