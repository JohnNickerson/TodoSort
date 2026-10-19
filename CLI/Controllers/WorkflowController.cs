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

    public void Defer(DeferSubOptions deferOpts, TodoService svc)
    {
        svc.ShowHeadOnly = !deferOpts.ShowAllItems;

        svc.SearchSpecification = deferOpts.SearchSpecification;
        var selected = _view.Disambiguate(svc.SearchResults, svc, !string.IsNullOrEmpty(deferOpts.ItemId));
        if (selected != null)
        {
            if (deferOpts.TickleDate.HasValue)
            {
                svc.Defer(selected, deferOpts.TickleDate.Value);
            }
            else
            {
                svc.Defer(selected);
            }
            _view.PrintItem(selected, null, svc, false, deferOpts.Verbose);
            svc.Save();
        }
    }

    public void DeferAll(DeferAllOptions pruneOptions, TodoService svc)
    {
        svc.ShowHeadOnly = false;

        svc.SearchSpecification = pruneOptions.GetSearchSpecification();
        _view.WriteLine("About to defer {0} items. Continue [Y/N]?", svc.SearchResults.Count());
        var k = Console.ReadKey();
        if (k.KeyChar.ToString().ToLower() == "y")
        {
            svc.Defer(svc.SearchResults.ToArray());
        }

        svc.Save();
    }

    public void Done(DoneSubOptions doneOpts, TodoService svc)
    {
        svc.ShowHeadOnly = !doneOpts.ShowAllItems;

        // If there is a next action, create a new item and add it to the correct context.
        svc.SearchSpecification = doneOpts.SearchSpecification;
        var selected = _view.Disambiguate(svc.SearchResults, svc, !string.IsNullOrEmpty(doneOpts.ItemId));
        if (selected != null)
        {
            svc.MarkDone(doneOpts.DoneDate, selected);
            _view.PrintItem(selected, null, svc, false, doneOpts.Verbose);
            svc.Save();
        }
    }

    public void Undefer(UndeferOptions undeferOptions, TodoService svc)
    {
        svc.ShowHeadOnly = !undeferOptions.ShowAllItems;

        svc.SomedaySearchSpecification = undeferOptions.SearchSpecification;
        var selected = _view.Disambiguate(svc.SomedaySearchResults, svc, !string.IsNullOrEmpty(undeferOptions.ItemId));
        if (selected != null)
        {
            svc.Undefer("inbox", selected);
            _view.PrintItem(selected, null, svc, false, undeferOptions.Verbose);
            svc.Save();
        }
    }

    public void Undo(UndoSubOptions undoOptions, TodoService svc)
    {
        svc.ShowHeadOnly = !undoOptions.ShowAllItems;

        svc.DoneSearchSpecification = undoOptions.SearchSpecification;
        var selected = _view.Disambiguate(svc.DoneSearchResults, svc, !string.IsNullOrEmpty(undoOptions.ItemId));
        if (selected != null)
        {
            svc.Undo(undoOptions.NewContext, selected);
            _view.PrintItem(selected, null, svc, false, undoOptions.Verbose);
            svc.Save();
        }
    }
}