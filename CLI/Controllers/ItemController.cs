using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class ItemController
{
    private readonly IItemDisplay _view;

    public ItemController(IItemDisplay view)
    {
        _view = view;
    }

    public void AddItem(AddSubOptions a, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = false;
        var selected = new ActionItem { Context = a.Context, Title = a.ActionTitle };
        if (!string.IsNullOrWhiteSpace(a.Note))
        {
            selected.Notes.Add(a.Note);
        }
        _view.WriteLine("Adding action '{0}' to context @{1}", a.ActionTitle, a.Context);
        vm.AddItem(selected);
        // Allow tagging right away.
        TagItem(repo, selected);
        _view.PrintItem(selected, null, repo, false, true);
        repo.SaveChanges();
    }

    public void Delete(DeleteOptions delOpts, TodoRepository repo)
    {
        // Find a matching item to delete.
        var searchResults = repo.Search(delOpts.SearchSpecification, delOpts.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, repo, !string.IsNullOrEmpty(delOpts.ItemId));
        if (selected != null)
        {
            repo.Delete(selected);
            _view.PrintItem(selected, null, repo, false, delOpts.Verbose);
            repo.SaveChanges();
        }
    }

    public void Merge(MergeSubOptions mergeOptions, ViewModel vm, TodoRepository repo)
        {
            _view.WriteLine("Confirm child item:");
            vm.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
            var mergeVictim = _view.Disambiguate(vm.SearchResults, repo);
            if (mergeVictim != null)
            {
                _view.WriteLine("Confirm item to merge into:");
                vm.SearchTerm = mergeOptions.TargetSearchTerm;
                var combined = _view.Disambiguate(vm.SearchResults.Where(x => x.ID != mergeVictim.ID), repo);
                if (mergeVictim != null && combined != null)
                {
                    vm.Merge(mergeVictim, combined);
                    _view.PrintItem(combined, null, repo, false, mergeOptions.Verbose);
                    repo.SaveChanges();
                }
            }
        }

    public void Move(MoveSubOptions moveOptions, TodoRepository repo)
    {
        var searchResults = repo.Search(moveOptions.SearchSpecification, moveOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, repo, !string.IsNullOrEmpty(moveOptions.ItemId));
        if (selected != null)
        {
            selected.Context = moveOptions.NewContext;
            if (moveOptions.Unrank)
            {
                selected.ParentId = null;
                foreach (var child in repo.GetChildren(selected))
                {
                    child.ParentId = null;
                    repo.Update(child);
                }
            }
            repo.Update(selected);
            _view.PrintItem(selected, null, repo, false, moveOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void MoveAll(MoveAllSubOptions moveAllOptions, TodoRepository repo)
    {
        var items = repo.Search(moveAllOptions.GetSearchSpecification(), moveAllOptions.ShowAllItems).ToList();
        var counter = 0;
        while (items.Count > 0)
        {
            items[0].Context = moveAllOptions.NewContext;
            repo.Update(items[0]);
            items.RemoveAt(0);
            counter++;
        }
        _view.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));

        repo.SaveChanges();
    }

    public void Note(NoteSubOptions noteOptions, TodoRepository repo)
    {
        // Add a note to a task.
        var searchResults = repo.Search(noteOptions.SearchSpecification, noteOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, repo, !string.IsNullOrEmpty(noteOptions.ItemId));
        if (selected != null)
        {
            selected.Notes.Add(string.Format("{0} ({1:yyyy-MM-dd})", noteOptions.NewNote, DateTime.Now));
            repo.Update(selected);
            _view.PrintItem(selected, null, repo, false, noteOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void Rename(RenameSubOptions renameOptions, TodoRepository repo)
    {
        var searchResults = repo.Search(renameOptions.SearchSpecification, renameOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, repo, !string.IsNullOrEmpty(renameOptions.ItemId));
        if (selected != null)
        {
            selected.Title = renameOptions.NewTitle;
            repo.Update(selected);
            _view.WriteLine("Item renamed.");
            if (renameOptions.ReTag)
            {
                TagItem(repo, selected);
            }
            _view.PrintItem(selected, null, repo, false, renameOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void SetProject(SetProjectSubOptions commandOptions, TodoRepository repo)
    {
        _view.WriteLine("Confirm child item:");
        var childSearchResults = repo.Search(new FullTextSearchSpecification(commandOptions.ChildSearchTerm), commandOptions.ShowAllItems);
        var child = _view.Disambiguate(childSearchResults, repo);

        _view.WriteLine("Confirm project:");
        var projectSearchResults = repo.Search(new FullTextSearchSpecification(commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm), commandOptions.ShowAllItems);
        var project = _view.Disambiguate(projectSearchResults, repo);
        if (child != null && project != null)
        {
            child.ProjectId = project.ID;
            repo.Update(child);
        }

        repo.SaveChanges();
    }

    public void Tag(SingleSearchSubOptions tagOptions, TodoRepository repo)
    {
        // Search for a matching item.
        var searchResults = repo.Search(tagOptions.SearchSpecification, tagOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, repo, !string.IsNullOrEmpty(tagOptions.ItemId));
        if (selected != null)
        {
            TagItem(repo, selected);
            _view.PrintItem(selected, null, repo, false, tagOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void TagAll(TagAllSubOptions tagAllOptions, TodoRepository repo)
    {
        if (string.IsNullOrWhiteSpace(tagAllOptions.TagName))
        {
            _view.WriteLine("Tag name cannot be empty.");
            return;
        }

        var items = repo.Search(tagAllOptions.SearchSpecification, tagAllOptions.ShowAllItems).ToArray();
        foreach (var i in items)
        {
            if (string.IsNullOrWhiteSpace(tagAllOptions.TagValue))
            {
                i.Tags.Remove(tagAllOptions.TagName);
                repo.Update(i);
            }
            else
            {
                i.Tags[tagAllOptions.TagName] = tagAllOptions.TagValue;
                repo.Update(i);
            }
        }

        repo.SaveChanges();
    }
        
    private void TagItem(TodoRepository repo, ActionItem selected)
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
                    repo.Update(selected);
                }
                else if (selected.Tags.ContainsKey(tagName))
                {
                    selected.Tags.Remove(tagName);
                    repo.Update(selected);
                }
                _view.WriteLine();
            }
        } while (!string.IsNullOrEmpty(tagName));
    }
}