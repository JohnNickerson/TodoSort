using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Export;
using AssimilationSoftware.TodoSort.Core.Import;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Spectre.Console;
using Humanizer;
using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Views;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Controllers;

namespace AssimilationSoftware.TodoSort.CLI
{
    public class Program
    {
        private static ActionItem? selected = null;

        static void Main(string[] args)
        {
#if DEBUG
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.AutoFlush = true;
#endif
            var argVerb = string.Empty;
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ".todosort");
            var f = FolderSettings.LoadFrom(settingsPath);
            var todoMapper = new ActionItemDiskMapper(f.TodoPath);
            var repo = new TodoRepository(todoMapper, Path.GetDirectoryName(f.TodoPath), Environment.MachineName);
            var consoleView = new ConsoleItemDisplay();

            var vm = new ViewModel(repo);


            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();

            var parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; with.HelpWriter = Console.Error; });
            parser.ParseArguments(args, types)
                    .WithParsed<AddSubOptions>(opts => new ItemController().AddItem(opts, vm, repo, consoleView))
                    .WithParsed<AdvancedSearchOptions>(opts => new SearchController().AdvancedSearch(opts, vm, repo, consoleView))
                    .WithParsed<BalanceOptions>(opts => new RankingController().Balance(opts, vm, repo, consoleView))
                    .WithParsed<BumpOptions>(opts => new RankingController().Bump(opts, vm, repo, consoleView))
                    .WithParsed<CheckChainOptions>(opts => new IntegrityController().CheckChain(opts, vm, repo, consoleView))
                    .WithParsed<CommitOptions>(opts => Commit(opts, vm, repo, consoleView))
                    .WithParsed<DedupeOptions>(opts => new IntegrityController().Dedupe(opts, vm, repo, consoleView))
                    .WithParsed<DeferAllOptions>(opts => new WorkflowController().DeferAll(opts, vm, repo, consoleView))
                    .WithParsed<DeferSubOptions>(opts => new WorkflowController().Defer(opts, vm, repo, consoleView))
                    .WithParsed<DeleteOptions>(opts => new ItemController().Delete(opts, vm, repo, consoleView))
                    .WithParsed<DoneSearchSubOptions>(opts => new SearchController().SearchDone(opts, vm, repo, consoleView))
                    .WithParsed<DoneSubOptions>(opts => new WorkflowController().Done(opts, vm, repo, consoleView))
                    .WithParsed<ExportSubOptions>(opts => new ImportExportController().Export(opts, vm, repo, consoleView))
                    .WithParsed<FixTitlesOptions>(opts => FixTitles(opts, vm, repo, consoleView))
                    .WithParsed<ImportSubOptions>(opts => new ImportExportController().Import(opts, vm, repo, consoleView))
                    .WithParsed<InitSubOptions>(opts => Init(opts, settingsPath))
                    .WithParsed<MergeSubOptions>(opts => Merge(opts, vm, repo, consoleView))
                    .WithParsed<MoveAllSubOptions>(opts => MoveAll(opts, vm, repo, consoleView))
                    .WithParsed<MoveSubOptions>(opts => new ItemController().Move(opts, vm, repo, consoleView))
                    .WithParsed<NoteSubOptions>(opts => new ItemController().Note(opts, vm, repo, consoleView))
                    .WithParsed<OpenTagSubOptions>(opts => OpenTag(opts, vm, repo, consoleView))
                    .WithParsed<RankOptions>(opts => new RankingController().Rank(opts, vm, repo, consoleView))
                    .WithParsed<RenameSubOptions>(opts => new ItemController().Rename(opts, vm, repo, consoleView))
                    .WithParsed<SearchOptions>(opts => new SearchController().Search(opts, vm, repo, consoleView))
                    .WithParsed<SetParentSubOptions>(opts => new RankingController().SetParent(opts, vm, repo, consoleView))
                    .WithParsed<SetProjectSubOptions>(opts => new ItemController().SetProject(opts, vm, repo, consoleView))
                    .WithParsed<SomedaySearchSubOptions>(opts => new SearchController().SearchSomeday(opts, vm, repo, consoleView))
                    .WithParsed<TagAllSubOptions>(opts => new ItemController().TagAll(opts, vm, repo, consoleView))
                    .WithParsed<TagOptions>(opts => new ItemController().Tag(opts, vm, repo, consoleView))
                    .WithParsed<UndeferOptions>(opts => new WorkflowController().Undefer(opts, vm, repo, consoleView))
                    .WithParsed<UndoSubOptions>(opts => new WorkflowController().Undo(opts, vm, repo, consoleView))
                    .WithParsed<SummaryOptions>(opts => new SearchController().Summary(opts, vm, repo, consoleView))
                    .WithParsed<UnrankAllOptions>(opts => new RankingController().UnrankAll(opts, vm, repo, consoleView))
                    .WithParsed<UnRankOptions>(opts => new RankingController().Unrank(opts, vm, repo, consoleView))
                    .WithNotParsed(errs => HandleErrors(errs));
        }

        private static void Commit(CommitOptions opts, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(opts, vm);

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

            TidyUp(vm, repo, view);
        }

        private static void FixTitles(FixTitlesOptions fixOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(fixOptions, vm);

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

            TidyUp(vm, repo, view);
        }

        private static void Init(InitSubOptions initOpts, string settingsPath)
        {
            var initSettings = new FolderSettings
            {
                TodoPath = initOpts.TodoFile
            };

            // Save settings.
            FolderSettings.SaveTo(settingsPath, initSettings);
            string rawDirectory = Path.GetDirectoryName(initOpts.TodoFile) ?? ".";
            var dirName = Path.GetFullPath(string.IsNullOrEmpty(rawDirectory) ? "." : rawDirectory);
            if (dirName is not null && !Directory.Exists(dirName) && AnsiConsole.Confirm($"{initOpts.TodoFile} does not exist. Create it?", false))
            {
                Directory.CreateDirectory(dirName);
            }
        }

        private static void Merge(MergeSubOptions mergeOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(mergeOptions, vm);

            view.WriteLine("Confirm child item:");
            vm.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
            var mergeVictim = view.Disambiguate(vm.SearchResults, repo, view);
            if (mergeVictim != null)
            {
                view.WriteLine("Confirm item to merge into:");
                vm.SearchTerm = mergeOptions.TargetSearchTerm;
                var combined = view.Disambiguate(vm.SearchResults.Where(x => x.ID != mergeVictim.ID), repo, view);
                if (mergeVictim != null && combined != null)
                {
                    vm.Merge(mergeVictim, combined);
                    selected = combined;
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void MoveAll(MoveAllSubOptions moveAllOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(moveAllOptions, vm);

            vm.ShowHeadOnly = false; // Make sure we move all items in the context, not just the head.
            vm.SearchSpecification = moveAllOptions.GetSearchSpecification(repo);
            var items = vm.SearchResults.ToList();
            var counter = 0;
            while (items.Count > 0)
            {
                vm.SetContext(items[0], moveAllOptions.NewContext);
                items.RemoveAt(0);
                counter++;
            }
            view.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));

            TidyUp(vm, repo, view);
        }

        private static void OpenTag(OpenTagSubOptions openTagOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(openTagOptions, vm);

            // Read a tag and pass it through to the "start" argVerb. Intended for URLs and file names.
            vm.SearchSpecification = openTagOptions.SearchSpecification;
            selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(openTagOptions.ItemId));
            if (selected != null)
            {
                if (selected.Tags.TryGetValue(openTagOptions.Tag ?? string.Empty, out string? tagValue))
                {
                    if (openTagOptions.Copy)
                    {
                        //System.Windows.Forms.Clipboard.SetText(tagValue);
                        // ^^ This doesn't work.
                    }
                    else
                    {
                        OpenItemTag(tagValue);
                    }
                    if (openTagOptions.Rename)
                    {
                        view.WriteLine("What new title should this item have?");
                        var newTitle = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(newTitle))
                        {
                            vm.Rename(selected, newTitle);
                        }
                    }
                    if (openTagOptions.ReTag)
                    {
                        TagItem(vm, selected, view);
                    }
                    if (openTagOptions.MarkAsDone)
                    {
                        vm.MarkDone(null, selected);
                    }
                }
                else
                {
                    view.WriteLine("Tag not found: {0}", openTagOptions.Tag);
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void SetUniversalOptions(object argSubs, ViewModel vm)
        {
            // Set universal options.
            if (argSubs is UniversalOptions options)
            {
                if (argSubs is MultiSearchSubOptions)
                {
                    vm.ShowHeadOnly = false;
                }
                else
                {
                    vm.ShowHeadOnly = !options.ShowAllItems;
                }
            }
        }

        private static void TidyUp(ViewModel vm, ITodoRepository repo, IItemDisplay view, bool verbose = false, bool forceSave = false)
        {
            if (selected != null)
            {
                view.PrintItem(selected, null, repo, false, verbose);
            }

            // Rewrite the files
            vm.Save(forceSave);
        }

        private static int HandleErrors(object errs)
        {
            return 1;
        }

        private static void OpenItemTag(string tagValue)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = tagValue;
            p.Start();
        }

        private static void TagItem(ViewModel vm, ActionItem selected, IItemDisplay view)
        {
            string? tagName;
            do
            {
                view.WriteLine("What should this new tag be called? (ENTER to quit)");
                tagName = Console.ReadLine()?.ToLower()?.Trim();
                if (!string.IsNullOrEmpty(tagName))
                {
                    view.WriteLine("What is the value of the tag?");
                    var value = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        vm.SetTag(selected, tagName, value);
                    }
                    else if (selected.Tags.ContainsKey(tagName))
                    {
                        vm.RemoveTag(selected, tagName);
                    }
                    view.WriteLine();
                }
            } while (!string.IsNullOrEmpty(tagName));
        }
    }
}
