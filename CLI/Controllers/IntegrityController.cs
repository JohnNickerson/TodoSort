using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using Spectre.Console;

namespace AssimilationSoftware.TodoSort.CLI.Controllers;

public class IntegrityController
{
    private readonly IItemDisplay _view;

    public IntegrityController(IItemDisplay view)
    {
        _view = view;
    }

    public void CheckChain(CheckChainOptions chainOptions, ViewModel vm, TodoRepository repo)
    {
        vm.ShowHeadOnly = false;
        var problemsFound = 0;

        vm.SearchSpecification = chainOptions.GetSearchSpecification(repo);
        // 1. Make sure there is at least one item.
        if (!vm.SearchResults.Any())
        {
            _view.WriteLine("No items found in the chain.");
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
                    _view.WriteLine($"Item missing at index {i}");
                    problemsFound++;
                }
                else if (count > 1)
                {
                    _view.WriteLine($"Too many items with index {i}");
                    problemsFound++;
                }
            }

            if (min == max)
            {
                // TODO: Do not raise this if we are down to the last item in the chain.
                _view.WriteLine("Only one item in chain.");
                problemsFound++;
            }
            // 3. Find and add items that might belong in the chain.
            foreach (var excluded in vm.SearchResults.Where(i => !i.Tags.ContainsKey("order") || (!i.Tags.ContainsKey("series") && !i.ProjectId.HasValue)))
            {
                _view.WriteLine("Item potentially excluded:");
                _view.PrintItem(excluded, null, repo, chainOptions.NSFW, chainOptions.Verbose);
                problemsFound++;
            }
        }
        if (chainOptions.PauseOnProblems && problemsFound > 0)
        {
            _view.WriteLine("Press a key to continue...");
            Console.ReadKey();
        }

        repo.SaveChanges();
    }

    public void Commit(CommitOptions opts, ViewModel vm, TodoRepository repo)
    {
        var conflicts = repo.FindConflicts();
        foreach (var conflictSet in conflicts)
        {
            // Present conflicts for resolution.
            var i = 0;
            _view.WriteLine("Updates:");
            foreach (var edit in conflictSet.Updates)
            {
                _view.PrintItem(edit, i, repo, opts.NSFW, opts.Verbose);
                i++;
            }
            // Present options: Delete, Update (one particular version), Revert.
            _view.WriteLine("Options:");
            _view.WriteLine("R: Revert all pending changes to this record");
            _view.WriteLine("D: Delete this item, including any pending changes");
            _view.WriteLine("[number]: Accept a specific revision above");
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

            _view.WriteLine(Environment.NewLine);
        }
        var commitCount = repo.CommitChanges();
        _view.WriteLine($"{commitCount} pending changes committed.");
        repo.SaveChanges();
    }

    public void Dedupe(DedupeOptions dupe, ViewModel vm, TodoRepository repo)
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
            _view.WriteLine();
            _view.WriteLine("Select one item to merge all others into (or Cancel to stop):");
            var survivor = _view.Disambiguate(vm.SearchResults, repo, includeCancel: true);
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
                _view.WriteLine();
                _view.WriteLine("Select one item to merge all others into (i = ignore):");
                var survivor = _view.Disambiguate(vm.SearchResults, repo);
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

    public void FixTitles(FixTitlesOptions fixOptions, TodoRepository repo)
    {
        var client = new HttpClient(); // new RedirectWebClient();

        var totalCount = 0;
        var searchResults = repo.Search(fixOptions.GetSearchSpecification(repo), false);
        decimal progressTotal = searchResults.Count();
        AnsiConsole.Progress().Start(ctx =>
        {
            var task = ctx.AddTask("Fetching titles", maxValue: decimal.ToDouble(progressTotal));

            foreach (var tem in searchResults.ToArray())
            {
                task.Increment(1);
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
                    if (!string.IsNullOrWhiteSpace(title) && title != tem.Title)
                    {
                        totalCount++;
                        tem.Title = title;
                        if (!string.IsNullOrEmpty(fixOptions.MoveTo))
                        {
                            tem.Context = fixOptions.MoveTo;
                        }

                        repo.Update(tem);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        });

        if (totalCount > 0)
        {
            _view.WriteLine("{0} items updated.", totalCount);
        }

        repo.SaveChanges();
    }
}