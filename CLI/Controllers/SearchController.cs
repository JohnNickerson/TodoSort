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

    public void AdvancedSearch(AdvancedSearchOptions argSubs, TodoRepository repo, IItemDisplay view)
    {
        var searchResults = PerformSearch(repo, argSubs.SearchSpecification, argSubs.ShowAllItems);
        view.PrintItems("title", searchResults, repo, argSubs.NSFW, argSubs.Verbose);
    }

    private IEnumerable<ActionItem> PerformSearch(TodoRepository repo, ISearchSpecification<ActionItem> searchSpecification, bool showAllItems)
    {
        if (!showAllItems)
        {
            return repo.Items.Where(i => searchSpecification.And(new HeadOnlySearchSpecification()).IsSatisfiedBy(i));
        }
        else
        {
            return repo.Items.Where(i => searchSpecification.IsSatisfiedBy(i));
        }
    }

    public void Search(SearchOptions searchOptions, TodoRepository repo, IItemDisplay view)
    {
        var searchResults = PerformSearch(repo, searchOptions.GetSearchSpecification(repo), searchOptions.ShowAllItems);
        if (searchResults.Count() == 0)
        {
            view.WriteLine("No results found.");
        }
        else if (searchOptions.PrintTree)
        {
            view.PrintTree(searchResults.ToList(), new List<ActionItem>(), repo, searchOptions.NSFW, searchOptions.Verbose);
        }
        else
        {
            view.PrintItems(searchOptions.SortTag ?? string.Empty, searchResults, repo, searchOptions.NSFW, searchOptions.Verbose);
            if (!searchOptions.NoCount)
            {
                view.WriteLine("{0} item(s) found.", searchResults.Count());
            }
        }
    }

    public void SearchDone(DoneSearchSubOptions search, TodoRepository repo, IItemDisplay view)
    {
        // Construct a search specification.
        var doneSearchResults = repo.DoneItems.Where(d => search.GetSearchSpecification(repo).IsSatisfiedBy(d));
        // Report the results.
        view.PrintItems(search.SortTag ?? "done-date", doneSearchResults, repo, search.NSFW, search.Verbose);
        if (!search.NoCount)
        {
            view.WriteLine("{0} item(s) found.", doneSearchResults.Count());
        }
    }

    public void SearchSomeday(SomedaySearchSubOptions search, TodoRepository repo, IItemDisplay view)
    {
        // Construct a search specification.
        var somedaySearchResults = repo.SomedayItems.Where(s => search.GetSearchSpecification(repo).IsSatisfiedBy(s));
        // Report the results.
        view.PrintItems(search.SortTag ?? "tickle-date", somedaySearchResults, repo, search.NSFW, search.Verbose);
        if (!search.NoCount)
        {
            view.WriteLine("{0} item(s) found.", somedaySearchResults.Count());
        }
    }

    public void Summary(UniversalOptions summaryArgs, ViewModel vm)
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