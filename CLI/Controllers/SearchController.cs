using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class SearchController
{
    public void AdvancedSearch(AdvancedSearchOptions argSubs, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !argSubs.ShowAllItems;

        vm.SearchSpecification = argSubs.SearchSpecification;
        view.PrintItems("title", vm.SearchResults, repo, argSubs.NSFW, argSubs.Verbose);

        vm.Save();
    }
}