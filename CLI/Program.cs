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
using System.Runtime.InteropServices;
using Spectre.Console;

namespace AssimilationSoftware.TodoSort.CLI
{
    enum SortOrder
    {
        Ascending,
        Descending
    }

    class Program
    {
        private static bool verbose = false;
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

            var vm = new ViewModel(repo);


            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();

            Parser.Default.ParseArguments(args, types)
                    .WithParsed<AddSubOptions>(opts => AddItem(opts, vm, repo))
                    .WithParsed<AdvancedSearchOptions>(opts => AdvancedSearch(opts, vm, repo))
                    .WithParsed<BalanceOptions>(opts => Balance(opts, vm, repo))
                    .WithParsed<BumpOptions>(opts => Bump(opts, vm, repo))
                    .WithParsed<CheckChainOptions>(opts => CheckChain(opts, vm, repo))
                    .WithParsed<CommitOptions>(opts => Commit(opts, vm, repo))
                    .WithParsed<DedupeOptions>(opts => Dedupe(opts, vm, repo))
                    .WithParsed<DeferAllOptions>(opts => DeferAll(opts, vm, repo))
                    .WithParsed<DeferSubOptions>(opts => Defer(opts, vm, repo))
                    .WithParsed<DeleteDoneOptions>(opts => DeleteDone(opts, vm, repo))
                    .WithParsed<DeleteOptions>(opts => Delete(opts, vm, repo))
                    .WithParsed<DoneSearchSubOptions>(opts => SearchDone(opts, vm, repo))
                    .WithParsed<DoneSubOptions>(opts => Done(opts, vm, repo))
                    .WithParsed<ExportSubOptions>(opts => Export(opts, vm, repo))
                    .WithParsed<FixTitlesOptions>(opts => FixTitles(opts, vm, repo))
                    .WithParsed<ImportSubOptions>(opts => Import(opts, vm, repo))
                    .WithParsed<InitSubOptions>(opts => Init(opts))
                    .WithParsed<MergeSubOptions>(opts => Merge(opts, vm, repo))
                    .WithParsed<MoveAllSubOptions>(opts => MoveAll(opts, vm, repo))
                    .WithParsed<MoveSubOptions>(opts => Move(opts, vm, repo))
                    .WithParsed<NoteSubOptions>(opts => Note(opts, vm, repo))
                    .WithParsed<OpenTagSubOptions>(opts => OpenTag(opts, vm, repo))
                    .WithParsed<ProcessOptions>(opts => Process(opts, vm, repo))
                    .WithParsed<RankOptions>(opts => Rank(opts, vm, repo))
                    .WithParsed<RenameSubOptions>(opts => Rename(opts, vm, repo))
                    .WithParsed<SearchOptions>(opts => Search(opts, vm, repo))
                    .WithParsed<SetParentSubOptions>(opts => SetParent(opts, vm, repo))
                    .WithParsed<SetProjectSubOptions>(opts => SetProject(opts, vm, repo))
                    .WithParsed<SomedaySearchSubOptions>(opts => SearchSomeday(opts, vm, repo))
                    .WithParsed<SomedaySubOptions>(opts => Someday(opts, vm, repo))
                    .WithParsed<TagAllSubOptions>(opts => TagAll(opts, vm, repo))
                    .WithParsed<TagOptions>(opts => Tag(opts, vm, repo))
                    .WithParsed<UndeferOptions>(opts => Undefer(opts, vm, repo))
                    .WithParsed<UndoSubOptions>(opts => Undo(opts, vm, repo))
                    .WithParsed<UpdateSubOptions>(opts => Update(opts, vm))
                    .WithParsed<SummaryOptions>(opts => Summary(opts, vm, repo))
                    .WithParsed<UnrankAllOptions>(opts => UnrankAll(opts, vm, repo))
                    .WithParsed<UnRankOptions>(opts => Unrank(opts, vm, repo))
                    .WithNotParsed(errs => HandleErrors(errs));
        }

        private static void Commit(CommitOptions opts, ViewModel vm, TodoRepository repo)
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
                    PrintItem(edit, i, repo);
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

