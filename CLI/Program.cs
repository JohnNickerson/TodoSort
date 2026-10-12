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
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;
using Humanizer;
using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Views;
using AssimilationSoftware.TodoSort.CLI.Interfaces;

namespace AssimilationSoftware.TodoSort.CLI
{
    public class Program
    {
        private static ActionItem? selected = null;
        private static string settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ".todosort");
        private static bool forceSave = false;

        static void Main(string[] args)
        {
#if DEBUG
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.AutoFlush = true;
#endif
            var argVerb = string.Empty;
            var f = FolderSettings.LoadFrom(settingsPath);
            var todoMapper = new ActionItemDiskMapper(f.TodoPath);
            var repo = new TodoRepository(todoMapper, Path.GetDirectoryName(f.TodoPath), Environment.MachineName);
            var consoleView = new ConsoleItemDisplay();

            var vm = new ViewModel(repo);


            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();

            var parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; with.HelpWriter = Console.Error; });
            parser.ParseArguments(args, types)
                    .WithParsed<AddSubOptions>(opts => AddItem(opts, vm, repo, consoleView))
                    .WithParsed<AdvancedSearchOptions>(opts => AdvancedSearch(opts, vm, repo, consoleView))
                    .WithParsed<BalanceOptions>(opts => Balance(opts, vm, repo, consoleView))
                    .WithParsed<BumpOptions>(opts => Bump(opts, vm, repo, consoleView))
                    .WithParsed<CheckChainOptions>(opts => CheckChain(opts, vm, repo, consoleView))
                    .WithParsed<CommitOptions>(opts => Commit(opts, vm, repo, consoleView))
                    .WithParsed<DedupeOptions>(opts => Dedupe(opts, vm, repo, consoleView))
                    .WithParsed<DeferAllOptions>(opts => DeferAll(opts, vm, repo, consoleView))
                    .WithParsed<DeferSubOptions>(opts => Defer(opts, vm, repo, consoleView))
                    .WithParsed<DeleteDoneOptions>(opts => DeleteDone(opts, vm, repo))
                    .WithParsed<DeleteOptions>(opts => Delete(opts, vm, repo, consoleView))
                    .WithParsed<DoneSearchSubOptions>(opts => SearchDone(opts, vm, repo, consoleView))
                    .WithParsed<DoneSubOptions>(opts => Done(opts, vm, repo, consoleView))
                    .WithParsed<ExportSubOptions>(opts => Export(opts, vm, repo, consoleView))
                    .WithParsed<FixTitlesOptions>(opts => FixTitles(opts, vm, repo, consoleView))
                    .WithParsed<ImportSubOptions>(opts => Import(opts, vm, repo, consoleView))
                    .WithParsed<InitSubOptions>(opts => Init(opts))
                    .WithParsed<MergeSubOptions>(opts => Merge(opts, vm, repo, consoleView))
                    .WithParsed<MoveAllSubOptions>(opts => MoveAll(opts, vm, repo, consoleView))
                    .WithParsed<MoveSubOptions>(opts => Move(opts, vm, repo, consoleView))
                    .WithParsed<NoteSubOptions>(opts => Note(opts, vm, repo, consoleView))
                    .WithParsed<OpenTagSubOptions>(opts => OpenTag(opts, vm, repo, consoleView))
                    .WithParsed<ProcessOptions>(opts => Process(opts, vm, repo, consoleView))
                    .WithParsed<RankOptions>(opts => Rank(opts, vm, repo, consoleView))
                    .WithParsed<RenameSubOptions>(opts => Rename(opts, vm, repo, consoleView))
                    .WithParsed<SearchOptions>(opts => Search(opts, vm, repo, consoleView))
                    .WithParsed<SetParentSubOptions>(opts => SetParent(opts, vm, repo, consoleView))
                    .WithParsed<SetProjectSubOptions>(opts => SetProject(opts, vm, repo, consoleView))
                    .WithParsed<SomedaySearchSubOptions>(opts => SearchSomeday(opts, vm, repo, consoleView))
                    .WithParsed<SomedaySubOptions>(opts => Someday(opts, vm, repo, consoleView))
                    .WithParsed<TagAllSubOptions>(opts => TagAll(opts, vm, repo, consoleView))
                    .WithParsed<TagOptions>(opts => Tag(opts, vm, repo, consoleView))
                    .WithParsed<UndeferOptions>(opts => Undefer(opts, vm, repo, consoleView))
                    .WithParsed<UndoSubOptions>(opts => Undo(opts, vm, repo, consoleView))
                    .WithParsed<SummaryOptions>(opts => Summary(opts, vm, repo, consoleView))
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
                Console.WriteLine("Updates:");
                foreach (var edit in conflictSet.Updates)
                {
                    view.PrintItem(edit, i, repo, opts.NSFW, opts.Verbose);
                    i++;
                }
                // Present options: Delete, Update (one particular version), Revert.
                Console.WriteLine("Options:");
                Console.WriteLine("R: Revert all pending changes to this record");
                Console.WriteLine("D: Delete this item, including any pending changes");
                Console.WriteLine("[number]: Accept a specific revision above");
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

                Console.WriteLine(Environment.NewLine);
            }
            var commitCount = repo.CommitChanges();
            Console.WriteLine($"{commitCount} pending changes committed.");
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
                Console.WriteLine();
                Console.WriteLine("Select one item to merge all others into (or Cancel to stop):");
                var survivor = Disambiguate(vm.SearchResults, repo, view, includeCancel: true);
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
                    Console.WriteLine();
                    Console.WriteLine("Select one item to merge all others into (i = ignore):");
                    var survivor = Disambiguate(vm.SearchResults, repo, view);
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
            Console.WriteLine("About to defer {0} items. Continue [Y/N]?", vm.SearchResults.Count());
            var k = Console.ReadKey();
            if (k.KeyChar.ToString().ToLower() == "y")
            {
                vm.Defer(vm.SearchResults.ToArray());
            }

            TidyUp(vm, repo, view);
        }

        private static void Defer(DeferSubOptions deferOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(deferOpts, vm);

            vm.SearchSpecification = deferOpts.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(deferOpts.ItemId));
            if (selected != null)
            {
                if (deferOpts.TickleDate.HasValue)
                {
                    vm.Defer(selected, deferOpts.TickleDate.Value);
                }
                else
                {
                    vm.Defer(selected);
                }
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

        private static void Delete(DeleteOptions delOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(delOpts, vm);

            // Find a matching item to delete.
            vm.SearchSpecification = delOpts.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(delOpts.ItemId));
            if (selected != null) vm.Delete(selected);

            TidyUp(vm, repo, view);
        }

        private static void SearchDone(DoneSearchSubOptions search, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(search, vm);

            // Construct a search specification.
            // Set the ViewModel property.
            vm.DoneSearchSpecification = search.GetSearchSpecification(repo);
            // Report the results.
            view.PrintItems(search.SortTag ?? "done-date", vm.DoneSearchResults, repo, search.NSFW, search.Verbose);
            if (!search.NoCount)
            {
                Console.WriteLine("{0} item(s) found.", vm.DoneSearchResults.Count());
            }

            TidyUp(vm, repo, view);
        }

        private static void Done(DoneSubOptions doneOpts, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(doneOpts, vm);

            // If there is a next action, create a new item and add it to the correct context.
            vm.SearchSpecification = doneOpts.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(doneOpts.ItemId));
            if (selected != null)
            {
                vm.MarkDone(doneOpts.DoneDate, selected);
            }

            TidyUp(vm, repo, view);
        }

        private static void Export(ExportSubOptions exportOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(exportOptions, vm);

            // TODO: Work with Mustache# to externalise the formatting.
            // Write to the console if the filename is empty.
            if (string.IsNullOrEmpty(exportOptions.Filename))
            {
                Console.WriteLine("Target filename is empty. You must specify a file name to write to.");
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
                        Console.WriteLine("Unknown output file format.");
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
                        Console.WriteLine("Renaming:\n\t{0}\n\t{1}", tem.Title, title);
                        totalCount++;
                        tem.Title = title;
                        if (!string.IsNullOrEmpty(fixOptions.MoveTo))
                        {
                            tem.Context = fixOptions.MoveTo;
                        }

                        vm.Update(tem);
                        Console.WriteLine("{0:0}% complete", 100m * progress / progressTotal);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }

            if (totalCount > 0)
            {
                Console.WriteLine("{0} items updated.", totalCount);
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
                    Console.WriteLine($"Unknown import format: {importOptions.Format}");
                    break;
            }
            if (importer != null)
            {
                // Get everything from the source file.
                // Exclude anything already seen, according to its import hash field.
                vm.AddAllItems(importOptions.Context, true, importer.GetAllItems());
                Console.WriteLine(vm.StatusMessage);
            }

            TidyUp(vm, repo, view);
        }

        private static void Init(InitSubOptions initOpts)
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

            Console.WriteLine("Confirm child item:");
            vm.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
            var mergeVictim = Disambiguate(vm.SearchResults, repo, view);
            if (mergeVictim != null)
            {
                Console.WriteLine("Confirm item to merge into:");
                vm.SearchTerm = mergeOptions.TargetSearchTerm;
                var combined = Disambiguate(vm.SearchResults.Where(x => x.ID != mergeVictim.ID), repo, view);
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
            Console.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));

            TidyUp(vm, repo, view);
        }

        private static void Move(MoveSubOptions moveOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(moveOptions, vm);

            vm.SearchSpecification = moveOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(moveOptions.ItemId));
            if (selected != null)
            {
                vm.SetContext(selected, moveOptions.NewContext);
                if (moveOptions.Unrank)
                {
                    vm.ResetPriorityParents(selected);
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void Note(NoteSubOptions noteOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(noteOptions, vm);

            // Add a note to a task.
            vm.SearchSpecification = noteOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(noteOptions.ItemId));
            if (selected != null)
            {
                vm.AddNote(selected, noteOptions.NewNote);
            }

            TidyUp(vm, repo, view, true);
        }

        private static void OpenTag(OpenTagSubOptions openTagOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(openTagOptions, vm);

            // Read a tag and pass it through to the "start" argVerb. Intended for URLs and file names.
            vm.SearchSpecification = openTagOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(openTagOptions.ItemId));
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
                        Console.WriteLine("What new title should this item have?");
                        var newTitle = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(newTitle))
                        {
                            vm.Rename(selected, newTitle);
                        }
                    }
                    if (openTagOptions.ReTag)
                    {
                        TagItem(vm, selected);
                    }
                    if (openTagOptions.MarkAsDone)
                    {
                        vm.MarkDone(null, selected);
                    }
                }
                else
                {
                    Console.WriteLine("Tag not found: {0}", openTagOptions.Tag);
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
                    Console.WriteLine(string.Format("{1}{1}@{0}", con, Environment.NewLine));
                }
                for (var x = 0; x < items.Count() - 1; x += 2)
                {
                    if (quitAndSave) break;
                    // get vote
                    Console.WriteLine("{0}/{1} ({2}%) complete", x, items.Count(), 100 * x / items.Count());
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
                            Console.WriteLine();
                            Console.WriteLine("Saving changes.");
                            break;
                        case "c":
                            Console.WriteLine();
                            Console.WriteLine("Cancelling. No changes saved.");
                            return;
                    }
                    Console.WriteLine();
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void Rename(RenameSubOptions renameOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(renameOptions, vm);

            vm.SearchSpecification = renameOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(renameOptions.ItemId));
            if (selected != null)
            {
                vm.Rename(selected, renameOptions.NewTitle);
                Console.WriteLine("Item renamed.");
                if (renameOptions.ReTag)
                {
                    TagItem(vm, selected);
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void Search(SearchOptions searchOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(searchOptions, vm);

            vm.SearchSpecification = searchOptions.GetSearchSpecification(repo);
            if (vm.SearchResults.Count() == 0)
            {
                Console.WriteLine("No results found.");
            }
            else if (searchOptions.PrintTree)
            {
                view.PrintTree(vm.SearchResults.ToList(), new List<ActionItem>(), repo, searchOptions.NSFW, searchOptions.Verbose);
            }
            else
            {
                view.PrintItems(searchOptions.SortTag ?? string.Empty, vm.SearchResults, repo, searchOptions.NSFW, searchOptions.Verbose);
                if (!searchOptions.NoCount)
                {
                    Console.WriteLine("{0} item(s) found.", vm.SearchResults.Count());
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void SetParent(SetParentSubOptions setParentOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(setParentOptions, vm);

            Console.WriteLine("Confirm child item:");
            vm.SearchSpecification = setParentOptions.GetChildSearchSpecification(repo);
            var child = Disambiguate(vm.SearchResults, repo, view);
            Console.WriteLine("Confirm parent item:");
            vm.SearchSpecification = setParentOptions.GetParentSearchSpecification(repo);
            if (child != null)
            {
                var parent = Disambiguate(vm.SearchResults.Where(x => x.ID != child.ID), repo, view);
                if (parent != null)
                {
                    vm.SetParent(child, parent);
                    Console.WriteLine();
                    view.PrintTree(new List<ActionItem> { { child }, { parent } }, new List<ActionItem>(), repo, setParentOptions.NSFW, setParentOptions.Verbose);
                }
                else
                {
                    // Null parent. Confirm to set to nothing.
                    Console.WriteLine("No parent selected. Remove current priority parent? [Y/N]");
                    var k = Console.ReadKey();
                    if (k.KeyChar.ToString().ToLower() == "y")
                    {
                        vm.SetParent(child, null);
                        Console.WriteLine();
                        view.PrintTree(new List<ActionItem> { child }, new List<ActionItem>(), repo, setParentOptions.NSFW, setParentOptions.Verbose);
                    }
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void SetProject(SetProjectSubOptions commandOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(commandOptions, vm);

            Console.WriteLine("Confirm child item:");
            vm.SearchTerm = commandOptions.ChildSearchTerm;
            var child = Disambiguate(vm.SearchResults, repo, view);
            Console.WriteLine("Confirm project:");
            vm.SearchTerm = commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm;
            var project = Disambiguate(vm.SearchResults, repo, view);
            if (child != null && project != null)
            {
                vm.SetProject(child, project);
            }

            TidyUp(vm, repo, view);
        }

        private static void SearchSomeday(SomedaySearchSubOptions search, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(search, vm);

            // Construct a search specification.
            // Set the ViewModel property.
            vm.SomedaySearchSpecification = search.GetSearchSpecification(repo);
            // Report the results.
            view.PrintItems(search.SortTag ?? "tickle-date", vm.SomedaySearchResults, repo, search.NSFW, search.Verbose);
            if (!search.NoCount)
            {
                Console.WriteLine("{0} item(s) found.", vm.SomedaySearchResults.Count());
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
                Console.WriteLine();
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



        private static void TagAll(TagAllSubOptions tagAllOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(tagAllOptions, vm);

            vm.SearchSpecification = tagAllOptions.SearchSpecification;
            var items = vm.SearchResults.ToArray();
            foreach (var i in items)
            {
                if (string.IsNullOrWhiteSpace(tagAllOptions.TagValue))
                {
                    vm.RemoveTag(i, tagAllOptions.TagName);
                }
                else
                {
                    vm.SetTag(i, tagAllOptions.TagName, tagAllOptions.TagValue);
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void Tag(SingleSearchSubOptions tagOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(tagOptions, vm);

            // Search for a matching item.
            vm.SearchSpecification = tagOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(tagOptions.ItemId));
            if (selected != null)
            {
                TagItem(vm, selected);
            }

            TidyUp(vm, repo, view, true);
        }

        private static void Undefer(UndeferOptions undeferOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(undeferOptions, vm);

            vm.SomedaySearchSpecification = undeferOptions.SearchSpecification;
            selected = Disambiguate(vm.SomedaySearchResults, repo, view, !string.IsNullOrEmpty(undeferOptions.ItemId));
            if (selected != null)
            {
                vm.Undefer("inbox", selected);
            }

            TidyUp(vm, repo, view);
        }

        private static void Undo(UndoSubOptions undoOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(undoOptions, vm);

            vm.DoneSearchSpecification = undoOptions.SearchSpecification;
            selected = Disambiguate(vm.DoneSearchResults, repo, view, !string.IsNullOrEmpty(undoOptions.ItemId));
            if (selected != null)
            {
                vm.Undo(undoOptions.NewContext, selected);
            }

            TidyUp(vm, repo, view);
        }

        private static void Summary(UniversalOptions summaryArgs, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(summaryArgs, vm);

            if (summaryArgs.Verbose && !summaryArgs.ShowAllItems)
            {
                vm.ShowHeadOnly = false;
            }
            vm.SearchSpecification = new TrueSpecification<ActionItem>();
            var summaryData = from i in vm.SearchResults group i by i.Context into c select new { Context = c.Key, Count = c.Count() };

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
                    vm.SearchSpecification = new ContextSearchSpecification(c.Context);
                    var depths = vm.GetDepthsView();

                    var detailed = new Dictionary<int, int>();
                    var unknownCount = 0;
                    foreach (var i in vm.SearchResults)
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

            TidyUp(vm, repo, view);
        }

        private static void UnrankAll(UnrankAllOptions unrankAllOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(unrankAllOptions, vm);

            vm.SearchSpecification = unrankAllOptions.GetSearchSpecification(repo);
            Console.WriteLine("About to delete all ranking data for {0} items. Continue [Y/N]?", vm.SearchResults.Count());
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
            selected = Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(unrankOptions.ItemId));
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
                Console.WriteLine("No items found in the chain.");
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
                        Console.WriteLine($"Item missing at index {i}");
                        problemsFound++;
                    }
                    else if (count > 1)
                    {
                        Console.WriteLine($"Too many items with index {i}");
                        problemsFound++;
                    }
                }

                if (min == max)
                {
                    Console.WriteLine("Only one item in chain.");
                    problemsFound++;
                }
                // 3. Find and add items that might belong in the chain.
                foreach (var excluded in vm.SearchResults.Where(i => !i.Tags.ContainsKey("order") || (!i.Tags.ContainsKey("series") && !i.ProjectId.HasValue)))
                {
                    Console.WriteLine("Item potentially excluded:");
                    view.PrintItem(excluded, null, repo, chainOptions.NSFW, chainOptions.Verbose);
                    problemsFound++;
                }
            }
            if (chainOptions.PauseOnProblems && problemsFound > 0)
            {
                Console.WriteLine("Press a key to continue...");
                Console.ReadKey();
            }

            TidyUp(vm, repo, view);
        }

        private static void SetUniversalOptions(object argSubs, ViewModel vm)
        {
            // Set universal options.
            if (argSubs is UniversalOptions options)
            {
                if (!(argSubs is MultiSearchSubOptions))
                {
                    vm.ShowHeadOnly = !options.ShowAllItems;
                }
                else
                {
                    vm.ShowHeadOnly = false;
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
            forceSave = argSubs.Force;
            vm.SomedaySearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
            vm.Undefer("inbox", vm.SomedaySearchResults.ToArray());

            vm.SearchSpecification = new ContextSearchSpecification("inbox");
            var inbox = vm.SearchResults.ToList();
            for (var i = 0; i < inbox.Count; i++)
            {
                // Assign the @inbox items to contexts.
                Console.WriteLine("To which context should this item go?");
                var first = inbox[i];
                view.PrintItem(first, null, repo);
                Console.WriteLine();
                PrintContexts(vm);
                var newContext = Console.ReadLine();
                vm.SetContext(first, newContext);
                Console.WriteLine();
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
                    Console.WriteLine("What is the next action required on this project?");
                    var first = projects[i];
                    view.PrintItem(first, null, repo);
                    var nextAction = Console.ReadLine();
                    Console.WriteLine("...and to what context does it belong?");
                    PrintContexts(vm);
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
                    Console.WriteLine();
                }
            }

            TidyUp(vm, repo, view);
            return 0;
        }

        private static void TidyUp(ViewModel vm, ITodoRepository repo, IItemDisplay view, bool verbose = false)
        {
            #region Tidy up
            // Delete any items with a context of "delete".
            vm.SearchSpecification = new ContextSearchSpecification("delete");
            vm.Delete(vm.SearchResults.ToArray());
            #endregion


            if (selected != null)
            {
                view.PrintItem(selected, null, repo, false, verbose);
            }

            // Rewrite the files
            vm.Save(forceSave);
        }

        private static void AddItem(AddSubOptions a, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(a, vm);
            selected = new ActionItem { Context = a.Context, Title = a.ActionTitle };
            if (!string.IsNullOrWhiteSpace(a.Note))
            {
                selected.Notes.Add(a.Note);
            }
            Console.WriteLine("Adding action '{0}' to context @{1}", a.ActionTitle, a.Context);
            vm.AddItem(selected);
            // Allow tagging right away.
            TagItem(vm, selected);
            TidyUp(vm, repo, view, true);
        }

        private static void AdvancedSearch(AdvancedSearchOptions argSubs, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(argSubs, vm);

            vm.SearchSpecification = argSubs.SearchSpecification;
            view.PrintItems("title", vm.SearchResults, repo, argSubs.NSFW, argSubs.Verbose);

            TidyUp(vm, repo, view, argSubs.Verbose);
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
                    Console.WriteLine($"{commitCount} pending changes committed.");
                    repo.SaveChanges();
                }
            }
            else
            {
                Console.WriteLine("Branching factor must be greater than zero.");
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
                var chosen = Disambiguate(vm.SearchResults, repo, view, true);
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

        private static void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.ProgressPercent))
            {
                Console.WriteLine($"{((ViewModel?)sender)?.ProgressPercent}%");
            }
        }

        private static void PrintContexts(ViewModel vm)
        {
            foreach (var c in vm.GetContextNames())
            {
                Console.WriteLine(c);
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
                Console.WriteLine("1. Undefer (and go to next item)");
                Console.WriteLine("2. Rename");
                Console.WriteLine("3. Open Tag");
                Console.WriteLine("4. Assign tickler date");
                Console.WriteLine("5. Assign tags");
                Console.WriteLine("6. Finished (next item)");
                var k = Console.ReadKey();
                Console.WriteLine();
                switch (k.KeyChar)
                {
                    case '1':
                        if (item.Tags.ContainsKey("previous-context") && item.Context != item.Tags["previous-context"])
                        {
                            vm.Undefer(item.Tags["previous-context"], item);
                        }
                        else
                        {
                            Console.WriteLine("To which context should this item go?");
                            // List contexts.
                            PrintContexts(vm);
                            var newContext = Console.ReadLine();
                            vm.Undefer(newContext, item);
                        }
                        return;
                    case '2':
                        Console.WriteLine("What new name should this item have?");
                        var name = Console.ReadLine();
                        vm.Rename(item, name);
                        break;
                    case '3':
                        Console.WriteLine("What tag to open?");
                        var tag = Console.ReadLine();
                        if (item.Tags.TryGetValue(tag ?? string.Empty, out string? tagValue))
                        {
                            OpenItemTag(tagValue);
                        }
                        else
                        {
                            Console.WriteLine("Tag not found on this item.");
                        }
                        break;
                    case '5':
                        TagItem(vm, item);
                        break;
                    case '4':
                        Console.WriteLine("When should this item reappear in the inbox?");
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

        private static void TagItem(ViewModel vm, ActionItem selected)
        {
            string? tagName;
            do
            {
                Console.WriteLine("What should this new tag be called? (ENTER to quit)");
                tagName = Console.ReadLine()?.ToLower()?.Trim();
                if (!string.IsNullOrEmpty(tagName))
                {
                    Console.WriteLine("What is the value of the tag?");
                    var value = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        vm.SetTag(selected, tagName, value);
                    }
                    else if (selected.Tags.ContainsKey(tagName))
                    {
                        vm.RemoveTag(selected, tagName);
                    }
                    Console.WriteLine();
                }
            } while (!string.IsNullOrEmpty(tagName));
        }

        private static string FormatTitle(ActionItem i, bool nsfw = false)
        {
            var maskTitle = (i.Tags?.ContainsKey("nsfw") ?? false && !nsfw) ? $"(nsfw) {Rot13.Transform(i.Title)}" : i.Title;
            maskTitle = maskTitle.EscapeMarkup();
            if (i.Tags?.TryGetValue("type", out var itemType) ?? false)
            {
                var typeIcon = i.Tags["type"].ToUpper() switch
                {
                    // These don't seem to display on Windows by default, so commented out. :(
                    // TODO: Use ascii art or something?
                    // "MOVIE" => ":movie_camera:", // [ ]<
                    // "TV" => ":television:", // [_]
                    // "BOOK" => ":open_book:", // \/
                    // "GAME" => ":video_game:", // <+>
                    _ => $"[[{i.Tags["type"].ToUpper()}]]"
                };
                return string.Format("{0} {1}", typeIcon, maskTitle);
            }
            else
            {
                return maskTitle;
            }
        }

        private static ActionItem? Disambiguate(IEnumerable<ActionItem> todoList, ITodoRepository repo, IItemDisplay view, bool autoAcceptOne = false, bool nsfw = false, bool verbose = false, bool includeCancel = false)
        {
            ActionItem? selected = null;

            // Disambiguate or verify search results.
            if (todoList.Count() == 0)
            {
                Console.WriteLine("No search matches. No action will be taken.");
            }
            else if (todoList.Count() == 1 && autoAcceptOne)
            {
                view.PrintItem(todoList.ElementAt(0), null, repo, nsfw, verbose);
                Console.WriteLine("Auto-accepting...");
                selected = todoList.ElementAt(0);
            }
            else
            {
                var prompt = new SelectionPrompt<ActionItem>()
                    .Title($"{"search result".ToQuantity(todoList.Count())}. Choose one:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more items)[/]")
                    .AddChoices(todoList)
                    .UseConverter(a => FormatTitle(a));
                if (includeCancel)
                {
                    prompt.AddChoice(new ActionItem
                    {
                        Title = "Cancel",
                        Context = "cancel"
                    });
                }
                selected = AnsiConsole.Prompt(prompt);
                if (selected.Context == "cancel")
                {
                    Console.WriteLine("Cancelled.");
                    return null;
                }
            }
            return selected;
        }
    }
}
