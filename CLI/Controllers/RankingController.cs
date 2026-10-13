using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using Spectre.Console;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class RankingController
{
    public void Rank(MultiSearchSubOptions rankOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        // for each context..
        var quitAndSave = false;
        vm.ShowHeadOnly = true;
        var contextList = vm.GetContextNames("inbox", "done").ToArray();
        for (int contextIndex = 0; contextIndex < contextList.Length; contextIndex++)
        {
            var con = contextList[contextIndex];
            if (quitAndSave) break;
            // select all items without rank parents
            vm.SearchSpecification = new ContextSearchSpecification(con).And(rankOptions.GetSearchSpecification(repo));
            var items = vm.SearchResults.ToArray();
            var index = new List<int>();
            for (var dex = 0; dex < items.Count(); dex++)
            {
                index.Add(dex);
            }
            // randomise an index list
            var rand = new Random();
            for (var dex = 0; dex < index.Count; dex++)
            {
                var r = rand.Next(index.Count);
                var b = index[dex];
                index[dex] = index[r];
                index[r] = b;
            }
            // show pairs of items
            if (items.Count() > 1)
            {
                view.WriteLine(string.Format("{1}{1}@{0}", con, Environment.NewLine));
            }
            for (var x = 0; x < items.Count() - 1; x += 2)
            {
                if (quitAndSave) break;
                // get vote
                view.WriteLine("{0}/{1} ({2}%) complete", x, items.Count(), 100 * x / items.Count());
                view.PrintItem(items.ElementAt(index[x]), 1, repo, rankOptions.NSFW, rankOptions.Verbose);
                view.PrintItem(items.ElementAt(index[x + 1]), 2, repo, rankOptions.NSFW, rankOptions.Verbose);
                var choice = AnsiConsole.Prompt(new Spectre.Console.TextPrompt<string>("Which of these is more important? (q=quit and save, c=cancel)")
                    .AddChoices(["1", "2", "q", "c"]));
                // assign parents based on vote
                switch (choice)
                {
                    case "1":
                        vm.SetParent(items.ElementAt(index[x + 1]), items.ElementAt(index[x]));
                        break;
                    case "2":
                        vm.SetParent(items.ElementAt(index[x]), items.ElementAt(index[x + 1]));
                        break;
                    case "q":
                        quitAndSave = true;
                        view.WriteLine();
                        view.WriteLine("Saving changes.");
                        break;
                    case "c":
                        view.WriteLine();
                        view.WriteLine("Cancelling. No changes saved.");
                        return;
                }
                view.WriteLine();
            }
        }

        vm.Save();
    }

    public void Unrank(UnRankOptions unrankOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !unrankOptions.ShowAllItems;

        vm.SearchSpecification = unrankOptions.SearchSpecification;
        var selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(unrankOptions.ItemId));
        if (selected != null)
        {
            vm.ResetPriorityParents(selected);
            view.PrintItem(selected, null, repo, false, unrankOptions.Verbose);
            vm.Save();
        }
    }

    public void UnrankAll(UnrankAllOptions unrankAllOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;

        vm.SearchSpecification = unrankAllOptions.GetSearchSpecification(repo);
        view.WriteLine("About to delete all ranking data for {0} items. Continue [Y/N]?", vm.SearchResults.Count());
        if (Console.ReadKey().KeyChar.ToString().ToLower() == "y")
        {
            vm.ResetPriorityParents(vm.SearchResults.ToArray());
        }

        vm.Save();
    }
}