using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using Spectre.Console;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class RankingController
{
    public void Balance(BalanceOptions balanceOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;
        if (balanceOpts.Verbose)
        {
            // Show progress percentage if in verbose mode.
            // TODO: Maybe Spectre has better ways of showing progress.
            //vm.PropertyChanged += VmOnPropertyChanged;
        }
        // Validate the branching factor: must be greater than zero.
        if (balanceOpts.BranchFactor > 0)
        {
            vm.SearchSpecification = balanceOpts.GetSearchSpecification(repo);
            var depths = vm.GetDepthsView();
            var vine = vm.SearchResults.OrderBy(i => depths[i.ID]).ThenByDescending(i => i.Upvotes).ToArray();
            vm.Balance(vine, balanceOpts.BranchFactor);
            if (balanceOpts.Commit)
            {
                // In large lists, rather than writing out thousands of changes, then reading them again to commit, just commit everything now.
                var commitCount = repo.CommitChanges();
                view.WriteLine($"{commitCount} pending changes committed.");
                repo.SaveChanges();
            }
        }
        else
        {
            view.WriteLine("Branching factor must be greater than zero.");
        }

        repo.SaveChanges();
    }

    public void Bump(BumpOptions bumpOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !bumpOpts.ShowAllItems;
        vm.SearchSpecification = bumpOpts.SearchSpecification;
        var bumpItems = new List<ActionItem>();
        if (bumpOpts.Top == 0)
        {
            // Manually pick one item.
            var chosen = view.Disambiguate(vm.SearchResults, repo, view, true);
            if (chosen != null) bumpItems.Add(chosen);
        }
        else if (!string.IsNullOrEmpty(bumpOpts.SortTag))
        {
            // First items.
            bumpItems.AddRange(view.ApplySort(bumpOpts.SortTag, vm.SearchResults).Take(bumpOpts.Top));
        }
        else if (!string.IsNullOrEmpty(bumpOpts.SortDescTag))
        {
            bumpItems.AddRange(view.ApplySort(bumpOpts.SortTag ?? string.Empty, vm.SearchResults, SortOrder.Descending).Take(bumpOpts.Top));
        }
        else
        {
            bumpItems.AddRange(vm.SearchResults.Take(bumpOpts.Top));
        }
        // TODO: Display the before and after positions together.
        // Before: red and strike-through.
        // After: yellow and bold.
        view.PrintTree(new List<ActionItem>(), bumpItems, repo, bumpOpts.NSFW, bumpOpts.Verbose);
        foreach (var target in bumpItems)
        {
            var depth = target.GetRankDepth(repo) / 2;
            if (depth == 0)
            {
                target.Upvotes++;
                vm.Update(target);
            }
            while (target.GetRankDepth(repo) > depth && target.ParentId != null)
            {
                vm.SetParent(target, target.GetParent(repo)?.GetParent(repo));
            }
        }
        // After
        view.PrintTree(bumpItems, new List<ActionItem>(), repo, bumpOpts.NSFW, bumpOpts.Verbose);
        repo.SaveChanges();
    }

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

        repo.SaveChanges();
    }

    public void SetParent(SetParentSubOptions setParentOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !setParentOptions.ShowAllItems;

        view.WriteLine("Confirm child item:");
        vm.SearchSpecification = setParentOptions.GetChildSearchSpecification(repo);
        var child = view.Disambiguate(vm.SearchResults, repo, view);
        view.WriteLine("Confirm parent item:");
        vm.SearchSpecification = setParentOptions.GetParentSearchSpecification(repo);
        if (child != null)
        {
            var parent = view.Disambiguate(vm.SearchResults.Where(x => x.ID != child.ID), repo, view);
            if (parent != null)
            {
                vm.SetParent(child, parent);
                view.WriteLine();
                view.PrintTree(new List<ActionItem> { { child }, { parent } }, new List<ActionItem>(), repo, setParentOptions.NSFW, setParentOptions.Verbose);
            }
            else
            {
                // Null parent. Confirm to set to nothing.
                view.WriteLine("No parent selected. Remove current priority parent? [Y/N]");
                var k = Console.ReadKey();
                if (k.KeyChar.ToString().ToLower() == "y")
                {
                    vm.SetParent(child, null);
                    view.WriteLine();
                    view.PrintTree(new List<ActionItem> { child }, new List<ActionItem>(), repo, setParentOptions.NSFW, setParentOptions.Verbose);
                }
            }
        }

        repo.SaveChanges();
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
            repo.SaveChanges();
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

        repo.SaveChanges();
    }
}