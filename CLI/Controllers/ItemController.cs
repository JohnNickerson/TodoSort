using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class ItemController
{
    private readonly IItemDisplay _view;

    public ItemController(IItemDisplay view)
    {
        _view = view;
    }

    public void AddItem(AddSubOptions a, TodoService svc)
    {
        svc.ShowHeadOnly = false;
        var selected = new ActionItem { Context = a.Context, Title = a.ActionTitle };
        if (!string.IsNullOrWhiteSpace(a.Note))
        {
            selected.Notes.Add(a.Note);
        }
        _view.WriteLine("Adding action '{0}' to context @{1}", a.ActionTitle, a.Context);
        svc.AddItem(selected);
        // Allow tagging right away.
        TagItem(svc, selected);
        _view.PrintItem(selected, null, svc, false, true);
        svc.Save();
    }

    public void Delete(DeleteOptions delOpts, TodoService svc)
    {
        // Find a matching item to delete.
        var searchResults = svc.Search(delOpts.SearchSpecification, delOpts.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, svc, !string.IsNullOrEmpty(delOpts.ItemId));
        if (selected != null)
        {
            svc.Delete(selected);
            _view.PrintItem(selected, null, svc, false, delOpts.Verbose);
            svc.Save();
        }
    }

    public void Merge(MergeSubOptions mergeOptions, TodoService svc)
    {
        _view.WriteLine("Confirm child item:");
        svc.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
        var mergeVictim = _view.Disambiguate(svc.SearchResults, svc);
        if (mergeVictim != null)
        {
            _view.WriteLine("Confirm item to merge into:");
            svc.SearchTerm = mergeOptions.TargetSearchTerm;
            var combined = _view.Disambiguate(svc.SearchResults.Where(x => x.ID != mergeVictim.ID), svc);
            if (mergeVictim != null && combined != null)
            {
                svc.Merge(mergeVictim, combined);
                _view.PrintItem(combined, null, svc, false, mergeOptions.Verbose);
                svc.Save();
            }
        }
    }

    public void Move(MoveSubOptions moveOptions, TodoService svc)
    {
        var searchResults = svc.Search(moveOptions.SearchSpecification, moveOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, svc, !string.IsNullOrEmpty(moveOptions.ItemId));
        if (selected != null)
        {
            selected.Context = moveOptions.NewContext;
            if (moveOptions.Unrank)
            {
                selected.ParentId = null;
                foreach (var child in svc.GetChildren(selected))
                {
                    child.ParentId = null;
                    svc.Update(child);
                }
            }
            svc.Update(selected);
            _view.PrintItem(selected, null, svc, false, moveOptions.Verbose);
            svc.Save();
        }
    }

    public void MoveAll(MoveAllSubOptions moveAllOptions, TodoService svc)
    {
        var items = svc.Search(moveAllOptions.GetSearchSpecification(), moveAllOptions.ShowAllItems).ToList();
        var counter = 0;
        while (items.Count > 0)
        {
            items[0].Context = moveAllOptions.NewContext;
            svc.Update(items[0]);
            items.RemoveAt(0);
            counter++;
        }
        _view.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));

        svc.Save();
    }

    public void Note(NoteSubOptions noteOptions, TodoService svc)
    {
        // Add a note to a task.
        var searchResults = svc.Search(noteOptions.SearchSpecification, noteOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, svc, !string.IsNullOrEmpty(noteOptions.ItemId));
        if (selected != null)
        {
            selected.Notes.Add(string.Format("{0} ({1:yyyy-MM-dd})", noteOptions.NewNote, DateTime.Now));
            svc.Update(selected);
            _view.PrintItem(selected, null, svc, false, noteOptions.Verbose);
            svc.Save();
        }
    }

    public void Rename(RenameSubOptions renameOptions, TodoService svc)
    {
        var searchResults = svc.Search(renameOptions.SearchSpecification, renameOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, svc, !string.IsNullOrEmpty(renameOptions.ItemId));
        if (selected != null)
        {
            selected.Title = renameOptions.NewTitle;
            svc.Update(selected);
            _view.WriteLine("Item renamed.");
            if (renameOptions.ReTag)
            {
                TagItem(svc, selected);
            }
            _view.PrintItem(selected, null, svc, false, renameOptions.Verbose);
            svc.Save();
        }
    }

    public void SetProject(SetProjectSubOptions commandOptions, TodoService svc)
    {
        _view.WriteLine("Confirm child item:");
        var childSearchResults = svc.Search(new FullTextSearchSpecification(commandOptions.ChildSearchTerm), commandOptions.ShowAllItems);
        var child = _view.Disambiguate(childSearchResults, svc);

        _view.WriteLine("Confirm project:");
        var projectSearchResults = svc.Search(new FullTextSearchSpecification(commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm), commandOptions.ShowAllItems);
        var project = _view.Disambiguate(projectSearchResults, svc);
        if (child != null && project != null)
        {
            child.ProjectId = project.ID;
            svc.Update(child);
        }

        svc.Save();
    }

    public void Tag(SingleSearchSubOptions tagOptions, TodoService svc)
    {
        // Search for a matching item.
        var searchResults = svc.Search(tagOptions.SearchSpecification, tagOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, svc, !string.IsNullOrEmpty(tagOptions.ItemId));
        if (selected != null)
        {
            TagItem(svc, selected);
            _view.PrintItem(selected, null, svc, false, tagOptions.Verbose);
            svc.Save();
        }
    }

    public void TagAll(TagAllSubOptions tagAllOptions, TodoService svc)
    {
        if (string.IsNullOrWhiteSpace(tagAllOptions.TagName))
        {
            _view.WriteLine("Tag name cannot be empty.");
            return;
        }

        var items = svc.Search(tagAllOptions.SearchSpecification, tagAllOptions.ShowAllItems).ToArray();
        foreach (var i in items)
        {
            if (string.IsNullOrWhiteSpace(tagAllOptions.TagValue))
            {
                i.Tags.Remove(tagAllOptions.TagName);
                svc.Update(i);
            }
            else
            {
                i.Tags[tagAllOptions.TagName] = tagAllOptions.TagValue;
                svc.Update(i);
            }
        }

        svc.Save();
    }

    private void TagItem(TodoService svc, ActionItem selected)
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
                    svc.Update(selected);
                }
                else if (selected.Tags.ContainsKey(tagName))
                {
                    selected.Tags.Remove(tagName);
                    svc.Update(selected);
                }
                _view.WriteLine();
            }
        } while (!string.IsNullOrEmpty(tagName));
    }
}