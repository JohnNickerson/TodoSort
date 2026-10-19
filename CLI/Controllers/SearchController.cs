using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;
using Humanizer;
using Spectre.Console;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class SearchController
{
    private readonly IItemDisplay _view;

    public SearchController(IItemDisplay view)
    {
        _view = view;
    }

    public void AdvancedSearch(AdvancedSearchOptions argSubs, TodoService svc)
    {
        var searchResults = svc.Search(argSubs.SearchSpecification, argSubs.ShowAllItems);
        _view.PrintItems("title", searchResults, svc, argSubs.NSFW, argSubs.Verbose);
    }

    public void Search(SearchOptions searchOptions, TodoService svc)
    {
        svc.SearchSpecification = searchOptions.GetSearchSpecification();
        var searchResults = svc.SearchResults;
        if (searchResults.Count() == 0)
        {
            _view.WriteLine("No results found.");
        }
        else if (searchOptions.PrintTree)
        {
            _view.PrintTree(searchResults.ToList(), new List<ActionItem>(), svc, searchOptions.NSFW, searchOptions.Verbose);
        }
        else
        {
            _view.PrintItems(searchOptions.SortTag ?? string.Empty, searchResults, svc, searchOptions.NSFW, searchOptions.Verbose);
            if (!searchOptions.NoCount)
            {
                _view.WriteLine("{0} item(s) found.", searchResults.Count());
            }
        }
    }

    public void SearchDone(DoneSearchSubOptions search, TodoService svc)
    {
        svc.DoneSearchSpecification = search.GetSearchSpecification();
        var doneSearchResults = svc.DoneSearchResults;
        _view.PrintItems(search.SortTag ?? "done-date", doneSearchResults, svc, search.NSFW, search.Verbose);
        if (!search.NoCount)
        {
            _view.WriteLine("{0} item(s) found.", doneSearchResults.Count());
        }
    }

    public void SearchSomeday(SomedaySearchSubOptions search, TodoService svc)
    {
        svc.SomedaySearchSpecification = search.GetSearchSpecification();
        var somedaySearchResults = svc.SomedaySearchResults;
        _view.PrintItems(search.SortTag ?? "tickle-date", somedaySearchResults, svc, search.NSFW, search.Verbose);
        if (!search.NoCount)
        {
            _view.WriteLine("{0} item(s) found.", somedaySearchResults.Count());
        }
    }

    public void Summary(UniversalOptions summaryArgs, TodoService svc)
    {
        svc.ShowHeadOnly = !summaryArgs.Verbose && !summaryArgs.ShowAllItems;

        svc.SearchSpecification = new TrueSpecification<ActionItem>();
        var summaryData = from i in svc.SearchResults group i by i.Context into c select new { Context = c.Key, Count = c.Count() };

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
                svc.SearchSpecification = new ContextSearchSpecification(c.Context);
                var depths = svc.GetDepthsView();

                var detailed = new Dictionary<int, int>();
                var unknownCount = 0;
                foreach (var i in svc.SearchResults)
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