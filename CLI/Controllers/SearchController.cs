using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using Humanizer;
using Spectre.Console;

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

    public void SearchDone(DoneSearchSubOptions search, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;

        // Construct a search specification.
        // Set the ViewModel property.
        vm.DoneSearchSpecification = search.GetSearchSpecification(repo);
        // Report the results.
        view.PrintItems(search.SortTag ?? "done-date", vm.DoneSearchResults, repo, search.NSFW, search.Verbose);
        if (!search.NoCount)
        {
            view.WriteLine("{0} item(s) found.", vm.DoneSearchResults.Count());
        }
    }

    public void SearchSomeday(SomedaySearchSubOptions search, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;

        // Construct a search specification.
        // Set the ViewModel property.
        vm.SomedaySearchSpecification = search.GetSearchSpecification(repo);
        // Report the results.
        view.PrintItems(search.SortTag ?? "tickle-date", vm.SomedaySearchResults, repo, search.NSFW, search.Verbose);
        if (!search.NoCount)
        {
            view.WriteLine("{0} item(s) found.", vm.SomedaySearchResults.Count());
        }
    }

    public void Summary(UniversalOptions summaryArgs, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        if (!summaryArgs.ShowAllItems)
        {
            vm.ShowHeadOnly = !summaryArgs.Verbose;
        }

        vm.SearchSpecification = new TrueSpecification<ActionItem>();
        var summaryData = from i in vm.SearchResults group i by i.Context into c select new { Context = c.Key, Count = c.Count() };

        var table = new Table();
        table.AddColumn(new TableColumn("Context").Padding(2, 0));
        table.AddColumn(new TableColumn("Count").RightAligned().Padding(2, 0));
        var total = 0;
        var secondOrLaterContext = false;
        foreach (var c in summaryData)
        {
            total += c.Count;
            if (secondOrLaterContext && summaryArgs.Verbose)
            {
                table.AddEmptyRow();
            }
            table.AddRow($"@{c.Context}", "item".ToQuantity(c.Count));
            secondOrLaterContext = true;
            if (summaryArgs.Verbose)
            {
                // Show a summary of numbers at each depth.
                vm.SearchSpecification = new ContextSearchSpecification(c.Context);
                var depths = vm.GetDepthsView();

                var detailed = new Dictionary<int, int>();
                var unknownCount = 0;
                foreach (var i in vm.SearchResults)
                {
                    if (depths.ContainsKey(i.ID))
                    {
                        var deep = depths[i.ID];
                        if (detailed.ContainsKey(deep))
                        {
                            detailed[deep]++;
                        }
                        else
                        {
                            detailed[deep] = 1;
                        }
                    }
                    else
                    {
                        unknownCount++;
                    }
                }
                foreach (var d in detailed.OrderBy(r => r.Key))
                {
                    table.AddRow(new TableRow([new Markup(d.Key.ToString()).RightJustified(), new Markup("item".ToQuantity(d.Value))]));
                }
                if (unknownCount > 0)
                {
                    table.AddRow("-", "item".ToQuantity(unknownCount));
                }
            }
        }
        table.AddEmptyRow();
        table.AddRow("Total", "item".ToQuantity(total));
        AnsiConsole.Write(table);
    }
}