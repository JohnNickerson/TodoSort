using AssimilationSoftware.Maroon.Model;
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
    }

    public void Search(SearchOptions searchOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;

        vm.SearchSpecification = searchOptions.GetSearchSpecification(repo);
        if (vm.SearchResults.Count() == 0)
        {
            view.WriteLine("No results found.");
        }
        else if (searchOptions.PrintTree)
        {
            view.PrintTree(vm.SearchResults.ToList(), new List<ActionItem>(), repo, searchOptions.NSFW, searchOptions.Verbose);
        }
        else
        {
            view.PrintItems(searchOptions.SortTag ?? string.Empty, vm.SearchResults, repo, searchOptions.NSFW, searchOptions.Verbose);
            if (!searchOptions.NoCount)
            {
                view.WriteLine("{0} item(s) found.", vm.SearchResults.Count());
            }
        }
    }
}