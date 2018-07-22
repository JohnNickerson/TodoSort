using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AssimilationSoftware.TodoSort.Core
{
    public class ViewModel : INotifyPropertyChanged
    {
        #region Fields
        ITodoRepository _repository;

        private bool showHeadOnly = true;
        string _statusMessage;

        private ISearchSpecification<ActionItem> _todoSearchSpec;
        private ISearchSpecification<ActionItem> _somedaySearchSpec;
        private ISearchSpecification<ActionItem> _doneSearchSpec;
        #endregion

        public ViewModel(IPimDataMapper<ActionItem> todo)
        {
            _repository = new TodoRepository(todo);
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
                return _repository.SomedayItems.ToList();
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
                    return _repository.FindAll().Where(i => SearchSpecification.And(new DepthRangeSearchSpecification(0, 0)).IsSatisfiedBy(i));
                }
                else
                {
                    return _repository.FindAll().Where(i => SearchSpecification.IsSatisfiedBy(i));
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
            return SearchResults.Where(a => a.Tags.ContainsKey(tag)).GroupBy(i => i.Tags[tag]).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        }

        public List<ActionItem> DoneItems
        {
            get
            {
                return _repository.DoneItems.ToList();
            }
        }
        #endregion

        #region Methods
        public void Save()
        {
            _repository.SaveChanges();
            //todo_mapper.SaveAll((from i in Items orderby i.RankDepth select i).ToList());
        }

        public void AddItem(ActionItem next)
        {
            // Special case: if adding straight to the "done" list, and there is no date, mark as today.
            if (next.Context == "done" && !next.DoneDate.HasValue)
            {
                next.DoneDate = DateTime.Today;
            }
            _repository.Create(next);
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
                _repository.Update(doneitem);
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
                _repository.Update(i);

                // If this item was the project for another, defer that one too.
                foreach (ActionItem c in _repository.GetProjectItems(i))
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
                i.TickleDate = null;
                _repository.Update(i);

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
                i.DoneDate = null;
                _repository.Update(i);
            }
        }

        public void Delete(params ActionItem[] selection)
        {
            foreach (ActionItem i in selection)
            {
                _repository.Delete(i);
            }
            RaisePropertyChanged("SearchResults");
        }

        public IEnumerable<string> GetContextNames(params string[] exclude)
        {
            return _repository.GetContexts(exclude); 
        }

        public void ResetPriorityParents(params ActionItem[] items)
        {
            foreach (var selected in items)
            {
                selected.RankParent = null;
                _repository.Update(selected);
                foreach (var i in _repository.GetChildren(selected))
                {
                    i.RankParent = null;
                    _repository.Update(i);
                }
            }
        }

        public void SetTag(ActionItem selected, string tagname, string value)
        {
            if (selected != null)
            {
                selected.Tags[tagname] = value;
                _repository.Update(selected);
            }
        }

        public void SetParent(ActionItem child, ActionItem parent)
        {
            child.RankParent = parent;
            // Increment the "upvotes" counter.
            parent.Upvotes++;
            _repository.Update(child);
            _repository.Update(parent);
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
                    if (setNullParents || items.Intersect(GetAncestors(items[i])).Count() > 0)
                    {
                        // Only reset the parent if requested or if not doing so would cause a loop.
                        items[i].RankParent = null;
                        _repository.Update(items[i]);
                    }
                }
                else
                {
                    items[i].RankParent = items[newdex];
                    _repository.Update(items[i]);
                }
            }
        }

        private IEnumerable<ActionItem> GetAncestors(ActionItem actionItem)
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
            _repository.Update(child);
        }

        public void Defer(ActionItem deferitem, DateTime tickleDate)
        {
            deferitem.TickleDate = tickleDate;
            Defer(deferitem);
        }

        public void SetContext(ActionItem item, string newcontext)
        {
            item.Context = newcontext;
            _repository.Update(item);
        }

        public void AddNote(ActionItem item, string note)
        {
            item.Notes.Add(string.Format("{0} ({1:yyyy-MM-dd})", note, DateTime.Now));
            _repository.Update(item);
        }

        public void Rename(ActionItem item, string retitle)
        {
            item.Title = retitle;
            _repository.Update(item);
        }

        public void RemoveTag(ActionItem item, string tagname)
        {
            item.Tags.Remove(tagname);
            _repository.Update(item);
        }

        public void Merge(ActionItem child, ActionItem target)
        {
            // Combine the two items.
            // Add notes and tags from second to first.
            target.Notes.AddRange(child.Notes);
            // Update the vote count.
            target.Upvotes = child.Upvotes + target.Upvotes;
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
            foreach (var c in _repository.GetChildren(child))
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
            _repository.Update(target);
            Delete(child);
        }
        #endregion
    }
}
