using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
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

        repo.SaveChanges();
    }

    public void Commit(CommitOptions opts, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        var conflicts = repo.FindConflicts();
        foreach (var conflictSet in conflicts)
        {
            // Present conflicts for resolution.
            var i = 0;
            view.WriteLine("Updates:");
            foreach (var edit in conflictSet.Updates)
            {
                view.PrintItem(edit, i, repo, opts.NSFW, opts.Verbose);
                i++;
            }
            // Present options: Delete, Update (one particular version), Revert.
            view.WriteLine("Options:");
            view.WriteLine("R: Revert all pending changes to this record");
            view.WriteLine("D: Delete this item, including any pending changes");
            view.WriteLine("[number]: Accept a specific revision above");
            var optKey = Console.ReadKey();
            switch (optKey.KeyChar.ToString().ToLower())
            {
                case "r":
                    repo.Revert(conflictSet.Id);
                    break;
                case "d":
                    repo.ResolveByDelete(conflictSet.Id);
                    break;
                default:
                    if (char.IsDigit(optKey.KeyChar))
                    {
                        var index = int.Parse(optKey.KeyChar.ToString());
                        if (index < conflictSet.Updates.Count)
                        {
                            repo.ResolveConflict(conflictSet.Updates[index]);
                        }
                    }
                    break;
            }

            view.WriteLine(Environment.NewLine);
        }
        var commitCount = repo.CommitChanges();
        view.WriteLine($"{commitCount} pending changes committed.");
        repo.SaveChanges();
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
        repo.SaveChanges();
    }

    public void FixTitles(FixTitlesOptions fixOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
    {
        vm.ShowHeadOnly = false;

        vm.SearchSpecification = fixOptions.GetSearchSpecification(repo);
        var client = new HttpClient(); // new RedirectWebClient();

        var totalCount = 0;
        decimal progress = 0;
        decimal progressTotal = vm.SearchResults.Count();
        foreach (var tem in vm.SearchResults.ToArray())
        {
            progress++;
            try
            {
                var source = client.GetStringAsync(tem.Tags["url"]).Result;
                //// Get the redirected URL, if any.
                //Debug.WriteLine(client.ResponseUri.OriginalString);
                //if (client.ResponseUri?.OriginalString != tem.Tags["url"])
                //{
                //    source = client.DownloadString(client.ResponseUri);
                //}

                var title = Regex.Match(source, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
                title = WebUtility.HtmlDecode(title);
                if (!String.IsNullOrWhiteSpace(title) && title != tem.Title)
                {
                    view.WriteLine("Renaming:\n\t{0}\n\t{1}", tem.Title, title);
                    totalCount++;
                    tem.Title = title;
                    if (!string.IsNullOrEmpty(fixOptions.MoveTo))
                    {
                        tem.Context = fixOptions.MoveTo;
                    }

                    vm.Update(tem);
                    view.WriteLine("{0:0}% complete", 100m * progress / progressTotal);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        if (totalCount > 0)
        {
            view.WriteLine("{0} items updated.", totalCount);
        }

        repo.SaveChanges();
    }
}