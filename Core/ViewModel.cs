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
    public class ViewModel : INotifyPropertyChanged
    {
        #region Fields
        List<ActionItem> todo_items;
        List<ActionItem> someday_items;
        List<ActionItem> done_items;

        IPimDataMapper<ActionItem> todo_mapper;
        IPimDataMapper<ActionItem> someday_mapper;
        IPimDataMapper<ActionItem> done_mapper;

        // Track whether changes have been made to the "someday" file, to avoid rewriting it if possible.
        bool todo_changes;
        bool someday_changes;
        bool done_changes;

        string _searchTerm;
        private bool showHeadOnly = true;
        #endregion

        public ViewModel(IPimDataMapper<ActionItem> todo, IPimDataMapper<ActionItem> done, IPimDataMapper<ActionItem> someday)
        {
            todo_mapper = todo;
            done_mapper = done;
            someday_mapper = someday;

            todo_items = todo.LoadAll();
            if (someday_mapper != null)
            {
                someday_items = someday.LoadAll();
            }
            if (done_mapper != null)
            {
                done_items = done.LoadAll();
            }

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
            if (PropertyChanged != null)
            {
                foreach (string prop in propnames)
                {
                    var e = new PropertyChangedEventArgs(prop);
                    PropertyChanged(this, e);
                }
            }
        }
        #endregion

        #region Properties

        public List<ActionItem> SomedayItems
        {
            get
            {
                if (someday_mapper != null)
                {
                    return someday_items;
                }
                else
                {
                    return GetContextItems("someday").ToList();
                }
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

        public ActionItem[] SearchResults
        {
            get
            {
                return this.Search(SearchTerm);
            }
        }

        public bool ShowHeadOnly
        {
            get
            {
                return showHeadOnly;
            }
            set
            {
                showHeadOnly = value;
                RaisePropertyChanged("ShowHeadOnly", "SearchResults");
            }
        }
        #endregion

        #region Methods
        public ActionItem[] GetContextItems(string context)
        {
            // TODO: return Search(context, null, null, null, null, null); // ?
            var result = from m in todo_items where m.Context.EndsWith(context.ToLower()) select m;
            if (showHeadOnly)
            {
                return (from i in result where i.RankParent == null select i).ToArray();
            }
            else
            {
                return result.ToArray();
            }
        }

        public void Save()
        {
            if (todo_changes)
            {
                todo_mapper.SaveAll((from i in todo_items orderby i.RankDepth select i).ToList());
                todo_changes = false;
            }
            if (someday_changes && someday_mapper != null)
            {
                someday_mapper.SaveAll((from i in someday_items orderby i.Title select i).ToList());
                someday_changes = false;
            }
            if (done_changes && done_mapper != null)
            {
                done_mapper.SaveAll((from i in done_items orderby i.DoneDate select i).ToList());
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
                if (done_mapper != null)
                {
                    doneitem.Context = string.Format("{0:yyyy-MM-dd}", doneitem.DoneDate);
                    done_items.Add(doneitem);
                    todo_items.Remove(doneitem);
                }
                else
                {
                    doneitem.Context = "done";
                }
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
            Queue<ActionItem> to_defer = new Queue<ActionItem>(selected);
            while (to_defer.Count > 0)
            {
                ActionItem i = to_defer.Dequeue();
                i.Context = "someday";
                if (someday_mapper != null)
                {
                    someday_items.Add(i);
                    todo_items.Remove(i);
                }
                someday_changes = true;
                todo_changes = true;

                //TODO: If this item was the project for another, defer that one too.
                foreach (ActionItem c in (from a in todo_items where a.Project == i select a))
                {
                    to_defer.Enqueue(c);
                }
            }
        }
        
        /// <summary>
        /// Moves an item from the Someday list to the main list.
        /// </summary>
        /// <param name="actionItem"></param>
        public void Undefer(string context, params ActionItem[] selection)
        {
            Queue<ActionItem> to_undefer = new Queue<ActionItem>(selection);
            while (to_undefer.Count > 0)
            {
                ActionItem i = to_undefer.Dequeue();
                i.Context = context;
                if (someday_mapper != null)
                {
                    todo_items.Add(i);
                    someday_items.Remove(i);
                }
                someday_changes = true;
                todo_changes = true;
                i.TickleDate = null;

                // If this item was the project for another, undefer that one, too.
                foreach (ActionItem c in (from a in SomedayItems where a.Project == i select a))
                {
                    to_undefer.Enqueue(c);
                }
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
        /// <param name="search_term">The string to look for.</param>
        /// <returns>A list of matching items.</returns>
        private ActionItem[] Search(string search_term)
        {
            // TODO: return Search(search_term, search_term, search_term, search_term, search_term, search_term);
            var result = (from i in todo_items
                    where i.Title.ToLower().Contains(search_term.ToLower())
                        || string.Join(Environment.NewLine, i.Notes).ToLower().Contains(search_term.ToLower())
                        || string.Join(Environment.NewLine, i.Tags.Keys).ToLower().Contains(search_term.ToLower())
                        || i.ID.ToString().StartsWith(search_term)
                        || string.Join(Environment.NewLine, (from k in i.Tags select k.Value)).ToLower().Contains(search_term.ToLower())
                    select i);
            if (showHeadOnly)
            {
                return (from i in result where i.RankParent == null select i).ToArray();
            }
            else
            {
                return result.ToArray();
            }
        }

        /// <summary>
        /// Advanced search_term. Any argument can be null to represent all values.
        /// </summary>
        /// <param name="context">The context to search_term for.</param>
        /// <param name="partial_title">Part of the title.</param>
        /// <param name="partial_note">Part of a note.</param>
        /// <param name="partial_id">The beginning of the ID.</param>
        /// <param name="has_tag">A tag name.</param>
        /// <param name="tag_value">A tag value (not necessarily that of the named tag).</param>
        /// <returns>An array of all matching items.</returns>
        /// <remarks>TODO: If both has_tag and tag_value are specified, require that item.Tags[has_tag] contains tag_value.</remarks>
        public ActionItem[] Search(string context, string partial_title, string partial_note, string partial_id, string has_tag, string tag_value, int mindepth, int maxdepth)
        {
            var result = (from i in todo_items
                          where ((partial_title == null || i.Title.ToLower().Contains(partial_title.ToLower()))
                          && (context == null || i.Context.ToLower() == context.ToLower())
                          && (partial_note == null || string.Join(Environment.NewLine, i.Notes).ToLower().Contains(partial_note.ToLower()))
                          && (has_tag == null || i.Tags.ContainsKey(has_tag))
                          && (partial_id == null || i.ID.ToString().ToLower().StartsWith(partial_id.ToLower()))
                          && (tag_value == null || string.Join(Environment.NewLine, from k in i.Tags select k.Value).ToLower().Contains(tag_value.ToLower())))
                          && (i.RankDepth >= mindepth && i.RankDepth <= maxdepth)
                          select i);
            return result.ToArray();
        }

        public List<ActionItem> GetProjectChildren(ActionItem actionItem)
        {
            var result = (from i in todo_items where i.Project == actionItem select i).ToList();
            if (showHeadOnly)
            {
                return (from i in result where i.RankParent == null select i).ToList();
            }
            else
            {
                return result.ToList();
            }
        }

        public List<ActionItem> GetTickleDueItems()
        {
            return (from i in SomedayItems where i.TickleDate <= DateTime.Now select i).ToList();
        }

        public IEnumerable<string> GetContextNames(params string[] exclude)
        {
            return (from i in todo_items select i.Context).Distinct().Except(exclude);
        }

        public void ResetPriorityParents()
        {
            foreach (var i in todo_items)
            {
                i.RankParent = null;
                todo_changes = true;
            }
        }

        public void ResetPriorityParents(ActionItem selected)
        {
            selected.RankParent = null;
            foreach (var i in todo_items)
            {
                if (i.RankParent == selected)
                {
                    i.RankParent = null;
                }
            }
            todo_changes = true;
        }

        public void SetTag(ActionItem selected, string tagname, string value)
        {
            selected.Tags[tagname] = value;
            todo_changes = true;
        }

        public void SetParent(ActionItem child, ActionItem parent)
        {
            child.RankParent = parent;
            todo_changes = true;
        }

        public void Defer(ActionItem deferitem, DateTime tickleDate)
        {
            deferitem.TickleDate = tickleDate;
            Defer(deferitem);
        }

        public void SetContext(ActionItem item, string newcontext)
        {
            item.Context = newcontext;
            todo_changes = true;
        }

        public void PruneBelowDepth(int depth)
        {
            var results = from i in todo_items where i.RankDepth >= depth select i;
            Defer(results.ToArray());
        }

        public void AddNote(ActionItem item, string note)
        {
            item.Notes.Add(note);
            todo_changes = true;
        }

        public void Rename(ActionItem item, string retitle)
        {
            item.Title = retitle;
            todo_changes = true;
        }

        public void RemoveTag(ActionItem item, string tagname)
        {
            item.Tags.Remove(tagname);
            todo_changes = true;
        }
        #endregion
    }
}
