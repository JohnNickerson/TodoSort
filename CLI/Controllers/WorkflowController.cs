using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class WorkflowController
{
    public void Done(DoneSubOptions doneOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !doneOpts.ShowAllItems;

        // If there is a next action, create a new item and add it to the correct context.
        vm.SearchSpecification = doneOpts.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(doneOpts.ItemId));
        if (selected != null)
        {
            vm.MarkDone(doneOpts.DoneDate, selected);
            view.PrintItem(selected, null, repo, false, doneOpts.Verbose);
            vm.Save();
        }
    }
}