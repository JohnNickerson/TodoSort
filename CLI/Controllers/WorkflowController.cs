using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class WorkflowController
{
    private readonly IItemDisplay _view;
    private readonly TodoService _svc;

    public WorkflowController(TodoService svc, IItemDisplay view)
    {
        _view = view;
        _svc = svc;
    }

    public void Defer(DeferSubOptions deferOpts)
    {
        _svc.ShowHeadOnly = !deferOpts.ShowAllItems;

        _svc.SearchSpecification = deferOpts.SearchSpecification;
        var selected = _view.Disambiguate(_svc.SearchResults, _svc, !string.IsNullOrEmpty(deferOpts.ItemId));
        if (selected != null)
        {
            if (deferOpts.TickleDate.HasValue)
            {
                _svc.Defer(selected, deferOpts.TickleDate.Value);
            }
            else
            {
                _svc.Defer(selected);
            }
            _view.PrintItem(selected, null, _svc, false, deferOpts.Verbose);
            _svc.Save();
        }
    }

    public void DeferAll(DeferAllOptions pruneOptions)
    {
        _svc.ShowHeadOnly = false;

        _svc.SearchSpecification = pruneOptions.GetSearchSpecification();
        _view.WriteLine("About to defer {0} items. Continue [Y/N]?", _svc.SearchResults.Count());
        var k = Console.ReadKey();
        if (k.KeyChar.ToString().ToLower() == "y")
        {
            _svc.Defer(_svc.SearchResults.ToArray());
        }

        _svc.Save();
    }

    public void Done(DoneSubOptions doneOpts)
    {
        _svc.ShowHeadOnly = !doneOpts.ShowAllItems;

        // If there is a next action, create a new item and add it to the correct context.
        _svc.SearchSpecification = doneOpts.SearchSpecification;
        var selected = _view.Disambiguate(_svc.SearchResults, _svc, !string.IsNullOrEmpty(doneOpts.ItemId));
        if (selected != null)
        {
            _svc.MarkDone(doneOpts.DoneDate, selected);
            _view.PrintItem(selected, null, _svc, false, doneOpts.Verbose);
            _svc.Save();
        }
    }

    public void Undefer(UndeferOptions undeferOptions)
    {
        _svc.ShowHeadOnly = !undeferOptions.ShowAllItems;

        _svc.SomedaySearchSpecification = undeferOptions.SearchSpecification;
        var selected = _view.Disambiguate(_svc.SomedaySearchResults, _svc, !string.IsNullOrEmpty(undeferOptions.ItemId));
        if (selected != null)
        {
            _svc.Undefer("inbox", selected);
            _view.PrintItem(selected, null, _svc, false, undeferOptions.Verbose);
            _svc.Save();
        }
    }

    public void Undo(UndoSubOptions undoOptions)
    {
        _svc.ShowHeadOnly = !undoOptions.ShowAllItems;

        _svc.DoneSearchSpecification = undoOptions.SearchSpecification;
        var selected = _view.Disambiguate(_svc.DoneSearchResults, _svc, !string.IsNullOrEmpty(undoOptions.ItemId));
        if (selected != null)
        {
            _svc.Undo(undoOptions.NewContext, selected);
            _view.PrintItem(selected, null, _svc, false, undoOptions.Verbose);
            _svc.Save();
        }
    }
}