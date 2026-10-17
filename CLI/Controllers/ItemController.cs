using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;

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

    public void Delete(DeleteOptions delOpts, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = !delOpts.ShowAllItems;

        // Find a matching item to delete.
        vm.SearchSpecification = delOpts.SearchSpecification;
        var selected = _view.Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(delOpts.ItemId));
        if (selected != null)
        {
            vm.Delete(selected);
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

    public void Move(MoveSubOptions moveOptions, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = !moveOptions.ShowAllItems;

        vm.SearchSpecification = moveOptions.SearchSpecification;
        var selected = _view.Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(moveOptions.ItemId));
        if (selected != null)
        {
            vm.SetContext(selected, moveOptions.NewContext);
            if (moveOptions.Unrank)
            {
                vm.ResetPriorityParents(selected);
            }
            _view.PrintItem(selected, null, repo, false, moveOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void MoveAll(MoveAllSubOptions moveAllOptions, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = false;

        vm.ShowHeadOnly = false; // Make sure we move all items in the context, not just the head.
        vm.SearchSpecification = moveAllOptions.GetSearchSpecification();
        var items = vm.SearchResults.ToList();
        var counter = 0;
        while (items.Count > 0)
        {
            vm.SetContext(items[0], moveAllOptions.NewContext);
            items.RemoveAt(0);
            counter++;
        }
        _view.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));

        repo.SaveChanges();
    }

    public void Note(NoteSubOptions noteOptions, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = !noteOptions.ShowAllItems;

        // Add a note to a task.
        vm.SearchSpecification = noteOptions.SearchSpecification;
        var selected = _view.Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(noteOptions.ItemId));
        if (selected != null)
        {
            vm.AddNote(selected, noteOptions.NewNote);
            _view.PrintItem(selected, null, repo, false, noteOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void Rename(RenameSubOptions renameOptions, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = !renameOptions.ShowAllItems;

        vm.SearchSpecification = renameOptions.SearchSpecification;
        var selected = _view.Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(renameOptions.ItemId));
        if (selected != null)
        {
            vm.Rename(selected, renameOptions.NewTitle);
            _view.WriteLine("Item renamed.");
            if (renameOptions.ReTag)
            {
                TagItem(repo, selected);
            }
            _view.PrintItem(selected, null, repo, false, renameOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void SetProject(SetProjectSubOptions commandOptions, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = !commandOptions.ShowAllItems;

        _view.WriteLine("Confirm child item:");
        vm.SearchTerm = commandOptions.ChildSearchTerm;
        var child = _view.Disambiguate(vm.SearchResults, repo);
        _view.WriteLine("Confirm project:");
        vm.SearchTerm = commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm;
        var project = _view.Disambiguate(vm.SearchResults, repo);
        if (child != null && project != null)
        {
            vm.SetProject(child, project);
        }

        repo.SaveChanges();
    }

    public void Tag(SingleSearchSubOptions tagOptions, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = !tagOptions.ShowAllItems;

        // Search for a matching item.
        vm.SearchSpecification = tagOptions.SearchSpecification;
        var selected = _view.Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(tagOptions.ItemId));
        if (selected != null)
        {
            TagItem(repo, selected);
            _view.PrintItem(selected, null, repo, false, tagOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void TagAll(TagAllSubOptions tagAllOptions, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = !tagAllOptions.ShowAllItems;

        vm.SearchSpecification = tagAllOptions.SearchSpecification;
        var items = vm.SearchResults.ToArray();
        foreach (var i in items)
        {
            if (string.IsNullOrWhiteSpace(tagAllOptions.TagValue))
            {
                vm.RemoveTag(i, tagAllOptions.TagName);
            }
            else
            {
                vm.SetTag(i, tagAllOptions.TagName, tagAllOptions.TagValue);
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