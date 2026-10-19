using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class WorkflowController
{
    private readonly IItemDisplay _view;

    public WorkflowController(IItemDisplay view)
    {
        _view = view;
    }

    public void Defer(DeferSubOptions deferOpts, TodoService vm)
    {
        vm.ShowHeadOnly = !deferOpts.ShowAllItems;

        vm.SearchSpecification = deferOpts.SearchSpecification;
        var selected = _view.Disambiguate(vm.SearchResults, vm, !string.IsNullOrEmpty(deferOpts.ItemId));
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
            _view.PrintItem(selected, null, vm, false, deferOpts.Verbose);
            vm.Save();
        }
    }

    public void DeferAll(DeferAllOptions pruneOptions, TodoService vm)
    {
        vm.ShowHeadOnly = false;

        vm.SearchSpecification = pruneOptions.GetSearchSpecification();
        _view.WriteLine("About to defer {0} items. Continue [Y/N]?", vm.SearchResults.Count());
        var k = Console.ReadKey();
        if (k.KeyChar.ToString().ToLower() == "y")
        {
            vm.Defer(vm.SearchResults.ToArray());
        }

        vm.Save();
    }

    public void Done(DoneSubOptions doneOpts, TodoService vm)
    {
        vm.ShowHeadOnly = !doneOpts.ShowAllItems;

        // If there is a next action, create a new item and add it to the correct context.
        vm.SearchSpecification = doneOpts.SearchSpecification;
        var selected = _view.Disambiguate(vm.SearchResults, vm, !string.IsNullOrEmpty(doneOpts.ItemId));
        if (selected != null)
        {
            vm.MarkDone(doneOpts.DoneDate, selected);
            _view.PrintItem(selected, null, vm, false, doneOpts.Verbose);
            vm.Save();
        }
    }

    public void Undefer(UndeferOptions undeferOptions, TodoService vm)
    {
        vm.ShowHeadOnly = !undeferOptions.ShowAllItems;

        vm.SomedaySearchSpecification = undeferOptions.SearchSpecification;
        var selected = _view.Disambiguate(vm.SomedaySearchResults, vm, !string.IsNullOrEmpty(undeferOptions.ItemId));
        if (selected != null)
        {
            vm.Undefer("inbox", selected);
            _view.PrintItem(selected, null, vm, false, undeferOptions.Verbose);
            vm.Save();
        }
    }

    public void Undo(UndoSubOptions undoOptions, TodoService vm)
    {
        vm.ShowHeadOnly = !undoOptions.ShowAllItems;

        vm.DoneSearchSpecification = undoOptions.SearchSpecification;
        var selected = _view.Disambiguate(vm.DoneSearchResults, vm, !string.IsNullOrEmpty(undoOptions.ItemId));
        if (selected != null)
        {
            vm.Undo(undoOptions.NewContext, selected);
            _view.PrintItem(selected, null, vm, false, undoOptions.Verbose);
            vm.Save();
        }
    }
}