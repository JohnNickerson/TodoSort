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
        List<ActionItem> todolist;
        List<ActionItem> someday_items;
        List<ActionItem> done_items;

        IActionItemMapper todo_mapper;
        IActionItemMapper someday_mapper;
        IActionItemMapper done_mapper;

        // Track whether changes have been made to the "someday" file, to avoid rewriting it if possible.
        bool someday_changes;
        bool done_changes;

        public ViewModel(IActionItemMapper todo, IActionItemMapper done, IActionItemMapper someday)
        {
            todo_mapper = todo;
            done_mapper = done;
            someday_mapper = someday;

            todolist = todo.LoadAll();
            someday_items = someday.LoadAll();
            done_items = done.LoadAll();
            
            someday_changes = false;
            done_changes = false;
        }

        internal IEnumerable<ActionItem> GetContext(string context)
        {
            return from m in todolist where m.Context.EndsWith(context) select m;
        }

        internal void Save()
        {
            todo_mapper.SaveAll(todolist);
            if (someday_changes)
            {
                someday_mapper.SaveAll(someday_items);
                someday_changes = false;
            }
            if (done_changes)
            {
                done_mapper.SaveAll(done_items);
                done_changes = false;
            }
        }

        internal void AddItem(ActionItem next)
        {
            todolist.Add(next);
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
                done_items.Add(doneitem);
                todolist.Remove(doneitem);
                done_changes = true;
            }
        }

        internal void Defer(params ActionItem[] selected)
        {
            foreach (ActionItem i in selected)
            {
                someday_items.Add(i);
                todolist.Remove(i);
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
                todolist.Add(i);
                someday_items.Remove(i);
                someday_changes = true;
            }
        }

        internal void Delete(params ActionItem[] selection)
        {
            foreach (ActionItem i in selection)
            {
                todolist.Remove(i);
            }
        }

        internal List<ActionItem> Search(string search)
        {
            return (from i in todolist where i.Title.ToLower().Contains(search.ToLower()) select i).ToList();
        }

        internal List<ActionItem> GetProjectChildren(ActionItem actionItem)
        {
            return (from i in todolist where i.Project == actionItem select i).ToList();
        }

        #region Properties

        public List<ActionItem> SomedayItems
        {
            get
            {
                return someday_items;
            }
        }
        #endregion
    }
}
