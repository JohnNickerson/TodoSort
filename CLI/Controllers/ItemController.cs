using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class ItemController
{
    private readonly IItemDisplay _view;
    private readonly TodoService _svc;

    public ItemController(TodoService svc, IItemDisplay view)
    {
        _view = view;
        _svc = svc;
    }

    public void AddItem(AddSubOptions a)
    {
        _svc.ShowHeadOnly = false;
        var selected = new ActionItem { Context = a.Context, Title = a.ActionTitle };
        if (!string.IsNullOrWhiteSpace(a.Note))
        {
            selected.Notes.Add(a.Note);
        }
        _view.WriteLine("Adding action '{0}' to context @{1}", a.ActionTitle, a.Context);
        _svc.AddItem(selected);
        // Allow tagging right away.
        TagItem(selected);
        _view.PrintItem(selected, null, _svc, false, true);
        _svc.Save();
    }

    public void Delete(DeleteOptions delOpts)
    {
        // Find a matching item to delete.
        var searchResults = _svc.Search(delOpts.SearchSpecification, delOpts.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, _svc, !string.IsNullOrEmpty(delOpts.ItemId));
        if (selected != null)
        {
            _svc.Delete(selected);
            _view.PrintItem(selected, null, _svc, false, delOpts.Verbose);
            _svc.Save();
        }
    }

    public void Merge(MergeSubOptions mergeOptions)
    {
        _view.WriteLine("Confirm child item:");
        _svc.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
        var mergeVictim = _view.Disambiguate(_svc.SearchResults, _svc);
        if (mergeVictim != null)
        {
            _view.WriteLine("Confirm item to merge into:");
            _svc.SearchTerm = mergeOptions.TargetSearchTerm;
            var combined = _view.Disambiguate(_svc.SearchResults.Where(x => x.ID != mergeVictim.ID), _svc);
            if (mergeVictim != null && combined != null)
            {
                _svc.Merge(mergeVictim, combined);
                _view.PrintItem(combined, null, _svc, false, mergeOptions.Verbose);
                _svc.Save();
            }
        }
    }

    public void Move(MoveSubOptions moveOptions)
    {
        var searchResults = _svc.Search(moveOptions.SearchSpecification, moveOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, _svc, !string.IsNullOrEmpty(moveOptions.ItemId));
        if (selected != null)
        {
            selected.Context = moveOptions.NewContext;
            if (moveOptions.Unrank)
            {
                selected.ParentId = null;
                foreach (var child in _svc.GetChildren(selected))
                {
                    child.ParentId = null;
                    _svc.Update(child);
                }
            }
            _svc.Update(selected);
            _view.PrintItem(selected, null, _svc, false, moveOptions.Verbose);
            _svc.Save();
        }
    }

    public void MoveAll(MoveAllSubOptions moveAllOptions)
    {
        var items = _svc.Search(moveAllOptions.GetSearchSpecification(), moveAllOptions.ShowAllItems).ToList();
        var counter = 0;
        while (items.Count > 0)
        {
            items[0].Context = moveAllOptions.NewContext;
            _svc.Update(items[0]);
            items.RemoveAt(0);
            counter++;
        }
        _view.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));

        _svc.Save();
    }

    public void Note(NoteSubOptions noteOptions)
    {
        // Add a note to a task.
        var searchResults = _svc.Search(noteOptions.SearchSpecification, noteOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, _svc, !string.IsNullOrEmpty(noteOptions.ItemId));
        if (selected != null)
        {
            selected.Notes.Add(string.Format("{0} ({1:yyyy-MM-dd})", noteOptions.NewNote, DateTime.Now));
            _svc.Update(selected);
            _view.PrintItem(selected, null, _svc, false, noteOptions.Verbose);
            _svc.Save();
        }
    }

    public void Rename(RenameSubOptions renameOptions)
    {
        var searchResults = _svc.Search(renameOptions.SearchSpecification, renameOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, _svc, !string.IsNullOrEmpty(renameOptions.ItemId));
        if (selected != null)
        {
            selected.Title = renameOptions.NewTitle;
            _svc.Update(selected);
            _view.WriteLine("Item renamed.");
            if (renameOptions.ReTag)
            {
                TagItem(selected);
            }
            _view.PrintItem(selected, null, _svc, false, renameOptions.Verbose);
            _svc.Save();
        }
    }

    public void SetProject(SetProjectSubOptions commandOptions)
    {
        _view.WriteLine("Confirm child item:");
        var childSearchResults = _svc.Search(new FullTextSearchSpecification(commandOptions.ChildSearchTerm), commandOptions.ShowAllItems);
        var child = _view.Disambiguate(childSearchResults, _svc);

        _view.WriteLine("Confirm project:");
        var projectSearchResults = _svc.Search(new FullTextSearchSpecification(commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm), commandOptions.ShowAllItems);
        var project = _view.Disambiguate(projectSearchResults, _svc);
        if (child != null && project != null)
        {
            child.ProjectId = project.ID;
            _svc.Update(child);
        }

        _svc.Save();
    }

    public void Tag(SingleSearchSubOptions tagOptions)
    {
        // Search for a matching item.
        var searchResults = _svc.Search(tagOptions.SearchSpecification, tagOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, _svc, !string.IsNullOrEmpty(tagOptions.ItemId));
        if (selected != null)
        {
            TagItem(selected);
            _view.PrintItem(selected, null, _svc, false, tagOptions.Verbose);
            _svc.Save();
        }
    }

    public void TagAll(TagAllSubOptions tagAllOptions)
    {
        if (string.IsNullOrWhiteSpace(tagAllOptions.TagName))
        {
            _view.WriteLine("Tag name cannot be empty.");
            return;
        }

        var items = _svc.Search(tagAllOptions.SearchSpecification, tagAllOptions.ShowAllItems).ToArray();
        foreach (var i in items)
        {
            if (string.IsNullOrWhiteSpace(tagAllOptions.TagValue))
            {
                i.Tags.Remove(tagAllOptions.TagName);
                _svc.Update(i);
            }
            else
            {
                i.Tags[tagAllOptions.TagName] = tagAllOptions.TagValue;
                _svc.Update(i);
            }
        }

        _svc.Save();
    }

    private void TagItem(ActionItem selected)
    {
        string? tagName;
        do
        {
            _view.WriteLine("What should this new tag be called? (ENTER to quit)");
            tagName = Console.ReadLine()?.ToLower()?.Trim();
            if (!string.IsNullOrEmpty(tagName))
            {
                _view.WriteLine("What is the value of the tag?");
                var value = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    selected.Tags[tagName] = value;
                    _svc.Update(selected);
                }
                else if (selected.Tags.ContainsKey(tagName))
                {
                    selected.Tags.Remove(tagName);
                    _svc.Update(selected);
                }
                _view.WriteLine();
            }
        } while (!string.IsNullOrEmpty(tagName));
    }
}