            TidyUp(vm, repo);
        }

        private static void Dedupe(DedupeOptions ddup, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(ddup, vm);

            vm.SearchSpecification = new TrueSpecification<ActionItem>();
            foreach (var duptit in vm.GetDuplicateTitles())
            {
                // Search by title
                vm.SearchSpecification = new ExactPropertyValueSpecification<ActionItem, string>(i => i.Title, duptit);
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
                Console.WriteLine("Select one item to merge all others into (i = ignore):");
                var survivor = Disambiguate(vm.SearchResults, repo);
                if (survivor != null)
                {
                    // Merge all into survivor.
                    foreach (var c in vm.SearchResults.Except(new[] { survivor }).ToList())
                    {
                        vm.Merge(c, survivor);
                    }
                }
            }
            if (!string.IsNullOrEmpty(ddup.Tag))
            {
                foreach (var duptag in vm.GetDuplicateTags(ddup.Tag))
                {
                    // Search by tag
                    vm.SearchSpecification = new TagValueSpecification(ddup.Tag, duptag);
                    // Present options for merging
                    // Get user input.
                    Console.WriteLine();
                    Console.WriteLine("Select one item to merge all others into (i = ignore):");
                    var survivor = Disambiguate(vm.SearchResults, repo);
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

            TidyUp(vm, repo);
        }

        private static void DeferAll(DeferAllOptions pruneOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(pruneOptions, vm);

            vm.SearchSpecification = pruneOptions.GetSearchSpecification(repo);
            Console.WriteLine("About to defer {0} items. Continue [Y/N]?", vm.SearchResults.Count());
            var k = Console.ReadKey();
            if (k.KeyChar.ToString().ToLower() == "y")
            {
                vm.Defer(vm.SearchResults.ToArray());
            }

            TidyUp(vm, repo);
        }

        private static void Defer(DeferSubOptions deferopts, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(deferopts, vm);

            vm.SearchSpecification = deferopts.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(deferopts.ItemId));
            if (selected != null)
            {
                if (deferopts.TickleDate.HasValue)
                {
                    vm.Defer(selected, deferopts.TickleDate.Value);
                }
                else
                {
                    vm.Defer(selected);
                }
            }

            TidyUp(vm, repo);
        }

        private static void DeleteDone(object opts, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(opts, vm);

            // TODO: Paste Process section
            throw new NotImplementedException("The delete-done command was never implemented.");

            //TidyUp(vm, repo);
        }

        private static void Delete(DeleteOptions argsubs, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(argsubs, vm);

            // Find a matching item to delete.
            vm.SearchSpecification = argsubs.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(argsubs.ItemId));
            if (selected != null) vm.Delete(selected);

            TidyUp(vm, repo);
        }

        private static void SearchDone(DoneSearchSubOptions search, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(search, vm);

            // Construct a search specification.
            // Set the ViewModel property.
            vm.DoneSearchSpecification = search.GetSearchSpecification(repo);
            // Report the results.
            PrintItems(search.SortTag ?? "done-date", vm.DoneSearchResults, repo, search.NSFW);
            if (!search.NoCount)
            {
                Console.WriteLine("{0} item(s) found.", vm.DoneSearchResults.Count());
            }

            TidyUp(vm, repo);
        }

        private static void Done(DoneSubOptions doneopts, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(doneopts, vm);

            // If there is a next action, create a new item and add it to the correct context.
            vm.SearchSpecification = doneopts.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(doneopts.ItemId));
            if (selected != null)
            {
                vm.MarkDone(doneopts.DoneDate, selected);
            }

            TidyUp(vm, repo);
        }

        private static void Export(ExportSubOptions exportOptions, ViewModel vm, TodoRepository repo)
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
                switch (exportOptions.Format?.ToLower())
                {
                    case "html":
                        exporter = new HtmlExporter { Filename = exportOptions.Filename };
                        break;
                    case "graphviz":
                        exporter = new GraphVizExporter { Filename = exportOptions.Filename };
                        break;
                    case "text":
                        exporter = new TextExporter { Filename = exportOptions.Filename };
                        break;
                    case "json":
                        exporter = new JsonExporter { Filename = exportOptions.Filename };
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
                    exporter.Export(ApplySort(exportOptions.SortTag, vm.SearchResults).ToList());
                }
                else if (!string.IsNullOrEmpty(exportOptions.SortDescTag))
                {
                    exporter.Export(ApplySort(exportOptions.SortDescTag, vm.SearchResults, SortOrder.Descending).ToList());
                }
                else
                {
                    exporter.Export(vm.SearchResults.ToList());
                }
            }

            TidyUp(vm, repo);
        }

        private static void FixTitles(FixTitlesOptions fixOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(fixOptions, vm);

            vm.SearchSpecification = fixOptions.GetSearchSpecification(repo);
            var client = new WebClient(); // new RedirectWebClient();

            var totalCount = 0;
            decimal progress = 0;
            decimal progressTotal = vm.SearchResults.Count();
            foreach (var tem in vm.SearchResults.ToArray())
            {
                progress++;
                try
                {
                    var source = client.DownloadString(tem.Tags["url"]);
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

            TidyUp(vm, repo);
        }

        private static void Import(ImportSubOptions importOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(importOptions, vm);

            IImporter? importer = null;
            switch (importOptions.Format)
            {
                case "todosort":
                    if (Directory.Exists(importOptions.Filename))
                    {
                        importer = new TextFolderImporter { Folder = importOptions.Filename };
                    }
                    else
                    {
                        importer = new TextImporter { Filename = importOptions.Filename };
                    }
                    break;
                case "instapaper":
                    importer = new InstapaperImporter(importOptions.Filename);
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

            TidyUp(vm, repo);
        }

        private static void Init(InitSubOptions initty)
        {
            var initsettings = new FolderSettings
            {
                TodoPath = initty.TodoFile
            };

            // Save settings.
            FolderSettings.SaveTo(settingsPath, initsettings);
            if (!Directory.Exists(Path.GetDirectoryName(initty.TodoFile)) && AnsiConsole.Confirm($"{initty.TodoFile} does not exist. Create it?", false))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(initty.TodoFile));
            }
        }

        private static void Merge(MergeSubOptions mergeOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(mergeOptions, vm);

            Console.WriteLine("Confirm child item:");
            vm.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
            var mergeVictim = Disambiguate(vm.SearchResults, repo);
            if (mergeVictim != null)
            {
                Console.WriteLine("Confirm item to merge into:");
                vm.SearchTerm = mergeOptions.TargetSearchTerm;
                var combined = Disambiguate(vm.SearchResults.Where(x => x.ID != mergeVictim.ID), repo);
                if (mergeVictim != null && combined != null)
                {
                    vm.Merge(mergeVictim, combined);
                    selected = combined;
                }
            }

            TidyUp(vm, repo);
        }

        private static void MoveAll(MoveAllSubOptions moveAllOptions, ViewModel vm, TodoRepository repo)
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

            TidyUp(vm, repo);
        }

        private static void Move(MoveSubOptions moveOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(moveOptions, vm);

            vm.SearchSpecification = moveOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(moveOptions.ItemId));
            if (selected != null)
            {
                vm.SetContext(selected, moveOptions.NewContext);
                if (moveOptions.Unrank)
                {
                    vm.ResetPriorityParents(selected);
                }
            }

            TidyUp(vm, repo);
        }

        private static void Note(NoteSubOptions noteOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(noteOptions, vm);

            // Add a note to a task.
            vm.SearchSpecification = noteOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(noteOptions.ItemId));
            if (selected != null)
            {
                // Force verbose mode to display all notes and tags.
                vm.AddNote(selected, noteOptions.NewNote);
                verbose = true;
            }

            TidyUp(vm, repo);
        }

        private static void OpenTag(OpenTagSubOptions openTagOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(openTagOptions, vm);

            // Read a tag and pass it through to the "start" argVerb. Intended for URLs and file names.
            vm.SearchSpecification = openTagOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(openTagOptions.ItemId));
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
                    if (openTagOptions.Retag)
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

            TidyUp(vm, repo);
        }

        private static void Rank(MultiSearchSubOptions rankOptions, ViewModel vm, TodoRepository repo)
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
                    PrintItem(items.ElementAt(index[x]), 1, repo, rankOptions.NSFW);
                    PrintItem(items.ElementAt(index[x + 1]), 2, repo, rankOptions.NSFW);
                    Console.Write("Which of these is more important? (q=quit) ");
                    // assign parents based on vote
                    switch (Console.ReadKey().KeyChar)
                    {
                        case '1':
                            vm.SetParent(items.ElementAt(index[x + 1]), items.ElementAt(index[x]));
                            break;
                        case '2':
                            vm.SetParent(items.ElementAt(index[x]), items.ElementAt(index[x + 1]));
                            break;
                        case 'q':
                            Console.WriteLine();
                            Console.WriteLine("Quitting. Save ranking so far?");
                            Console.WriteLine("\tY: Quit and save.");
                            Console.WriteLine("\tN: Quit without saving (all work this session will be lost, no undo).");
                            Console.WriteLine("\tC: Cancel (default). Return to ranking.");
                            switch (Console.ReadKey().KeyChar)
                            {
                                case 'y':
                                    // Quit and save.
                                    quitAndSave = true;
                                    break;
                                case 'n':
                                    // Quit without saving.
                                    return;
                                    // Default. No action. Just return to ranking.
                            }
                            break;
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }

            TidyUp(vm, repo);
        }

        private static void Rename(RenameSubOptions renameOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(renameOptions, vm);

            vm.SearchSpecification = renameOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(renameOptions.ItemId));
            if (selected != null)
            {
                vm.Rename(selected, renameOptions.NewTitle);
                Console.WriteLine("Item renamed.");
                if (renameOptions.Retag)
                {
                    TagItem(vm, selected);
                }
            }

            TidyUp(vm, repo);
        }

        private static void Search(SearchOptions searchOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(searchOptions, vm);

            vm.SearchSpecification = searchOptions.GetSearchSpecification(repo);
            if (vm.SearchResults.Count() == 0)
            {
                Console.WriteLine("No results found.");
            }
            else if (searchOptions.PrintTree)
            {
                PrintTree(vm.SearchResults.ToList(), true, repo, searchOptions.NSFW);
            }
            else
            {
                PrintItems(searchOptions.SortTag ?? string.Empty, vm.SearchResults, repo, searchOptions.NSFW);
                if (!searchOptions.NoCount)
                {
                    Console.WriteLine("{0} item(s) found.", vm.SearchResults.Count());
                }
            }

            TidyUp(vm, repo);
        }

        private static void SetParent(SetParentSubOptions setParentOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(setParentOptions, vm);

            Console.WriteLine("Confirm child item:");
            vm.SearchSpecification = setParentOptions.GetChildSearchSpecification(repo);
            var child = Disambiguate(vm.SearchResults, repo);
            Console.WriteLine("Confirm parent item:");
            vm.SearchSpecification = setParentOptions.GetParentSearchSpecification(repo);
            if (child != null)
            {
                var parent = Disambiguate(vm.SearchResults.Where(x => x.ID != child.ID), repo);
                if (parent != null)
                {
                    vm.SetParent(child, parent);
                    Console.WriteLine();
                    PrintTree(new List<ActionItem> { { child }, { parent } }, false, repo, setParentOptions.NSFW);
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
                        PrintTree(new List<ActionItem> { child }, false, repo, setParentOptions.NSFW);
                    }
                }
            }

            TidyUp(vm, repo);
        }

        private static void SetProject(SetProjectSubOptions commandOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(commandOptions, vm);

            Console.WriteLine("Confirm child item:");
            vm.SearchTerm = commandOptions.ChildSearchTerm;
            var child = Disambiguate(vm.SearchResults, repo);
            Console.WriteLine("Confirm project:");
            vm.SearchTerm = commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm;
            var project = Disambiguate(vm.SearchResults, repo);
            if (child != null && project != null)
            {
                vm.SetProject(child, project);
            }

            TidyUp(vm, repo);
        }

        private static void SearchSomeday(SomedaySearchSubOptions search, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(search, vm);

            // Construct a search specification.
            // Set the ViewModel property.
            vm.SomedaySearchSpecification = search.GetSearchSpecification(repo);
            // Report the results.
            PrintItems(search.SortTag ?? "tickle-date", vm.SomedaySearchResults, repo, search.NSFW);
            if (!search.NoCount)
            {
                Console.WriteLine("{0} item(s) found.", vm.SomedaySearchResults.Count());
            }

            TidyUp(vm, repo);
        }

        private static void Someday(SomedaySubOptions somesub, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(somesub, vm);

            verbose = somesub.Verbose;
            // Display the whole Someday file, [somesub.PageSize] items at a time, and either delete or do one per listing.
            ActionItem? undefer = null;
            if (somesub.PageSize <= 0) somesub.PageSize = 1;
            if (somesub.PageSize > 10) somesub.PageSize = 10;
            var someitems = (from s in vm.SomedayItems where !s.TickleDate.HasValue || somesub.IncludeTickle select s);
            for (var offset = 0; offset <= someitems.Count(); offset += somesub.PageSize)
            {
                Console.Clear();
                for (var index = 0; index < somesub.PageSize && offset + index < someitems.Count(); index++)
                {
                    PrintItem(someitems.ElementAt(offset + index), index, repo, somesub.NSFW);
                }
                var choice = Console.ReadKey().KeyChar;
                Console.WriteLine();
                int dex;
                if (Int32.TryParse(choice.ToString(), out dex))
                {
                    undefer = someitems.ElementAt(offset + dex);
                }
                if (undefer != null)
                {
                    EditSomedayItem(vm, undefer, repo);
                    undefer = null;
                }
            }

            TidyUp(vm, repo);
        }

        public static void Update(UpdateSubOptions updateOptions, ViewModel vm)
        {
            var itemSource = new CsvReader(updateOptions.Filename);
            IEnumerable<Dictionary<string, string>> items = itemSource.GetAllItems();
            foreach (var item in items)
            {
                if (vm.FindByTag("url", item["URL"]) is ActionItem existingItem)
                {
                    if (item["Folder"].Equals("Archive", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (!existingItem.Done)
                        {
                            vm.MarkDone(DateTime.Now, existingItem);
                        }
                    }
                }
                else
                {
                    vm.AddItem(new ActionItem
                    {
                        ID = Guid.NewGuid(),
                        Title = string.IsNullOrWhiteSpace(item["Title"]) ? item["URL"] : item["Title"],
                        Context = string.IsNullOrWhiteSpace(updateOptions.Context) ? "instapaper" : updateOptions.Context,
                        Tags = new Dictionary<string, string>
                        {
                            { "url", item["URL"] },
                        },
                        LastModified = DateTime.Now,
                        RevisionGuid = Guid.NewGuid()
                    });
                }
            }
            vm.Save();
        }

        static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    System.Diagnostics.Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    System.Diagnostics.Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        private static void TagAll(TagAllSubOptions tagAllOptions, ViewModel vm, TodoRepository repo)
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

            TidyUp(vm, repo);
        }

        private static void Tag(SingleSearchSubOptions tagOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(tagOptions, vm);

            // Search for a matching item.
            vm.SearchSpecification = tagOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(tagOptions.ItemId));
            if (selected != null)
            {
                TagItem(vm, selected);
            }
            verbose = true; // Display the newly-added tags.

            TidyUp(vm, repo);
        }

        private static void Undefer(UndeferOptions undeferOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(undeferOptions, vm);

            vm.SomedaySearchSpecification = undeferOptions.SearchSpecification;
            selected = Disambiguate(vm.SomedaySearchResults, repo, !string.IsNullOrEmpty(undeferOptions.ItemId));
            if (selected != null)
            {
                vm.Undefer("inbox", selected);
            }

            TidyUp(vm, repo);
        }

        private static void Undo(UndoSubOptions undoOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(undoOptions, vm);

            vm.DoneSearchSpecification = undoOptions.SearchSpecification;
            selected = Disambiguate(vm.DoneSearchResults, repo, !string.IsNullOrEmpty(undoOptions.ItemId));
            if (selected != null)
            {
                vm.Undo(undoOptions.NewContext, selected);
            }

            TidyUp(vm, repo);
        }

        private static void Summary(UniversalOptions summaryArgs, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(summaryArgs, vm);

            if (summaryArgs.Verbose && !summaryArgs.ShowAllItems)
            {
                vm.ShowHeadOnly = false;
            }
            vm.SearchSpecification = new TrueSpecification<ActionItem>();
            var summarydata = (from i in vm.SearchResults group i by i.Context into c select new { Context = c.Key, Count = c.Count() });

            // TODO: Cuneiform table display.
            var maxwidth = (summarydata.Count() > 0 ? (from r in summarydata select r.Context.Length).Max() : 0);
            var maxnum = Math.Ceiling(Math.Log10((summarydata.Count() > 0 ? (from c in summarydata select c.Count).Max() : 0)));
            var total = 0;
            foreach (var c in summarydata)
            {
                total += c.Count;

                // @context         n item(s)
                var format = string.Format("@{{0}}\t{{1,{0}}} item{1}", maxnum, (c.Count == 1 ? "" : "s"));
                Console.WriteLine(format, c.Context.PadRight(maxwidth), c.Count);

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
                        format = $"\t{{0}}\t{{1,{maxnum}}} item{(d.Value == 1 ? "" : "s")}";
                        Console.WriteLine(format, d.Key, d.Value);
                    }
                    if (unknownCount > 0)
                    {
                        Console.WriteLine(format, "-", unknownCount);
                    }
                    Console.WriteLine();
                }
            }
            Console.WriteLine("Total\t{0}", total);

            TidyUp(vm, repo);
        }

        private static void UnrankAll(UnrankAllOptions unrankAllOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(unrankAllOptions, vm);

            vm.SearchSpecification = unrankAllOptions.GetSearchSpecification(repo);
            Console.WriteLine("About to delete all ranking data for {0} items. Continue [Y/N]?", vm.SearchResults.Count());
            if (Console.ReadKey().KeyChar.ToString().ToLower() == "y")
            {
                vm.ResetPriorityParents(vm.SearchResults.ToArray());
            }

            TidyUp(vm, repo);
        }

        private static void Unrank(UnRankOptions unrankOptions, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(unrankOptions, vm);

            vm.SearchSpecification = unrankOptions.SearchSpecification;
            selected = Disambiguate(vm.SearchResults, repo, !string.IsNullOrEmpty(unrankOptions.ItemId));
            if (selected != null)
            {
                vm.ResetPriorityParents(selected);
            }

            TidyUp(vm, repo);
        }

        private static void CheckChain(CheckChainOptions chainOptions, ViewModel vm, TodoRepository repo)
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
                    PrintItem(excluded, null, repo, chainOptions.NSFW);
                    problemsFound++;
                }
            }
            if (chainOptions.PauseOnProblems && problemsFound > 0)
            {
                Console.WriteLine("Press a key to continue...");
                Console.ReadKey();
            }

            TidyUp(vm, repo);
        }

        private static void SetUniversalOptions(object argsubs, ViewModel vm)
        {
            // Set universal options.
            if (argsubs is UniversalOptions options)
            {
                verbose = options.Verbose;
                if (!(argsubs is MultiSearchSubOptions))
                {
                    vm.ShowHeadOnly = !options.ShowAllItems;
                }
                else
                {
                    vm.ShowHeadOnly = false;
                }
            }
        }

        private static int Process(ProcessOptions argsubs, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(argsubs, vm);

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
            forceSave = argsubs.Force;
            vm.SomedaySearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
            vm.Undefer("inbox", vm.SomedaySearchResults.ToArray());

            vm.SearchSpecification = new ContextSearchSpecification("inbox");
            var inbox = vm.SearchResults.ToList();
            for (var i = 0; i < inbox.Count; i++)
            {
                // Assign the @inbox items to contexts.
                Console.WriteLine("To which context should this item go?");
                var first = inbox[i];
                PrintItem(first, null, repo);
                Console.WriteLine();
                PrintContexts(vm);
                var newcontext = Console.ReadLine();
                vm.SetContext(first, newcontext);
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
                    PrintItem(first, null, repo);
                    var nextaction = Console.ReadLine();
                    Console.WriteLine("...and to what context does it belong?");
                    PrintContexts(vm);
                    var newcontext = Console.ReadLine();
                    if (nextaction == newcontext)
                    {
                        // Wrote something like "someday"/"someday". Assume it is a new context.
                        vm.SetContext(first, newcontext);
                    }
                    else
                    {
                        var next = new ActionItem
                        {
                            Context = newcontext,
                            Title = nextaction,
                            ProjectId = first.ID
                        };
                        vm.AddItem(next);
                    }
                    Console.WriteLine();
                }
            }

            TidyUp(vm, repo);
            return 0;
        }

        private static void TidyUp(ViewModel vm, TodoRepository repo)
        {
            #region Tidy up
            // Delete any items with a context of "delete".
            vm.SearchSpecification = new ContextSearchSpecification("delete");
            vm.Delete(vm.SearchResults.ToArray());
            #endregion


            if (selected != null)
            {
                PrintItem(selected, null, repo);
            }

            // Rewrite the files
            vm.Save(forceSave);
        }

        private static void AddItem(AddSubOptions a, ViewModel vm, TodoRepository repo)
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
            verbose = true;
            TidyUp(vm, repo);
        }

        private static void AdvancedSearch(AdvancedSearchOptions argsubs, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(argsubs, vm);

            vm.SearchSpecification = argsubs.SearchSpecification;
            PrintItems("title", vm.SearchResults, repo, argsubs.NSFW);

            TidyUp(vm, repo);
        }

        private static void Balance(BalanceOptions balopts, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(balopts, vm);
            if (balopts.Verbose)
            {
                // Show progress percentage if in verbose mode.
                vm.PropertyChanged += VmOnPropertyChanged;
            }
            // Validate the branching factor: must be greater than zero.
            if (balopts.BranchFactor > 0)
            {
                vm.SearchSpecification = balopts.GetSearchSpecification(repo);
                var depths = vm.GetDepthsView();
                var vine = vm.SearchResults.OrderBy(i => depths[i.ID]).ThenByDescending(i => i.Upvotes).ToArray();
                vm.Balance(vine, balopts.BranchFactor);
                if (balopts.Commit)
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

            TidyUp(vm, repo);
        }

        private static void Bump(BumpOptions bumpOpts, ViewModel vm, TodoRepository repo)
        {
            SetUniversalOptions(bumpOpts, vm);
            vm.SearchSpecification = bumpOpts.SearchSpecification;
            var bumpItems = new List<ActionItem>();
            if (bumpOpts.Top == 0)
            {
                // Manually pick one item.
                var chosen = Disambiguate(vm.SearchResults, repo, true);
                if (chosen != null) bumpItems.Add(chosen);
            }
            else if (!string.IsNullOrEmpty(bumpOpts.SortTag))
            {
                // First items.
                bumpItems.AddRange(ApplySort(bumpOpts.SortTag, vm.SearchResults).Take(bumpOpts.Top));
            }
            else if (!string.IsNullOrEmpty(bumpOpts.SortDescTag))
            {
                bumpItems.AddRange(ApplySort(bumpOpts.SortTag ?? string.Empty, vm.SearchResults, SortOrder.Descending).Take(bumpOpts.Top));
            }
            else
            {
                bumpItems.AddRange(vm.SearchResults.Take(bumpOpts.Top));
            }
            // Before
            PrintTree(bumpItems, true, repo, bumpOpts.NSFW);
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
            PrintTree(bumpItems, true, repo, bumpOpts.NSFW);
            TidyUp(vm, repo);
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

        private static void EditSomedayItem(ViewModel vm, ActionItem item, ITodoRepository repo, bool nsfw = false)
        {
            while (true)
            {
                PrintItem(item, null, repo, nsfw);
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
                            var newcontext = Console.ReadLine();
                            vm.Undefer(newcontext, item);
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
                        var dateinput = Console.ReadLine();
                        DateTime parseddate;
                        if (DateTime.TryParse(dateinput, out parseddate))
                        {
                            vm.Defer(item, parseddate);
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

        private static IOrderedEnumerable<ActionItem> ApplySort(string sorttag, IEnumerable<ActionItem> list, SortOrder sort = SortOrder.Ascending)
        {
            var sortedlist = sort == SortOrder.Descending ? list.OrderByDescending(a => a.Context) : from a in list orderby a.Context select a;
            if (sorttag == "done-date")
            {
                if (sort == SortOrder.Descending)
                    sortedlist = sortedlist.ThenByDescending(i => i.DoneDate ?? DateTime.Now);
                else
                    sortedlist = sortedlist.ThenBy(i => i.DoneDate ?? DateTime.Now);
            }
            else if (sorttag == "tickle-date")
            {
                if (sort == SortOrder.Descending)
                    sortedlist = sortedlist.ThenByDescending(i => i.TickleDate ?? DateTime.Now);
                else
                    sortedlist = sortedlist.ThenBy(i => i.TickleDate ?? DateTime.Now);
            }
            else if (sorttag == "upvotes")
            {
                if (sort == SortOrder.Descending)
                    sortedlist = sortedlist.ThenByDescending(i => i.Upvotes);
                else
                    sortedlist = sortedlist.ThenBy(i => i.Upvotes);
            }
            else if (sorttag == "title" || string.IsNullOrEmpty(sorttag))
            {
                if (sort == SortOrder.Descending)
                    sortedlist = sortedlist.ThenByDescending(i => i.Title);
                else
                    sortedlist = sortedlist.ThenBy(i => i.Title);
            }
            else
            {
                if (sort == SortOrder.Descending)
                    sortedlist = sortedlist.ThenByDescending(a => a.Tags.ContainsKey(sorttag) ? a.Tags[sorttag] : "0", new SemiNumericComparer());
                else
                    sortedlist = sortedlist.ThenBy(a => a.Tags.ContainsKey(sorttag) ? a.Tags[sorttag] : "0", new SemiNumericComparer());
            }
            return sortedlist;
        }

        private static void PrintItems(string sorttag, IEnumerable<ActionItem> list, ITodoRepository repo, bool nsfw = false)
        {
            var last_context = string.Empty;
            var sortedlist = ApplySort(sorttag, list);
            foreach (var i in sortedlist)
            {
                if (i.Context != last_context)
                {
                    Console.WriteLine("@{0}", i.Context);
                }
                PrintItem(i, null, repo, nsfw);
                last_context = i.Context;
            }
        }

        private static void PrintTree(List<ActionItem> list, bool showAncestors, ITodoRepository repo, bool nsfw = false)
        {
            var ancestors = new List<ActionItem>();
            ancestors.AddRange(list);
            if (showAncestors)
            {
                // Fill out the results.
                for (var i = 0; i < ancestors.Count; i++)
                {
                    if (ancestors[i] != null && ancestors[i].ParentId != null && !ancestors.Contains(ancestors[i].GetParent(repo)))
                    {
                        ancestors.Add(ancestors[i].GetParent(repo));
                    }
                }
            }

            // Find the roots.
            var roots = ancestors.Where(t => t != null && (t.ParentId == null || !ancestors.Contains(t.GetParent(repo))));

            foreach (var r in roots)
            {
                PrintTree(r, list, ancestors, nsfw);
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private struct PrintTreeItem
        {
            public ActionItem Item;
            public int Depth;
            public string? PadLine;
        }

        private static void PrintTree(ActionItem root, List<ActionItem> tree, List<ActionItem> ancestors, bool nsfw = false)
        {
            var conwide = Console.WindowWidth;
            var stack = new Stack<PrintTreeItem>();
            stack.Push(new PrintTreeItem { Item = root, Depth = 1, PadLine = null });
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                var indent = node.Depth;
                var focus = node.Item;
                var children = ancestors.Where(i => i.ParentId == focus.ID);
                var prefix = new StringBuilder();
                var padline = new StringBuilder();
                for (var i = 0; i < indent; i++)
                {
                    if (i < indent - 1)
                    {
                        prefix.Append("| ");
                    }
                    else
                    {
                        prefix.Append("* ");
                    }
                    padline.Append("| ");
                }
                if (!string.IsNullOrEmpty(node.PadLine))
                {
                    Console.WriteLine(node.PadLine);
                }
                Console.Write(prefix);
                var name = focus.Title.Substring(0, Math.Min(conwide - prefix.Length, focus.Title.Length));
                if (focus.Tags.ContainsKey("nsfw") && focus.Tags["nsfw"].ToLower() == "true" && !nsfw)
                {
                    name = "(nsfw) " + Rot13.Transform(name);
                }
                if (tree != null && tree.Contains(focus))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(name);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(name);
                }
                if (name.Length + prefix.Length < conwide)
                {
                    Console.WriteLine();
                }

                for (var j = 0; j < children.Count(); j++)
                {
                    stack.Push(new PrintTreeItem { Item = children.ElementAt(j), PadLine = padline.ToString().Trim() + (j == 0 ? "" : "\\"), Depth = node.Depth + (j == 0 ? 0 : 1) });
                }
            }
        }

        /// <summary>
        /// Prints an individual item to the console.
        /// </summary>
        /// <param name="i">The item to print.</param>
        /// <remarks>
        /// TODO: Refactor into a Console View class?
        /// </remarks>
        private static void PrintItem(ActionItem i, int? index, ITodoRepository repo, bool nsfw = false)
        {
            var wrapwidth = Console.WindowWidth - 1;
            var title = i.Title;
            if (i.Tags?.ContainsKey("nsfw") ?? false && !nsfw)
            {
                title = "(nsfw) " + Rot13.Transform(title);
            }
            if (i.Tags?.ContainsKey("type") ?? false)
            {
                title = string.Format("{1} [{0}]", i.Tags["type"].ToUpper(), title);
            }
            if (i.DoneDate.HasValue)
            {
                title = string.Format("[{0:yyyy-MM-dd}] {1}", i.DoneDate.Value, title);
            }
            if (i.TickleDate.HasValue)
            {
                title = string.Format("[{0:yyyy-MM-dd}] {1}", i.TickleDate.Value, title);
            }
            if (i.IsDeleted)
            {
                title = $"[DELETED] {title}";
            }

            if (index.HasValue)
            {
                var prefix = new StringBuilder();
                prefix.Append(index);
                prefix.Append(':');
                prefix.Append(' ', Math.Max(4 - prefix.Length, 0));
                WrapOutput(prefix.ToString(), title, wrapwidth);
            }
            else
            {
                WrapOutput("-   ", title, wrapwidth);
            }
            if (verbose)
            {
                if (i.Notes.Count > 0)
                {
                    foreach (var n in i.Notes)
                    {
                        WrapOutput("        - ", n, wrapwidth);
                    }
                }
                if (i.Tags?.Count > 0)
                {
                    foreach (var k in i.Tags)
                    {
                        WrapOutput($"        #{k.Key}:", k.Value, wrapwidth);
                    }
                }
                if (i.Upvotes > 0)
                {
                    WrapOutput("        #upvotes:", i.Upvotes.ToString(), wrapwidth);
                }
                if (i.DoneDate.HasValue)
                {
                    WrapOutput("        #done-date:", i.DoneDate.Value.ToString("yyyy-MM-dd"), wrapwidth);
                }
                if (i.TickleDate.HasValue)
                {
                    WrapOutput("        #tickle-date:", i.TickleDate.Value.ToString("yyyy-MM-dd"), wrapwidth);
                }
                WrapOutput("        #ID:", i.ID.ToString(), wrapwidth);
                if (i.ProjectId != null)
                {
                    WrapOutput("        #project:", string.Format("{0} - {1}", i.ProjectId, i.GetProject(repo).Title), wrapwidth);
                }
                WrapOutput("        #context:", i.Context, wrapwidth);
                WrapOutput("        #last-modified:", i.LastModified.ToString("yyyy-MM-dd"), wrapwidth);
                // TODO: Any other special tags?
            }
        }

        private static void WrapOutput(string indent, string content, int width)
        {
            var printwidth = width - indent.Length;
            var breaks = " \t-/=&+_";
            var line = new StringBuilder();
            line.Append(indent);
            while (content.Length > printwidth)
            {
                var snip = Math.Min(printwidth, content.Length);
                for (var i = snip; i > 0; i--)
                {
                    if (breaks.Contains(content[i]))
                    {
                        snip = i;
                        break;
                    }
                }
                line.Append(content.Substring(0, snip));
                content = content.Remove(0, snip).TrimStart();
                Console.WriteLine(line);
                line.Clear();
                line.Append(' ', indent.Length);
            }
            Console.Write(line);
            Console.WriteLine(content);
        }

        private static ActionItem? Disambiguate(IEnumerable<ActionItem> todolist, ITodoRepository repo, bool autoAcceptOne = false, bool nsfw = false)
        {
            ActionItem? selected = null;

            // Disambiguate or verify search results.
            if (todolist.Count() == 0)
            {
                Console.WriteLine("No search matches. No action will be taken.");
            }
            else if (todolist.Count() > 9)
            {
                // Print the items so we know what to narrow down.
                for (var i = 0; i < todolist.Count(); i++)
                {
                    PrintItem(todolist.ElementAt(i), null, repo, nsfw);
                }
                Console.WriteLine("Too many search matches. Try to be more specific. No action will be taken this time.");
            }
            else if (todolist.Count() == 1 && autoAcceptOne)
            {
                PrintItem(todolist.ElementAt(0), null, repo, nsfw);
                Console.WriteLine("Auto-accepting...");
                selected = todolist.ElementAt(0);
            }
            else
            {
                for (var i = 0; i < todolist.Count(); i++)
                {
                    PrintItem(todolist.ElementAt(i), i, repo, nsfw);
                }
                var choice = Console.ReadKey().KeyChar;
                // Write a blank line to prevent whatever comes next from printing right after this on the same line.
                Console.WriteLine();
                if (int.TryParse(choice.ToString(), out int dex))
                {
                    if (todolist.Count() > dex)
                    {
                        selected = todolist.ElementAt(dex);
                    }
                }
            }
            return selected;
        }
    }
}
