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

    public void AddItem(AddSubOptions a, TodoService vm, TodoRepository repo)
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
        TagItem(vm, selected);
        _view.PrintItem(selected, null, vm, false, true);
        vm.Save();
    }

    public void Delete(DeleteOptions delOpts, TodoService vm, TodoRepository repo)
    {
        // Find a matching item to delete.
        var searchResults = vm.Search(delOpts.SearchSpecification, delOpts.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, vm, !string.IsNullOrEmpty(delOpts.ItemId));
        if (selected != null)
        {
            vm.Delete(selected);
            _view.PrintItem(selected, null, vm, false, delOpts.Verbose);
            vm.Save();
        }
    }

    public void Merge(MergeSubOptions mergeOptions, TodoService vm, TodoRepository repo)
    {
        _view.WriteLine("Confirm child item:");
        vm.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
        var mergeVictim = _view.Disambiguate(vm.SearchResults, vm);
        if (mergeVictim != null)
        {
            _view.WriteLine("Confirm item to merge into:");
            vm.SearchTerm = mergeOptions.TargetSearchTerm;
            var combined = _view.Disambiguate(vm.SearchResults.Where(x => x.ID != mergeVictim.ID), vm);
            if (mergeVictim != null && combined != null)
            {
                vm.Merge(mergeVictim, combined);
                _view.PrintItem(combined, null, vm, false, mergeOptions.Verbose);
                vm.Save();
            }
        }
    }

    public void Move(MoveSubOptions moveOptions, TodoService vm, TodoRepository repo)
    {
        var searchResults = vm.Search(moveOptions.SearchSpecification, moveOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, vm, !string.IsNullOrEmpty(moveOptions.ItemId));
        if (selected != null)
        {
            selected.Context = moveOptions.NewContext;
            if (moveOptions.Unrank)
            {
                selected.ParentId = null;
                foreach (var child in vm.GetChildren(selected))
                {
                    child.ParentId = null;
                    vm.Update(child);
                }
            }
            vm.Update(selected);
            _view.PrintItem(selected, null, vm, false, moveOptions.Verbose);
            vm.Save();
        }
    }

    public void MoveAll(MoveAllSubOptions moveAllOptions, TodoService vm, TodoRepository repo)
    {
        var items = vm.Search(moveAllOptions.GetSearchSpecification(), moveAllOptions.ShowAllItems).ToList();
        var counter = 0;
        while (items.Count > 0)
        {
            items[0].Context = moveAllOptions.NewContext;
            vm.Update(items[0]);
            items.RemoveAt(0);
            counter++;
        }
        _view.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));

        vm.Save();
    }

    public void Note(NoteSubOptions noteOptions, TodoService vm, TodoRepository repo)
    {
        // Add a note to a task.
        var searchResults = vm.Search(noteOptions.SearchSpecification, noteOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, vm, !string.IsNullOrEmpty(noteOptions.ItemId));
        if (selected != null)
        {
            selected.Notes.Add(string.Format("{0} ({1:yyyy-MM-dd})", noteOptions.NewNote, DateTime.Now));
            vm.Update(selected);
            _view.PrintItem(selected, null, vm, false, noteOptions.Verbose);
            vm.Save();
        }
    }

    public void Rename(RenameSubOptions renameOptions, TodoService vm, TodoRepository repo)
    {
        var searchResults = vm.Search(renameOptions.SearchSpecification, renameOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, vm, !string.IsNullOrEmpty(renameOptions.ItemId));
        if (selected != null)
        {
            selected.Title = renameOptions.NewTitle;
            vm.Update(selected);
            _view.WriteLine("Item renamed.");
            if (renameOptions.ReTag)
            {
                TagItem(vm, selected);
            }
            _view.PrintItem(selected, null, vm, false, renameOptions.Verbose);
            vm.Save();
        }
    }

    public void SetProject(SetProjectSubOptions commandOptions, TodoService vm, TodoRepository repo)
    {
        _view.WriteLine("Confirm child item:");
        var childSearchResults = vm.Search(new FullTextSearchSpecification(commandOptions.ChildSearchTerm), commandOptions.ShowAllItems);
        var child = _view.Disambiguate(childSearchResults, vm);

        _view.WriteLine("Confirm project:");
        var projectSearchResults = vm.Search(new FullTextSearchSpecification(commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm), commandOptions.ShowAllItems);
        var project = _view.Disambiguate(projectSearchResults, vm);
        if (child != null && project != null)
        {
            child.ProjectId = project.ID;
            vm.Update(child);
        }

        vm.Save();
    }

    public void Tag(SingleSearchSubOptions tagOptions, TodoService vm, TodoRepository repo)
    {
        // Search for a matching item.
        var searchResults = vm.Search(tagOptions.SearchSpecification, tagOptions.ShowAllItems);
        var selected = _view.Disambiguate(searchResults, vm, !string.IsNullOrEmpty(tagOptions.ItemId));
        if (selected != null)
        {
            TagItem(vm, selected);
            _view.PrintItem(selected, null, vm, false, tagOptions.Verbose);
            vm.Save();
        }
    }

    public void TagAll(TagAllSubOptions tagAllOptions, TodoService vm, TodoRepository repo)
    {
        if (string.IsNullOrWhiteSpace(tagAllOptions.TagName))
        {
            _view.WriteLine("Tag name cannot be empty.");
            return;
        }

        var items = vm.Search(tagAllOptions.SearchSpecification, tagAllOptions.ShowAllItems).ToArray();
        foreach (var i in items)
        {
            if (string.IsNullOrWhiteSpace(tagAllOptions.TagValue))
            {
                i.Tags.Remove(tagAllOptions.TagName);
                vm.Update(i);
            }
            else
            {
                i.Tags[tagAllOptions.TagName] = tagAllOptions.TagValue;
                vm.Update(i);
            }
        }

        vm.Save();
    }

    private void TagItem(TodoService vm, ActionItem selected)
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
                    vm.Update(selected);
                }
                else if (selected.Tags.ContainsKey(tagName))
                {
                    selected.Tags.Remove(tagName);
                    vm.Update(selected);
                }
                _view.WriteLine();
            }
        } while (!string.IsNullOrEmpty(tagName));
    }
}