using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;

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
}