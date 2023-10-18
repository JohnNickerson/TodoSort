using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace AssimilationSoftware.TodoSort.Core
{
    public class ViewModel : INotifyPropertyChanged
    {
        #region Fields

        readonly ITodoRepository _repository;

        private bool _showHeadOnly = true;
        string _statusMessage;
        private bool _unsavedChanges;

        private ISearchSpecification<ActionItem> _todoSearchSpec;
        private ISearchSpecification<ActionItem> _somedaySearchSpec;
        private ISearchSpecification<ActionItem> _doneSearchSpec;
        private int _progressPercent;

        #endregion

        public ViewModel(ITodoRepository repo)
        {
            _repository = repo;
            SearchSpecification = null;
            _unsavedChanges = false;
        }

        #region Events
        /// <summary>
        /// An event that indicates a property has changed value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the PropertyChanged event with given arguments.
        /// </summary>
        public void RaisePropertyChanged(params string[] propNames)
        {
            if (PropertyChanged != null)
            {
                foreach (string prop in propNames)
                {
                    var e = new PropertyChangedEventArgs(prop);
                    PropertyChanged(this, e);
                }
            }
        }
        #endregion

        #region Properties

        public List<ActionItem> SomedayItems => _repository.SomedayItems.ToList();

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
                    return _repository.Items.Where(i => SearchSpecification.And(new HeadOnlySearchSpecification()).IsSatisfiedBy(i));
                }
                else
                {
                    return _repository.Items.Where(i => SearchSpecification.IsSatisfiedBy(i));
                }
            }
        }

        public bool ShowHeadOnly
        {
            get => _showHeadOnly;
            set
            {
                _showHeadOnly = value;
                RaisePropertyChanged("ShowHeadOnly", "SearchResults");
            }
        }

        /// <summary>
        /// Gets or sets a general status message.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                RaisePropertyChanged(nameof(StatusMessage));
            }
        }

        public ISearchSpecification<ActionItem> SearchSpecification
        {
            get => _todoSearchSpec ?? new TrueSpecification<ActionItem>();
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
            get => _somedaySearchSpec ?? new TrueSpecification<ActionItem>();
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
            get => _doneSearchSpec ?? new TrueSpecification<ActionItem>();
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

        public List<ActionItem> DoneItems => _repository.DoneItems.ToList();

        public bool UnsavedChanges
        {
            get => _unsavedChanges;
            set
            {
                if (_unsavedChanges == value) return;
                _unsavedChanges = value;
                RaisePropertyChanged("UnsavedChanges");
            }
        }

        public int ProgressPercent
        {
            get => _progressPercent;
            set
            {
                if (_progressPercent == value) return;
                _progressPercent = value;
                RaisePropertyChanged(nameof(ProgressPercent));
            }
        }

        #endregion

        #region Methods
        public void Save(bool forceSave = false)
        {
            
            _repository.SaveChanges();
            UnsavedChanges = false;
        }

        public void AddItem(ActionItem next)
        {
            // Special case: if adding straight to the "done" list, and there is no date, mark as today.
            if (next.Context == "done" && !next.DoneDate.HasValue)
            {
                next.DoneDate = DateTime.Today;
            }

            if (next.ID == Guid.Empty)
            {
                next.ID = Guid.NewGuid();
            }
            _repository.Create(next);
            UnsavedChanges = true;
        }

        public void AddAllItems(string context, bool checkHashes, params ActionItem[] items)
        {
            int importedCount = 0;
            var existingHashes = new HashSet<string>();
            if (checkHashes)
            {
                existingHashes = new HashSet<string>(_repository.Items.Where(i => !string.IsNullOrEmpty(i.ImportHash)).Select(i => i.ImportHash));
            }
            foreach (var i in items)
            {
                if (checkHashes && existingHashes.Contains(i.ImportHash)) continue;
                if (!string.IsNullOrEmpty(context))
                {
                    i.Context = context;
                }
                else if (string.IsNullOrEmpty(i.Context))
                {
                    i.Context = "import";
                }

                AddItem(i);
                importedCount++;
            }
            StatusMessage = $"Imported {importedCount} items.";
            UnsavedChanges = true;
        }

        /// <summary>
        /// Mark an item as done.
        /// </summary>
        public void MarkDone(DateTime? doneDate, params ActionItem[] doneItems)
        {
            foreach (ActionItem doneItem in doneItems)
            {
                doneItem.DoneDate = doneDate ?? DateTime.Now;
                doneItem.Context = "done";
                ResetPriorityParents(doneItem); // Or else it will continue to hide its children by default.
                _repository.Update(doneItem);
                // Implicit Chains: If this item has an "order" tag, and is attached to a project or has a "series" tag, look for the next item.
                if (doneItem.Tags.ContainsKey("order"))
                {
                    var restoreHead = _showHeadOnly;
                    _showHeadOnly = false;
                    var inChain = false;
                    if (doneItem.Tags.ContainsKey("series"))
                    {
                        SearchSpecification = new AndSpecification<ActionItem>(
                            new TagValueSpecification("series", doneItem.Tags["series"]),
                            new TagValueSpecification("order", (doneItem.GetIntTag("order", 0) + 1).ToString()));
                        inChain = true;
                    }
                    else if (doneItem.ProjectId != null)
                    {
                        SearchSpecification = new AndSpecification<ActionItem>(
                            new ProjectChildrenSearchSpecification(doneItem.ProjectId.ToString()),
                            new TagValueSpecification("order", (doneItem.GetIntTag("order", 0) + 1).ToString()));
                        inChain = true;
                    }
                    if (inChain)
                    {
                        if (SearchResults.Any())
                        {
                            foreach (var next in SearchResults.ToList())
                            {
                                SetParent(next, null);
                            }
                        }
                        else
                        {
                            // No next item found. Issue a warning.
                            StatusMessage = "No next item found in chain.";
                        }
                    }
                    else
                    {
                        StatusMessage = string.Empty;
                    }

                    _showHeadOnly = restoreHead;
                }
            }
            RaisePropertyChanged("SearchResults", "DoneSearchResults");
            UnsavedChanges = true;
        }

        /// <summary>
        /// Moves a list of items to the Someday list.
        /// </summary>
        /// <param name="selected"></param>
        public void Defer(params ActionItem[] selected)
        {
            Queue<ActionItem> toDefer = new Queue<ActionItem>(selected);
            while (toDefer.Count > 0)
            {
                ActionItem i = toDefer.Dequeue();
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
                    if (c.Context != "done")
                    {
                        toDefer.Enqueue(c);
                    }
                }
            }
            RaisePropertyChanged("SearchResults", "SomedaySearchResults");
            UnsavedChanges = true;
        }

        /// <summary>
        /// Moves an item from the Someday list to the main list.
        /// </summary>
        public void Undefer(string context, params ActionItem[] selection)
        {
            Queue<ActionItem> toUndefer = new Queue<ActionItem>(selection);
            while (toUndefer.Count > 0)
            {
                ActionItem i = toUndefer.Dequeue();
                if (context == "inbox" && i.Tags.ContainsKey("previous-context"))
                {
                    i.Context = i.Tags["previous-context"] != i.Context ? i.Tags["previous-context"] : context;
                    i.Tags.Remove("previous-context");
                }
                else
                {
                    i.Context = context;
                }
                i.TickleDate = null;
                _repository.Update(i);
                UnsavedChanges = true;

                // If this item was the project for another, undefer that one, too.
                foreach (var c in SomedayItems.Where(a => a.ProjectId != null && a.ProjectId == i.ID))
                {
                    toUndefer.Enqueue(c);
                }
            }
        }

        /// <summary>
        /// Moves an item from the Done list to the main list.
        /// </summary>
        public void Undo(string context, params ActionItem[] selection)
        {
            Queue<ActionItem> toUndo = new Queue<ActionItem>(selection);
            while (toUndo.Count > 0)
            {
                ActionItem i = toUndo.Dequeue();
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
                UnsavedChanges = true;
            }
        }

        public void Delete(params ActionItem[] selection)
        {
            foreach (ActionItem i in selection)
            {
                _repository.Delete(i);
            }
            RaisePropertyChanged("SearchResults");
            UnsavedChanges = true;
        }

        public IEnumerable<string> GetContextNames(params string[] exclude)
        {
            return _repository.GetContexts(exclude);
        }

        public void ResetPriorityParents(params ActionItem[] items)
        {
            foreach (var selected in items)
            {
                if (selected.ParentId != null)
                {
                    selected.ParentId = null;
                    _repository.Update(selected);
                }
                foreach (var i in _repository.GetChildren(selected).ToArray())
                {
                    i.ParentId = null;
                    _repository.Update(i);
                    UnsavedChanges = true;
                }
            }
        }

        public void SetTag(ActionItem selected, string tagName, string value)
        {
            if (selected != null)
            {
                selected.Tags[tagName] = value;
                _repository.Update(selected);
                UnsavedChanges = true;
            }
        }

        public void SetParent(ActionItem child, ActionItem parent)
        {
            child.ParentId = parent?.ID;
            // Increment the "upvotes" counter.
            if (parent != null)
            {
                parent.Upvotes++;
                _repository.Update(parent);
            }
            _repository.Update(child);
            UnsavedChanges = true;
        }

        public void Balance(ActionItem[] items, int branchFactor, bool setNullParents = true)
        {
            // Assume that the items have already been sorted.
            // In order, set parents.
            for (int i = 0; i < items.Length; i++)
            {
                var newIndex = (int)Math.Floor((double)i / branchFactor) - 1;
                if (newIndex == -1)
                {
                    if (setNullParents || items.Intersect(GetAncestors(items[i])).Any())
                    {
                        // Only reset the parent if requested or if not doing so would cause a loop.
                        items[i].ParentId = null;
                        _repository.Update(items[i]);
                    }
                }
                else
                {
                    items[i].ParentId = items[newIndex].ID;
                    _repository.Update(items[i]);
                }

                // Report progress every 5%.
                if (items.Length > 20 && (i == 0 || i % (items.Length / 20) == 0))
                {
                    ProgressPercent = i * 100 / items.Length;
                }
            }
            UnsavedChanges = true;
        }

        private IEnumerable<ActionItem> GetAncestors(ActionItem actionItem)
        {
            var a = new List<ActionItem>
            {
                actionItem
            };
            if (actionItem.ParentId == null) return a;
            for (var b = actionItem.GetParent(_repository);
                b != null && !a.Contains(b);
                b = b.GetParent(_repository))
            {
                a.Add(b);
            }

            return a;
        }

        public void SetProject(ActionItem child, ActionItem project)
        {
            child.ProjectId = project.ID;
            _repository.Update(child);
            UnsavedChanges = true;
        }

        public void Defer(ActionItem deferItem, DateTime tickleDate)
        {
            deferItem.TickleDate = tickleDate;
            Defer(deferItem);
            UnsavedChanges = true;
        }

        public void SetContext(ActionItem item, string newContext)
        {
            item.Context = newContext;
            _repository.Update(item);
            UnsavedChanges = true;
        }

        public void AddNote(ActionItem item, string note)
        {
            item.Notes.Add(string.Format("{0} ({1:yyyy-MM-dd})", note, DateTime.Now));
            _repository.Update(item);
            UnsavedChanges = true;
        }

        public void Rename(ActionItem item, string newTitle)
        {
            item.Title = newTitle;
            _repository.Update(item);
            UnsavedChanges = true;
        }

        public void RemoveTag(ActionItem item, string tagName)
        {
            item.Tags.Remove(tagName);
            _repository.Update(item);
            UnsavedChanges = true;
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
            foreach (var c in _repository.GetChildren(child).ToArray())
            {
                c.ParentId = target.ID;
            }
            if (child.ProjectId != null && target.ProjectId == null)
            {
                target.ProjectId = child.ProjectId;
            }
            if (child.TickleDate != null && target.TickleDate == null)
            {
                target.TickleDate = child.TickleDate;
            }
            target.Notes.Add(string.Format("Merged with '{0}' on {1:yyyy-MM-dd}", child.Title, DateTime.Now));
            _repository.Update(target);
            Delete(child);
            UnsavedChanges = true;
        }

        public List<ActionItem> GetProjects()
        {
            return _repository.Items.Where(p => p.Context == "projects").ToList();
        }

        public void Update(ActionItem item)
        {
            _repository.Update(item);
            UnsavedChanges = true;
        }

        public Dictionary<Guid, int> GetDepthsView()
        {
            // 1. Get a list of parent/child ID relationships.
            var parents = SearchResults.ToDictionary(ki => ki.ID, v => v.ParentId);
            // 2. Process parents dictionary for depths.
            var depths = new Dictionary<Guid, int>();
            var proq = new Queue<Guid>(parents.Keys);
            while (proq.Count > 0)
            {
                var child = proq.Dequeue();
                if (depths.ContainsKey(child))
                {
                    // skip it.
                }
                else if (!parents.ContainsKey(child) || parents[child] == null)
                {
                    // No parent or parent unknown.
                    depths[child] = 0;
                }
                else if (depths.ContainsKey(parents[child].Value))
                {
                    depths[child] = depths[parents[child].Value] + 1;
                }
                else
                {
                    // We need to check for out-of-bounds references and circular references.
                    var ancestors = new Stack<Guid>();
                    Guid? current = child;
                    while (current != null && !ancestors.Contains(current.Value))
                    {
                        ancestors.Push(current.Value);
                        current = parents.ContainsKey(current.Value) ? parents[current.Value] : null;
                    }
                    var root = ancestors.Pop();
                    if (!parents.ContainsKey(root) || parents[root] == null || !parents.ContainsKey(parents[root].Value))
                    {
                        depths[root] = 0;
                        while (ancestors.Count > 0)
                        {
                            var head = ancestors.Pop();
                            if (parents.ContainsKey(head) && parents[head].HasValue && depths.ContainsKey(parents[head].Value))
                            {
                                depths[head] = depths[parents[head].Value] + 1;
                            }
                        }
                    }
                    else
                    {
                        depths[root] = 0;
                    }
                }
            }

            return depths;
        }

        #endregion
    }
}
