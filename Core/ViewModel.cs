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

        // Track whether changes have been made to each file, to avoid rewriting them if possible.
        bool todo_changes;
        bool someday_changes;
        bool done_changes;

        private bool showHeadOnly = true;
        string _statusMessage;

        private ISearchSpecification<ActionItem> _todoSearchSpec;
        private ISearchSpecification<ActionItem> _somedaySearchSpec;
        private ISearchSpecification<ActionItem> _doneSearchSpec;
        #endregion

        public ViewModel(IPimDataMapper<ActionItem> todo)
        {
            todo_mapper = todo;

            todo_items = todo.LoadAll();

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
                return todo_items.Where(i => i.Context == "someday").ToList();
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
                // Always exclude "done" and "someday" contexts, now that we're working with just one list.
                _todoSearchSpec = value.And(new NotSpecification<ActionItem>(new ContextSearchSpecification("done"), new ContextSearchSpecification("someday")));
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

        /// <summary>
        /// Searches for items with identical titles and report those titles.
        /// </summary>
        /// <returns>A list of titles that are duplicated in the collection.</returns>
        public IEnumerable<string> GetDuplicateTitles()
        {
            return SearchResults.GroupBy(i => i.Title).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        }

        /// <summary>
        /// Searches for tag values that are duplicated among multiple items and reports those tag values.
        /// </summary>
        /// <param name="tag">The name of the tag to retrieve.</param>
        /// <returns>A list of tag values.</returns>
        public IEnumerable<string> GetDuplicateTags(string tag)
        {
            return SearchResults.GroupBy(i => i.Tags.ContainsKey(tag) ? i.Tags[tag] : "").Where(g => g.Count() > 1).Select(g => g.Key).ToList();
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
                return todo_items.Where(i => i.Context == "done").ToList();
            }
        }
        #endregion

        #region Methods
        public void Save(bool force_save = false)
        {
            if (todo_changes || force_save)
            {
                todo_mapper.SaveAll((from i in todo_items orderby i.RankDepth select i).ToList());
                todo_changes = false;
            }
        }

        public void AddItem(ActionItem next)
        {
            todo_items.Add(next);
            // Special case: if adding straight to the "done" list, and there is no date, mark as today.
            if (next.Context == "done" && !next.DoneDate.HasValue)
            {
                next.DoneDate = DateTime.Today;
            }
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
                doneitem.Context = "done";
                ResetPriorityParents(doneitem); // Or else it will continue to hide its children by default.
                done_changes = true;
                todo_changes = true;
            }
            RaisePropertyChanged("SearchResults", "DoneSearchResults");
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
                ResetPriorityParents(i); // To avoid hiding children while deferred.
                someday_changes = true;
                todo_changes = true;

                //TODO: If this item was the project for another, defer that one too.
                foreach (ActionItem c in (from a in todo_items where a.Project == i select a))
                {
                    to_defer.Enqueue(c);
                }
            }
            RaisePropertyChanged("SearchResults", "SomedaySearchResults");
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

        public void Balance(ActionItem[] items, int branchfactor, bool setNullParents = true)
        {
            // Assume that the items have already been sorted.
            // In order, set parents.
            for (int i = 0; i < items.Count(); i++)
            {
                var newdex = (int)Math.Floor((double)i / branchfactor) - 1;
                if (newdex == -1)
                {
                    if (setNullParents || items.Intersect(Ancestors(items[i])).Count() > 0)
                    {
                        // Only reset the parent if requested or if not doing so would cause a loop.
                        items[i].RankParent = null;
                    }
                }
                else
                {
                    items[i].RankParent = items[newdex];
                }
            }
            todo_changes = true;
        }

        private IEnumerable<ActionItem> Ancestors(ActionItem actionItem)
        {
            var a = new List<ActionItem>
            {
                actionItem
            };
            for (var b = actionItem.RankParent; b != null && !a.Contains(b); b = b.RankParent)
            {
                a.Add(b);
            }
            return a;
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
            // Update the vote count.
            target.Tags["upvotes"] = (child.Upvotes() + target.Upvotes()).ToString();
            foreach (var tag in child.Tags.Where(t => t.Key != "upvotes"))
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
