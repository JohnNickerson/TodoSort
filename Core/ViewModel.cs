using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core.Search;
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

        private bool showHeadOnly = true;
        string _statusMessage;

        private ISearchSpecification<ActionItem> _todoSearchSpec;
        private ISearchSpecification<ActionItem> _somedaySearchSpec;
        private ISearchSpecification<ActionItem> _doneSearchSpec;
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

            SearchSpecification = null;
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
                    return todo_items.Where(i => i.Context == "someday").ToList();
                }
            }
        }

        public string SearchTerm
        {
            set
            {
                SearchSpecification = new FullTextSearchSpecification(value);
                RaisePropertyChanged("SearchTerm", "SearchResults");
            }
        }

        public IEnumerable<ActionItem> SearchResults
        {
            get
            {
                if (ShowHeadOnly)
                {
                    return this.todo_items.Where(i => SearchSpecification.And(new DepthRangeSearchSpecification(0, 0)).IsSatisfiedBy(i));
                }
                else
                {
                    return this.todo_items.Where(i => SearchSpecification.IsSatisfiedBy(i));
                }
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

        /// <summary>
        /// Gets or sets a general status message.
        /// </summary>
        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
                RaisePropertyChanged("StatusMessage");
            }
        }

        public ISearchSpecification<ActionItem> SearchSpecification
        {
            get
            {
                return _todoSearchSpec ?? new TrueSpecification<ActionItem>();
            }
            set
            {
                _todoSearchSpec = value;
                RaisePropertyChanged("SearchSpecification", "SearchResults");
            }
        }

        /// <summary>
        /// A search specification for someday items.
        /// </summary>
        public ISearchSpecification<ActionItem> SomedaySearchSpecification
        {
            get
            {
                return _somedaySearchSpec ?? new TrueSpecification<ActionItem>();
            }
            set
            {
                _somedaySearchSpec = value;
                RaisePropertyChanged("SomedaySearchSpecification", "SomedaySearchResults");
            }
        }

        /// <summary>
        /// Results of searching the Someday collection.
        /// </summary>
        public IEnumerable<ActionItem> SomedaySearchResults
        {
            get
            {
                return SomedayItems.Where(s => SomedaySearchSpecification.IsSatisfiedBy(s));
            }
        }

        /// <summary>
        /// A search specification for looking through the Done items.
        /// </summary>
        public ISearchSpecification<ActionItem> DoneSearchSpecification
        {
            get
            {
                return _doneSearchSpec ?? new TrueSpecification<ActionItem>();
            }
            set
            {
                _doneSearchSpec = value;
                RaisePropertyChanged("DoneSearchSpecification", "DoneSearchResults");
            }
        }

        /// <summary>
        /// Search results from the Done collection.
        /// </summary>
        public IEnumerable<ActionItem> DoneSearchResults
        {
            get
            {
                return DoneItems.Where(s => DoneSearchSpecification.IsSatisfiedBy(s));
            }
        }

        public List<ActionItem> Items
        {
            get
            {
                return todo_items;
            }
        }

        public List<ActionItem> DoneItems
        {
            get
            {
                if (done_mapper == null)
                {
                    return todo_items.Where(i => i.Context == "done").ToList();
                }
                else
                {
                    return done_items;
                }
            }
        }
        #endregion

        #region Methods
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

        public void AddAllItems(string context, params ActionItem[] items)
        {
            int importedCount = 0;
            foreach (ActionItem i in items)
            {
                if (context != null)
                {
                    i.Context = context;
                }
                AddItem(i);
                importedCount++;
            }
            StatusMessage = string.Format("Imported {0} items.", importedCount);
        }

        /// <summary>
        /// Mark an item as done.
        /// </summary>
        /// <param name="doneitem">The item to mark as done.</param>
        public void MarkDone(DateTime? donedate, params ActionItem[] doneitems)
        {
            foreach (ActionItem doneitem in doneitems)
            {
                doneitem.DoneDate = donedate.HasValue ? donedate.Value : DateTime.Now;
                if (done_mapper != null)
                {
                    doneitem.Context = string.Format("{0:yyyy-MM-dd}", doneitem.DoneDate);
                    done_items.Add(doneitem);
                    todo_items.Remove(doneitem);
                }
                else
                {
                    doneitem.Context = "done";
                    ResetPriorityParents(doneitem); // Or else it will continue to hide its children by default.
                }
                done_changes = true;
                todo_changes = true;
            }
            RaisePropertyChanged("SearchResults");
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
                if (i.Context != "someday")
                {
                    i.Tags["previous-context"] = i.Context;
                    i.Context = "someday";
                }
                if (someday_mapper != null)
                {
                    someday_items.Add(i);
                    todo_items.Remove(i);
                }
                else
                {
                    ResetPriorityParents(i); // To avoid hiding children while deferred.
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
                if (context == "inbox" && i.Tags.ContainsKey("previous-context"))
                {
                    if (i.Tags["previous-context"] != i.Context)
                    {
                        i.Context = i.Tags["previous-context"];
                    }
                    else
                    {
                        i.Context = context;
                    }
                    i.Tags.Remove("previous-context");
                }
                else
                {
                    i.Context = context;
                }
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

        /// <summary>
        /// Moves an item from the Done list to the main list.
        /// </summary>
        /// <param name="actionItem"></param>
        public void Undo(string context, params ActionItem[] selection)
        {
            Queue<ActionItem> to_undo = new Queue<ActionItem>(selection);
            while (to_undo.Count > 0)
            {
                ActionItem i = to_undo.Dequeue();
                if (context == "inbox" && i.Tags.ContainsKey("previous-context") && i.Tags["previous-context"] != "done")
                {
                    i.Context = i.Tags["previous-context"];
                    i.Tags.Remove("previous-context");
                }
                else
                {
                    i.Context = context;
                }
                if (done_mapper != null)
                {
                    todo_items.Add(i);
                    done_items.Remove(i);
                }
                done_changes = true;
                todo_changes = true;
                i.DoneDate = null;
            }
        }

        public void Delete(params ActionItem[] selection)
        {
            foreach (ActionItem i in selection)
            {
                todo_items.Remove(i);
                todo_changes = true;
            }
            RaisePropertyChanged("SearchResults");
        }

        public IEnumerable<string> GetContextNames(params string[] exclude)
        {
            return (from i in todo_items select i.Context).Distinct().Except(exclude);
        }

        public void ResetPriorityParents(params ActionItem[] items)
        {
            foreach (var selected in items)
            {
                selected.RankParent = null;
                foreach (var i in todo_items.Where(t => t.RankParent == selected))
                {
                    i.RankParent = null;
                }
            }
            todo_changes = true;
        }

        public void SetTag(ActionItem selected, string tagname, string value)
        {
            if (selected != null)
            {
                selected.Tags[tagname] = value;
                todo_changes = true;
            }
        }

        public void SetParent(ActionItem child, ActionItem parent)
        {
            child.RankParent = parent;
            // Increment the "upvotes" counter.
            SetTag(parent, "upvotes", (parent.GetIntTag("upvotes", 0) + 1).ToString());
            todo_changes = true;
        }

        public string GetStringTag(ActionItem item, string tagname, string fallback = "")
        {
            if (!item.Tags.ContainsKey(tagname))
            {
                return fallback;
            }
            else
            {
                return item.Tags[tagname];
            }
        }

        public void Balance(ActionItem[] items, int branchfactor)
        {
            // Line up the items from the given context by depth and upvotes.
            var vine = items.OrderBy(a => a.Depth()).ThenByDescending(a => a.GetIntTag("upvotes", 0)).ToList();
            // In order, set parents.
            for (int i = 0; i < vine.Count; i++)
            {
                var newdex = (int)Math.Floor((double)i / branchfactor) - 1;
                if (newdex == -1)
                {
                    vine[i].RankParent = null;
                }
                else
                {
                    vine[i].RankParent = vine[newdex];
                }
            }
            todo_changes = true;
        }

        public void SetProject(ActionItem child, ActionItem project)
        {
            child.Project = project;
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

        public void AddNote(ActionItem item, string note)
        {
            item.Notes.Add(string.Format("{0} ({1:yyyy-MM-dd})", note, DateTime.Now));
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

        public void Merge(ActionItem child, ActionItem target)
        {
            // Combine the two items.
            // Add notes and tags from second to first.
            target.Notes.AddRange(child.Notes);
            foreach (var tag in child.Tags)
            {
                if (!target.Tags.ContainsKey(tag.Key))
                {
                    target.Tags[tag.Key] = tag.Value;
                }
                else if (target.Tags[tag.Key] == tag.Value)
                {
                    // Both items have the same tag value. Just ignore it.
                }
                else
                {
                    // Keep the tag as a note.
                    target.Notes.Add(string.Format("Merged key conflict: {0}:{1}", tag.Key, tag.Value));
                }
            }
            // Set any child objects from second to first.
            var children = from i in todo_items where i.RankParent == child select i;
            foreach (var c in children)
            {
                c.RankParent = target;
            }
            if (child.Project != null && target.Project == null)
            {
                target.Project = child.Project;
            }
            if (child.TickleDate != null && target.TickleDate == null)
            {
                target.TickleDate = child.TickleDate;
            }
            target.Notes.Add(string.Format("Merged with '{0}' on {1:yyyy-MM-dd}", child.Title, DateTime.Now));
            Delete(child);
            todo_changes = true;
        }
        #endregion
    }
}
