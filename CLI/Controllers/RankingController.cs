using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;
using Spectre.Console;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class RankingController
{
    private readonly IItemDisplay _view;
    private readonly TodoService _svc;

    public RankingController(TodoService svc, IItemDisplay view)
    {
        _view = view;
        _svc = svc;
    }

    public void Balance(BalanceOptions balanceOpts)
    {
        _svc.ShowHeadOnly = false;
        if (balanceOpts.Verbose)
        {
            // Show progress percentage if in verbose mode.
            // TODO: Maybe Spectre has better ways of showing progress.
            //vm.PropertyChanged += VmOnPropertyChanged;
        }
        // Validate the branching factor: must be greater than zero.
        if (balanceOpts.BranchFactor > 0)
        {
            _svc.SearchSpecification = balanceOpts.GetSearchSpecification();
            var depths = _svc.GetDepthsView();
            var vine = _svc.SearchResults.OrderBy(i => depths[i.ID]).ThenByDescending(i => i.Upvotes).ToArray();
            _svc.Balance(vine, balanceOpts.BranchFactor);
            if (balanceOpts.Commit)
            {
                // In large lists, rather than writing out thousands of changes, then reading them again to commit, just commit everything now.
                var commitCount = _svc.CommitChanges();
                _view.WriteLine($"{commitCount} pending changes committed.");
                _svc.Save();
            }
        }
        else
        {
            _view.WriteLine("Branching factor must be greater than zero.");
        }

        _svc.Save();
    }

    public void Bump(BumpOptions bumpOpts)
    {
        _svc.ShowHeadOnly = !bumpOpts.ShowAllItems;
        _svc.SearchSpecification = bumpOpts.SearchSpecification;
        var bumpItems = new List<ActionItem>();
        if (bumpOpts.Top == 0)
        {
            // Manually pick one item.
            var chosen = _view.Disambiguate(_svc.SearchResults, _svc, true);
            if (chosen != null) bumpItems.Add(chosen);
        }
        else if (!string.IsNullOrEmpty(bumpOpts.SortTag))
        {
            // First items.
            bumpItems.AddRange(_view.ApplySort(bumpOpts.SortTag, _svc.SearchResults).Take(bumpOpts.Top));
        }
        else if (!string.IsNullOrEmpty(bumpOpts.SortDescTag))
        {
            bumpItems.AddRange(_view.ApplySort(bumpOpts.SortTag ?? string.Empty, _svc.SearchResults, SortOrder.Descending).Take(bumpOpts.Top));
        }
        else
        {
            bumpItems.AddRange(_svc.SearchResults.Take(bumpOpts.Top));
        }
        // TODO: Display the before and after positions together.
        // Before: red and strike-through.
        // After: yellow and bold.
        _view.PrintTree(new List<ActionItem>(), bumpItems, _svc, bumpOpts.NSFW, bumpOpts.Verbose);
        foreach (var target in bumpItems)
        {
            var depth = _svc.GetDepthsView().TryGetValue(target.ID, out var targetDepth) ? targetDepth / 2 : 0;
            if (depth == 0)
            {
                target.Upvotes++;
                _svc.Update(target);
            }
            while (_svc.GetDepthsView().TryGetValue(target.ID, out var currentDepth) && currentDepth > depth && target.ParentId != null)
            {
                var currentParent = _svc.GetParent(target);
                if (currentParent == null)
                {
                    break;
                }
                _svc.SetParent(target, _svc.GetParent(currentParent));
            }
        }
        // After
        _view.PrintTree(bumpItems, new List<ActionItem>(), _svc, bumpOpts.NSFW, bumpOpts.Verbose);
        _svc.Save();
    }

    public void Rank(MultiSearchSubOptions rankOptions)
    {
        // for each context..
        var quitAndSave = false;
        _svc.ShowHeadOnly = true;
        var contextList = _svc.GetContextNames("inbox", "done").ToArray();
        for (int contextIndex = 0; contextIndex < contextList.Length; contextIndex++)
        {
            var con = contextList[contextIndex];
            if (quitAndSave) break;
            // select all items without rank parents
            _svc.SearchSpecification = new ContextSearchSpecification(con).And(rankOptions.GetSearchSpecification());
            var items = _svc.SearchResults.ToArray();
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
                _view.WriteLine(string.Format("{1}{1}@{0}", con, Environment.NewLine));
            }
            for (var x = 0; x < items.Count() - 1; x += 2)
            {
                if (quitAndSave) break;
                // get vote
                _view.WriteLine("{0}/{1} ({2}%) complete", x, items.Count(), 100 * x / items.Count());
                _view.PrintItem(items.ElementAt(index[x]), 1, _svc, rankOptions.NSFW, rankOptions.Verbose);
                _view.PrintItem(items.ElementAt(index[x + 1]), 2, _svc, rankOptions.NSFW, rankOptions.Verbose);
                var choice = AnsiConsole.Prompt(new Spectre.Console.TextPrompt<string>("Which of these is more important? (q=quit and save, c=cancel)")
                    .AddChoices(["1", "2", "q", "c"]));
                // assign parents based on vote
                switch (choice)
                {
                    case "1":
                        _svc.SetParent(items.ElementAt(index[x + 1]), items.ElementAt(index[x]));
                        break;
                    case "2":
                        _svc.SetParent(items.ElementAt(index[x]), items.ElementAt(index[x + 1]));
                        break;
                    case "q":
                        quitAndSave = true;
                        _view.WriteLine();
                        _view.WriteLine("Saving changes.");
                        break;
                    case "c":
                        _view.WriteLine();
                        _view.WriteLine("Cancelling. No changes saved.");
                        return;
                }
                _view.WriteLine();
            }
        }

        _svc.Save();
    }

    public void SetParent(SetParentSubOptions setParentOptions)
    {
        _svc.ShowHeadOnly = !setParentOptions.ShowAllItems;

        _view.WriteLine("Confirm child item:");
        _svc.SearchSpecification = setParentOptions.GetChildSearchSpecification();
        var child = _view.Disambiguate(_svc.SearchResults, _svc);
        _view.WriteLine("Confirm parent item:");
        _svc.SearchSpecification = setParentOptions.GetParentSearchSpecification();
        if (child != null)
        {
            var parent = _view.Disambiguate(_svc.SearchResults.Where(x => x.ID != child.ID), _svc);
            if (parent != null)
            {
                _svc.SetParent(child, parent);
                _view.WriteLine();
                _view.PrintTree(new List<ActionItem> { { child }, { parent } }, new List<ActionItem>(), _svc, setParentOptions.NSFW, setParentOptions.Verbose);
            }
            else
            {
                // Null parent. Confirm to set to nothing.
                _view.WriteLine("No parent selected. Remove current priority parent? [Y/N]");
                var k = Console.ReadKey();
                if (k.KeyChar.ToString().ToLower() == "y")
                {
                    _svc.SetParent(child, null);
                    _view.WriteLine();
                    _view.PrintTree(new List<ActionItem> { child }, new List<ActionItem>(), _svc, setParentOptions.NSFW, setParentOptions.Verbose);
                }
            }
        }

        _svc.Save();
    }

    public void Unrank(UnRankOptions unrankOptions)
    {
        _svc.ShowHeadOnly = !unrankOptions.ShowAllItems;

        _svc.SearchSpecification = unrankOptions.SearchSpecification;
        var selected = _view.Disambiguate(_svc.SearchResults, _svc, !string.IsNullOrEmpty(unrankOptions.ItemId));
        if (selected != null)
        {
            _svc.ResetPriorityParents(selected);
            _view.PrintItem(selected, null, _svc, false, unrankOptions.Verbose);
            _svc.Save();
        }
    }

    public void UnrankAll(UnrankAllOptions unrankAllOptions)
    {
        _svc.ShowHeadOnly = false;

        _svc.SearchSpecification = unrankAllOptions.GetSearchSpecification();
        _view.WriteLine("About to delete all ranking data for {0} items. Continue [Y/N]?", _svc.SearchResults.Count());
        if (Console.ReadKey().KeyChar.ToString().ToLower() == "y")
        {
            _svc.ResetPriorityParents(_svc.SearchResults.ToArray());
        }

        _svc.Save();
    }
}