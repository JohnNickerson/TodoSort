using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class WorkflowController
{
    public void Defer(DeferSubOptions deferOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !deferOpts.ShowAllItems;

        vm.SearchSpecification = deferOpts.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(deferOpts.ItemId));
        if (selected != null)
        {
            if (deferOpts.TickleDate.HasValue)
            {
                vm.Defer(selected, deferOpts.TickleDate.Value);
            }
            else
            {
                vm.Defer(selected);
            }
            view.PrintItem(selected, null, repo, false, deferOpts.Verbose);
            repo.SaveChanges();
        }
    }

    public void DeferAll(DeferAllOptions pruneOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;

        vm.SearchSpecification = pruneOptions.GetSearchSpecification(repo);
        view.WriteLine("About to defer {0} items. Continue [Y/N]?", vm.SearchResults.Count());
        var k = Console.ReadKey();
        if (k.KeyChar.ToString().ToLower() == "y")
        {
            vm.Defer(vm.SearchResults.ToArray());
        }

        repo.SaveChanges();
    }

    public void Done(DoneSubOptions doneOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !doneOpts.ShowAllItems;

        // If there is a next action, create a new item and add it to the correct context.
        vm.SearchSpecification = doneOpts.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(doneOpts.ItemId));
        if (selected != null)
        {
            vm.MarkDone(doneOpts.DoneDate, selected);
            view.PrintItem(selected, null, repo, false, doneOpts.Verbose);
            repo.SaveChanges();
        }
    }

    public void Undefer(UndeferOptions undeferOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !undeferOptions.ShowAllItems;

        vm.SomedaySearchSpecification = undeferOptions.SearchSpecification;
        var selected = view.Disambiguate(vm.SomedaySearchResults, repo, !string.IsNullOrEmpty(undeferOptions.ItemId));
        if (selected != null)
        {
            vm.Undefer("inbox", selected);
            view.PrintItem(selected, null, repo, false, undeferOptions.Verbose);
            repo.SaveChanges();
        }
    }

    public void Undo(UndoSubOptions undoOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !undoOptions.ShowAllItems;

        vm.DoneSearchSpecification = undoOptions.SearchSpecification;
        var selected = view.Disambiguate(vm.DoneSearchResults, repo, !string.IsNullOrEmpty(undoOptions.ItemId));
        if (selected != null)
        {
            vm.Undo(undoOptions.NewContext, selected);
            view.PrintItem(selected, null, repo, false, undoOptions.Verbose);
            repo.SaveChanges();
        }
    }
}