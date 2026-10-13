using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class ItemController
{
    public void AddItem(AddSubOptions a, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;
        var selected = new ActionItem { Context = a.Context, Title = a.ActionTitle };
        if (!string.IsNullOrWhiteSpace(a.Note))
        {
            selected.Notes.Add(a.Note);
        }
        view.WriteLine("Adding action '{0}' to context @{1}", a.ActionTitle, a.Context);
        vm.AddItem(selected);
        // Allow tagging right away.
        TagItem(vm, selected, view);
        view.PrintItem(selected, null, repo, false, true);
        vm.Save();
    }

    public void Delete(DeleteOptions delOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !delOpts.ShowAllItems;

        // Find a matching item to delete.
        vm.SearchSpecification = delOpts.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(delOpts.ItemId));
        if (selected != null)
        {
            vm.Delete(selected);
            view.PrintItem(selected, null, repo, false, delOpts.Verbose);
            vm.Save();
        }
    }

    public void Merge(MergeSubOptions mergeOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            view.WriteLine("Confirm child item:");
            vm.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
            var mergeVictim = view.Disambiguate(vm.SearchResults, repo, view);
            if (mergeVictim != null)
            {
                view.WriteLine("Confirm item to merge into:");
                vm.SearchTerm = mergeOptions.TargetSearchTerm;
                var combined = view.Disambiguate(vm.SearchResults.Where(x => x.ID != mergeVictim.ID), repo, view);
                if (mergeVictim != null && combined != null)
                {
                    vm.Merge(mergeVictim, combined);
                    view.PrintItem(combined, null, repo, false, mergeOptions.Verbose);
                    vm.Save();
                }
            }
        }

    public void Move(MoveSubOptions moveOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !moveOptions.ShowAllItems;

        vm.SearchSpecification = moveOptions.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(moveOptions.ItemId));
        if (selected != null)
        {
            vm.SetContext(selected, moveOptions.NewContext);
            if (moveOptions.Unrank)
            {
                vm.ResetPriorityParents(selected);
            }
            view.PrintItem(selected, null, repo, false, moveOptions.Verbose);
            vm.Save();
        }
    }

    public void MoveAll(MoveAllSubOptions moveAllOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;

        vm.ShowHeadOnly = false; // Make sure we move all items in the context, not just the head.
        vm.SearchSpecification = moveAllOptions.GetSearchSpecification(repo);
        var items = vm.SearchResults.ToList();
        var counter = 0;
        while (items.Count > 0)
        {
            vm.SetContext(items[0], moveAllOptions.NewContext);
            items.RemoveAt(0);
            counter++;
        }
        view.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));

        vm.Save();
    }

    public void Note(NoteSubOptions noteOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !noteOptions.ShowAllItems;

        // Add a note to a task.
        vm.SearchSpecification = noteOptions.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(noteOptions.ItemId));
        if (selected != null)
        {
            vm.AddNote(selected, noteOptions.NewNote);
            view.PrintItem(selected, null, repo, false, noteOptions.Verbose);
            vm.Save();
        }
    }

    public void Rename(RenameSubOptions renameOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !renameOptions.ShowAllItems;

        vm.SearchSpecification = renameOptions.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(renameOptions.ItemId));
        if (selected != null)
        {
            vm.Rename(selected, renameOptions.NewTitle);
            view.WriteLine("Item renamed.");
            if (renameOptions.ReTag)
            {
                TagItem(vm, selected, view);
            }
            view.PrintItem(selected, null, repo, false, renameOptions.Verbose);
            vm.Save();
        }
    }

    public void SetProject(SetProjectSubOptions commandOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !commandOptions.ShowAllItems;

        view.WriteLine("Confirm child item:");
        vm.SearchTerm = commandOptions.ChildSearchTerm;
        var child = view.Disambiguate(vm.SearchResults, repo, view);
        view.WriteLine("Confirm project:");
        vm.SearchTerm = commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm;
        var project = view.Disambiguate(vm.SearchResults, repo, view);
        if (child != null && project != null)
        {
            vm.SetProject(child, project);
        }

        vm.Save();
    }

    public void Tag(SingleSearchSubOptions tagOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !tagOptions.ShowAllItems;

        // Search for a matching item.
        vm.SearchSpecification = tagOptions.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(tagOptions.ItemId));
        if (selected != null)
        {
            TagItem(vm, selected, view);
            view.PrintItem(selected, null, repo, false, tagOptions.Verbose);
            vm.Save();
        }
    }

    public void TagAll(TagAllSubOptions tagAllOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
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

        vm.Save();
    }

    private void TagItem(ViewModel vm, ActionItem selected, IItemDisplay view)
    {
        string? tagName;
        do
        {
            view.WriteLine("What should this new tag be called? (ENTER to quit)");
            tagName = Console.ReadLine()?.ToLower()?.Trim();
            if (!string.IsNullOrEmpty(tagName))
            {
                view.WriteLine("What is the value of the tag?");
                var value = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    vm.SetTag(selected, tagName, value);
                }
                else if (selected.Tags.ContainsKey(tagName))
                {
                    vm.RemoveTag(selected, tagName);
                }
                view.WriteLine();
            }
        } while (!string.IsNullOrEmpty(tagName));
    }
}