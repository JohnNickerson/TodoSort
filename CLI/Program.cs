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
                    .WithParsed<BalanceOptions>(opts => Balance(opts, vm, repo, consoleView))
                    .WithParsed<BumpOptions>(opts => Bump(opts, vm, repo, consoleView))
                    .WithParsed<CheckChainOptions>(opts => CheckChain(opts, vm, repo, consoleView))
                    .WithParsed<CommitOptions>(opts => Commit(opts, vm, repo, consoleView))
                    .WithParsed<DedupeOptions>(opts => Dedupe(opts, vm, repo, consoleView))
                    .WithParsed<DeferAllOptions>(opts => DeferAll(opts, vm, repo, consoleView))
                    .WithParsed<DeferSubOptions>(opts => new WorkflowController().Defer(opts, vm, repo, consoleView))
                    .WithParsed<DeleteDoneOptions>(opts => DeleteDone(opts, vm, repo))
                    .WithParsed<DeleteOptions>(opts => new ItemController().Delete(opts, vm, repo, consoleView))
                    .WithParsed<DoneSearchSubOptions>(opts => new SearchController().SearchDone(opts, vm, repo, consoleView))
                    .WithParsed<DoneSubOptions>(opts => new WorkflowController().Done(opts, vm, repo, consoleView))
                    .WithParsed<ExportSubOptions>(opts => Export(opts, vm, repo, consoleView))
                    .WithParsed<FixTitlesOptions>(opts => FixTitles(opts, vm, repo, consoleView))
                    .WithParsed<ImportSubOptions>(opts => Import(opts, vm, repo, consoleView))
                    .WithParsed<InitSubOptions>(opts => Init(opts, settingsPath))
                    .WithParsed<MergeSubOptions>(opts => Merge(opts, vm, repo, consoleView))
                    .WithParsed<MoveAllSubOptions>(opts => MoveAll(opts, vm, repo, consoleView))
                    .WithParsed<MoveSubOptions>(opts => new ItemController().Move(opts, vm, repo, consoleView))
                    .WithParsed<NoteSubOptions>(opts => new ItemController().Note(opts, vm, repo, consoleView))
                    .WithParsed<OpenTagSubOptions>(opts => OpenTag(opts, vm, repo, consoleView))
                    .WithParsed<ProcessOptions>(opts => Process(opts, vm, repo, consoleView))
                    .WithParsed<RankOptions>(opts => Rank(opts, vm, repo, consoleView))
                    .WithParsed<RenameSubOptions>(opts => new ItemController().Rename(opts, vm, repo, consoleView))
                    .WithParsed<SearchOptions>(opts => new SearchController().Search(opts, vm, repo, consoleView))
                    .WithParsed<SetParentSubOptions>(opts => SetParent(opts, vm, repo, consoleView))
                    .WithParsed<SetProjectSubOptions>(opts => SetProject(opts, vm, repo, consoleView))
                    .WithParsed<SomedaySearchSubOptions>(opts => new SearchController().SearchSomeday(opts, vm, repo, consoleView))
                    .WithParsed<SomedaySubOptions>(opts => Someday(opts, vm, repo, consoleView))
                    .WithParsed<TagAllSubOptions>(opts => new ItemController().TagAll(opts, vm, repo, consoleView))
                    .WithParsed<TagOptions>(opts => new ItemController().Tag(opts, vm, repo, consoleView))
                    .WithParsed<UndeferOptions>(opts => Undefer(opts, vm, repo, consoleView))
                    .WithParsed<UndoSubOptions>(opts => new WorkflowController().Undo(opts, vm, repo, consoleView))
                    .WithParsed<SummaryOptions>(opts => new SearchController().Summary(opts, vm, repo, consoleView))
                    .WithParsed<UnrankAllOptions>(opts => UnrankAll(opts, vm, repo, consoleView))
                    .WithParsed<UnRankOptions>(opts => Unrank(opts, vm, repo, consoleView))
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

        private static void Dedupe(DedupeOptions dupe, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(dupe, vm);

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

            TidyUp(vm, repo, view);
        }

        private static void DeferAll(DeferAllOptions pruneOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(pruneOptions, vm);

            vm.SearchSpecification = pruneOptions.GetSearchSpecification(repo);
            view.WriteLine("About to defer {0} items. Continue [Y/N]?", vm.SearchResults.Count());
            var k = Console.ReadKey();
            if (k.KeyChar.ToString().ToLower() == "y")
            {
                vm.Defer(vm.SearchResults.ToArray());
            }

            TidyUp(vm, repo, view);
        }

        private static void DeleteDone(object opts, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(opts, vm);

            // TODO: Paste Process section
            throw new NotImplementedException("The delete-done command was never implemented.");

            //TidyUp(vm, repo);
        }

        private static void Export(ExportSubOptions exportOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(exportOptions, vm);

            // TODO: Work with Mustache# to externalise the formatting.
            // Write to the console if the filename is empty.
            if (string.IsNullOrEmpty(exportOptions.Filename))
            {
                view.WriteLine("Target filename is empty. You must specify a file name to write to.");
                return;
            }
            IExporter? exporter = null;
            if (!string.IsNullOrEmpty(exportOptions.TemplateFilename))
            {
                exporter = new TemplateExporter(exportOptions.Filename, exportOptions.TemplateFilename);
            }
            else
            {
                switch (exportOptions.Format)
                {
                    case ExportFormat.html:
                        exporter = new HtmlExporter { Filename = exportOptions.Filename };
                        break;
                    case ExportFormat.graphviz:
                        exporter = new GraphVizExporter { Filename = exportOptions.Filename };
                        break;
                    case ExportFormat.todosort:
                        exporter = new TextExporter { Filename = exportOptions.Filename };
                        break;
                    case ExportFormat.json:
                        exporter = new JsonExporter { Filename = exportOptions.Filename };
                        break;
                    case ExportFormat.todotxt:
                        exporter = new TodoTxtExporter {Filename = exportOptions.Filename };
                        break;
                    default:
                        view.WriteLine("Unknown output file format.");
                        break;
                }
            }
            if (exporter != null)
            {
                vm.SearchSpecification = exportOptions.GetSearchSpecification(repo);
                if (!string.IsNullOrEmpty(exportOptions.SortTag))
                {
                    exporter.Export(view.ApplySort(exportOptions.SortTag, vm.SearchResults).ToList());
                }
                else if (!string.IsNullOrEmpty(exportOptions.SortDescTag))
                {
                    exporter.Export(view.ApplySort(exportOptions.SortDescTag, vm.SearchResults, SortOrder.Descending).ToList());
                }
                else
                {
                    exporter.Export(vm.SearchResults.ToList());
                }
            }

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

        public static void Import(ImportSubOptions importOptions, ViewModel vm, ITodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(importOptions, vm);

            IImporter? importer = null;
            switch (importOptions.Format)
            {
                case ImportFormat.todosort:
                    if (Directory.Exists(importOptions.Filename))
                    {
                        importer = new TextFolderImporter { Folder = importOptions.Filename };
                    }
                    else
                    {
                        importer = new TextImporter { Filename = importOptions.Filename };
                    }
                    break;
                case ImportFormat.instapaper:
                    importer = new InstapaperImporter(importOptions.Filename);
                    break;
                case ImportFormat.urls:
                    importer = new RawUrlsImporter(importOptions.Filename);
                    break;
                default:
                    view.WriteLine($"Unknown import format: {importOptions.Format}");
                    break;
            }
            if (importer != null)
            {
                // Get everything from the source file.
                // Exclude anything already seen, according to its import hash field.
                vm.AddAllItems(importOptions.Context, true, importer.GetAllItems());
                view.WriteLine(vm.StatusMessage);
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

        private static void Rank(MultiSearchSubOptions rankOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(rankOptions, vm);

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

            TidyUp(vm, repo, view);
        }

        private static void SetParent(SetParentSubOptions setParentOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(setParentOptions, vm);

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

            TidyUp(vm, repo, view);
        }

        private static void SetProject(SetProjectSubOptions commandOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(commandOptions, vm);

            view.WriteLine("Confirm child item:");
            vm.SearchTerm = commandOptions.ChildSearchTerm;
            var child = view.Disambiguate(vm.SearchResults, repo, view);
            view.WriteLine("Confirm project:");
            vm.SearchTerm = commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm;
            var project = view.Disambiguate(vm.SearchResults, repo, view);
            if (child != null && project != null)
            {
                vm.SetProject(child, project);
            }

            TidyUp(vm, repo, view);
        }

        private static void Someday(SomedaySubOptions someSub, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(someSub, vm);

            // Display the whole Someday file, [someSub.PageSize] items at a time, and either delete or do one per listing.
            ActionItem? undefer = null;
            if (someSub.PageSize <= 0) someSub.PageSize = 1;
            if (someSub.PageSize > 10) someSub.PageSize = 10;
            var someItems = from s in vm.SomedayItems where !s.TickleDate.HasValue || someSub.IncludeTickle select s;
            for (var offset = 0; offset <= someItems.Count(); offset += someSub.PageSize)
            {
                Console.Clear();
                for (var index = 0; index < someSub.PageSize && offset + index < someItems.Count(); index++)
                {
                    view.PrintItem(someItems.ElementAt(offset + index), index, repo, someSub.NSFW, someSub.Verbose);
                }
                var choice = Console.ReadKey().KeyChar;
                view.WriteLine();
                int dex;
                if (Int32.TryParse(choice.ToString(), out dex))
                {
                    undefer = someItems.ElementAt(offset + dex);
                }
                if (undefer != null)
                {
                    EditSomedayItem(vm, undefer, repo, view);
                    undefer = null;
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void Undefer(UndeferOptions undeferOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(undeferOptions, vm);

            vm.SomedaySearchSpecification = undeferOptions.SearchSpecification;
            selected = view.Disambiguate(vm.SomedaySearchResults, repo, view, !string.IsNullOrEmpty(undeferOptions.ItemId));
            if (selected != null)
            {
                vm.Undefer("inbox", selected);
            }

            TidyUp(vm, repo, view);
        }

        private static void UnrankAll(UnrankAllOptions unrankAllOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(unrankAllOptions, vm);

            vm.SearchSpecification = unrankAllOptions.GetSearchSpecification(repo);
            view.WriteLine("About to delete all ranking data for {0} items. Continue [Y/N]?", vm.SearchResults.Count());
            if (Console.ReadKey().KeyChar.ToString().ToLower() == "y")
            {
                vm.ResetPriorityParents(vm.SearchResults.ToArray());
            }

            TidyUp(vm, repo, view);
        }

        private static void Unrank(UnRankOptions unrankOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(unrankOptions, vm);

            vm.SearchSpecification = unrankOptions.SearchSpecification;
            selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(unrankOptions.ItemId));
            if (selected != null)
            {
                vm.ResetPriorityParents(selected);
            }

            TidyUp(vm, repo, view);
        }

        private static void CheckChain(CheckChainOptions chainOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(chainOptions, vm);
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

        private static int Process(ProcessOptions argSubs, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(argSubs, vm);

            // Assign revision IDs where none are found.
            repo.FindAll();
            foreach (var item in repo.Items)
            {
                if (item.RevisionGuid == Guid.Empty)
                {
                    item.RevisionGuid = Guid.NewGuid();
                }
            }
            repo.SaveChanges();

            // Go over the @someday items and look for tickle dates.
            vm.SomedaySearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
            vm.Undefer("inbox", vm.SomedaySearchResults.ToArray());

            vm.SearchSpecification = new ContextSearchSpecification("inbox");
            var inbox = vm.SearchResults.ToList();
            for (var i = 0; i < inbox.Count; i++)
            {
                // Assign the @inbox items to contexts.
                view.WriteLine("To which context should this item go?");
                var first = inbox[i];
                view.PrintItem(first, null, repo);
                view.WriteLine();
                PrintContexts(vm, view);
                var newContext = Console.ReadLine();
                vm.SetContext(first, newContext);
                view.WriteLine();
            }

            // Need to find projects for which there is no next action.
            vm.SearchSpecification = new ContextSearchSpecification("projects");
            var projects = vm.SearchResults.ToList();
            for (var i = 0; i < projects.Count; i++)
            {
                vm.ShowHeadOnly = false;
                vm.SearchSpecification = new ProjectChildrenSearchSpecification(projects[i]);
                if (!vm.SearchResults.Any())
                {
                    // Add next actions for projects.
                    view.WriteLine("What is the next action required on this project?");
                    var first = projects[i];
                    view.PrintItem(first, null, repo);
                    var nextAction = Console.ReadLine();
                    view.WriteLine("...and to what context does it belong?");
                    PrintContexts(vm, view);
                    var newContext = Console.ReadLine();
                    if (nextAction == newContext)
                    {
                        // Wrote something like "someday"/"someday". Assume it is a new context.
                        vm.SetContext(first, newContext);
                    }
                    else
                    {
                        var next = new ActionItem
                        {
                            Context = newContext,
                            Title = nextAction,
                            ProjectId = first.ID
                        };
                        vm.AddItem(next);
                    }
                    view.WriteLine();
                }
            }

            TidyUp(vm, repo, view, forceSave: argSubs.Force);
            return 0;
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

        private static void Balance(BalanceOptions balanceOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(balanceOpts, vm);
            if (balanceOpts.Verbose)
            {
                // Show progress percentage if in verbose mode.
                vm.PropertyChanged += VmOnPropertyChanged;
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

            TidyUp(vm, repo, view);
        }

        private static void Bump(BumpOptions bumpOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(bumpOpts, vm);
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
            TidyUp(vm, repo, view);
        }

        private static int HandleErrors(object errs)
        {
            return 1;
        }

        // TODO: Remove this and do better.
        private static void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.ProgressPercent))
            {
                Console.WriteLine($"{((ViewModel?)sender)?.ProgressPercent}%");
            }
        }

        private static void PrintContexts(ViewModel vm, IItemDisplay view)
        {
            foreach (var c in vm.GetContextNames())
            {
                view.WriteLine(c);
            }
        }

        private static void OpenItemTag(string tagValue)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = tagValue;
            p.Start();
        }

        private static void EditSomedayItem(ViewModel vm, ActionItem item, ITodoRepository repo, IItemDisplay view, bool nsfw = false, bool verbose = false)
        {
            while (true)
            {
                view.PrintItem(item, null, repo, nsfw, verbose);
                // Write menu.
                view.WriteLine("1. Undefer (and go to next item)");
                view.WriteLine("2. Rename");
                view.WriteLine("3. Open Tag");
                view.WriteLine("4. Assign tickler date");
                view.WriteLine("5. Assign tags");
                view.WriteLine("6. Finished (next item)");
                var k = Console.ReadKey();
                view.WriteLine();
                switch (k.KeyChar)
                {
                    case '1':
                        if (item.Tags.ContainsKey("previous-context") && item.Context != item.Tags["previous-context"])
                        {
                            vm.Undefer(item.Tags["previous-context"], item);
                        }
                        else
                        {
                            view.WriteLine("To which context should this item go?");
                            // List contexts.
                            PrintContexts(vm, view);
                            var newContext = Console.ReadLine();
                            vm.Undefer(newContext, item);
                        }
                        return;
                    case '2':
                        view.WriteLine("What new name should this item have?");
                        var name = Console.ReadLine();
                        vm.Rename(item, name);
                        break;
                    case '3':
                        view.WriteLine("What tag to open?");
                        var tag = Console.ReadLine();
                        if (item.Tags.TryGetValue(tag ?? string.Empty, out string? tagValue))
                        {
                            OpenItemTag(tagValue);
                        }
                        else
                        {
                            view.WriteLine("Tag not found on this item.");
                        }
                        break;
                    case '5':
                        TagItem(vm, item, view);
                        break;
                    case '4':
                        view.WriteLine("When should this item reappear in the inbox?");
                        var dateInput = Console.ReadLine();
                        DateTime parsedDate;
                        if (DateTime.TryParse(dateInput, out parsedDate))
                        {
                            vm.Defer(item, parsedDate);
                        }
                        break;
                    case '6':
                        return;
                }
            }
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
