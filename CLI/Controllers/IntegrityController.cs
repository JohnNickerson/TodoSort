using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class IntegrityController
{
    public void CheckChain(CheckChainOptions chainOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;
        var problemsFound = 0;

        vm.SearchSpecification = chainOptions.GetSearchSpecification(repo);
        // 1. Make sure there is at least one item.
        if (!vm.SearchResults.Any())
        {
            view.WriteLine("No items found in the chain.");
            problemsFound++;
        }
        else
        {
            // 2. Make sure there are no gaps.
            var min = vm.SearchResults.Min(i => i.GetIntTag("order", Int32.MaxValue));
            var max = vm.SearchResults.Max(i => i.GetIntTag("order", Int32.MinValue));
            // Special case: If searching in a project, and the project specifies a size, use that for maximum. Must specify full project ID.
            if (Guid.TryParse(chainOptions.ProjectID, out var projId))
            {
                // Look for a "length" tag on the project.
                var project = repo.Find(projId);
                if (project.Tags.TryGetValue("length", out var maxString))
                {
                    max = int.Parse(maxString);
                }
            }
            for (var i = min; i <= max; i++)
            {
                var count = vm.SearchResults.Count(j => j.GetIntTag("order", 0) == i);
                if (count == 0)
                {
                    view.WriteLine($"Item missing at index {i}");
                    problemsFound++;
                }
                else if (count > 1)
                {
                    view.WriteLine($"Too many items with index {i}");
                    problemsFound++;
                }
            }

            if (min == max)
            {
                // TODO: Do not raise this if we are down to the last item in the chain.
                view.WriteLine("Only one item in chain.");
                problemsFound++;
            }
            // 3. Find and add items that might belong in the chain.
            foreach (var excluded in vm.SearchResults.Where(i => !i.Tags.ContainsKey("order") || (!i.Tags.ContainsKey("series") && !i.ProjectId.HasValue)))
            {
                view.WriteLine("Item potentially excluded:");
                view.PrintItem(excluded, null, repo, chainOptions.NSFW, chainOptions.Verbose);
                problemsFound++;
            }
        }
        if (chainOptions.PauseOnProblems && problemsFound > 0)
        {
            view.WriteLine("Press a key to continue...");
            Console.ReadKey();
        }

        vm.Save();
    }

    public void Dedupe(DedupeOptions dupe, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = !dupe.ShowAllItems;

        vm.SearchSpecification = new TrueSpecification<ActionItem>();
        foreach (var duplicate in vm.GetDuplicateTitles())
        {
            // Search by title
            vm.SearchSpecification = new ExactPropertyValueSpecification<ActionItem, string>(i => i.Title, duplicate);
            // Quick hack: If we have two items with the same title but different type tags, skip them.
            if (vm.SearchResults.Count() == 2 &&
                vm.SearchResults.All(i => i.Tags.ContainsKey("type")) &&
                vm.SearchResults.ElementAt(0).Tags["type"].ToLower() !=
                vm.SearchResults.ElementAt(1).Tags["type"].ToLower())
            {
                continue;
            }
            // Present options for merging
            // Get user input
            view.WriteLine();
            view.WriteLine("Select one item to merge all others into (or Cancel to stop):");
            var survivor = view.Disambiguate(vm.SearchResults, repo, view, includeCancel: true);
            if (survivor != null)
            {
                // Merge all into survivor.
                foreach (var c in vm.SearchResults.Except(new[] { survivor }).ToList())
                {
                    vm.Merge(c, survivor);
                }
            }
        }
        if (!string.IsNullOrEmpty(dupe.Tag))
        {
            foreach (var dupeTag in vm.GetDuplicateTags(dupe.Tag))
            {
                // Search by tag
                vm.SearchSpecification = new TagValueSpecification(dupe.Tag, dupeTag);
                // Present options for merging
                // Get user input.
                view.WriteLine();
                view.WriteLine("Select one item to merge all others into (i = ignore):");
                var survivor = view.Disambiguate(vm.SearchResults, repo, view);
                if (survivor != null)
                {
                    // Merge into survivor.
                    foreach (var c in vm.SearchResults.Except(new[] { survivor }).ToList())
                    {
                        vm.Merge(c, survivor);
                    }
                }
            }
        }
        vm.Save();
    }
}