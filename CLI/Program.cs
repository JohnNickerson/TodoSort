using AssimilationSoftware.PimData.Mappers.Text;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Export;
using AssimilationSoftware.TodoSort.Core.Import;
using AssimilationSoftware.TodoSort.Core.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI
{
    class Program
    {
        private static bool verbose = false;

        static void Main(string[] args)
        {
            string argverb = string.Empty;
            object argsubs = null;
            string settingspath = Path.Combine(Directory.GetCurrentDirectory(), ".todosort");
            var options = new Options.Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options,
                (verb, subOptions) =>
                {
                    argverb = verb;
                    argsubs = subOptions;
                }))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }
            else if (argverb == "init")
            {
                InitSubOptions initty = (InitSubOptions)argsubs;
                var initsettings = new FolderSettings();
                initsettings.TodoPath = initty.TodoFile;
                
                // Save settings.
                FolderSettings.SaveTo(settingspath, initsettings);
                return;
            }
            else if (!File.Exists(settingspath))
            {
                // Ask for initialisation? Just use defaults.
            }
            var f = FolderSettings.LoadFrom(settingspath);
            ActionItemDiskMapper todomapper = new ActionItemDiskMapper(f.TodoPath);

            ViewModel vm = new ViewModel(todomapper);

            // Set universal options.
            if (argsubs is UniversalOptions)
            {
                verbose = ((UniversalOptions)argsubs).Verbose;
                if (!(argsubs is MultiSearchSubOptions))
                {
                    vm.ShowHeadOnly = !((UniversalOptions)argsubs).ShowAllItems;
                }
                else
                {
                    vm.ShowHeadOnly = false;
                }
            }

            ActionItem selected = null;
            var force_save = false;
            switch (argverb)
            {
                #region Add
                case "add":
                    // Add a new item.
                    var a = (AddSubOptions)argsubs;
                    selected = new ActionItem(a.Context, a.ActionTitle);
                    if (!string.IsNullOrWhiteSpace(a.Note))
                    {
                        selected.Notes.Add(a.Note);
                    }
                    Console.WriteLine("Adding action '{0}' to context @{1}", a.ActionTitle, a.Context);
                    vm.AddItem(selected);
                    // Allow tagging right away.
                    TagItem(vm, selected);
                    verbose = true;
                    break;
                #endregion

                #region Advanced Search
                case "advanced-search":
                    {
                        vm.SearchSpecification = ((AdvancedSearchOptions)argsubs).SearchSpecification;
                        PrintItems("title", vm.SearchResults);
                        break;
                    }
                #endregion

                #region Balance
                case "balance":
                    {
                        var balopts = (BalanceOptions)argsubs;
                        // Validate the branching factor: must be greater than zero.
                        if (balopts.BranchFactor > 0)
                        {
                            vm.SearchSpecification = balopts.SearchSpecification;
                            var vine = vm.SearchResults.OrderBy(i => i.RankDepth).ThenByDescending(i => i.GetIntTag("upvotes", 0)).ToArray();
                            vm.Balance(vine, balopts.BranchFactor);
                        }
                        else
                        {
                            Console.WriteLine("Branching factor must be greater than zero.");
                        }
                        break;
                    }
                #endregion

                #region Bump
                case "bump":
                    {
                        var bumpOpts = (SingleSearchSubOptions)argsubs;
                        vm.SearchSpecification = bumpOpts.SearchSpecification;
                        var target = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(bumpOpts.ItemId));
                        if (target != null)
                        {
                            // Before
                            PrintTree(new List<ActionItem>(new[] { target }), true);
                            var depth = target.RankDepth / 2;
                            while (target.RankDepth > depth && target.RankParent != null)
                            {
                                vm.SetParent(target, target.RankParent.RankParent);
                            }
                            // After
                            PrintTree(new List<ActionItem>(new[] { target }), true);
                        }
                    }
                    break;
                #endregion

                #region Chain
                case "chain":
                    {
                        var balopts = (BalanceOptions)argsubs;
                        vm.SearchSpecification = balopts.SearchSpecification;
                        var vine = vm.SearchResults.OrderBy(i => i.GetIntTag("order", 0)).ToArray();
                        vm.Balance(vine, 1, false);
                        // Show the resulting chain.
                        PrintTree(vm.SearchResults.ToList(), true);
                        break;
                    }
                #endregion

                #region Dedupe
                case "dedupe":
                    {
                        var ddup = (DedupeOptions)argsubs;
                        vm.SearchSpecification = new TrueSpecification<ActionItem>();
                        foreach (var duptit in vm.GetDuplicateTitles())
                        {
                            // Search by title
                            vm.SearchSpecification = new ExactPropertyValueSpecification<ActionItem, string>(i => i.Title, duptit);
                            // Present options for merging
                            // Get user input
                            Console.WriteLine();
                            Console.WriteLine("Select one item to merge all others into (i = ignore):");
                            var master = Disambiguate(vm.SearchResults);
                            if (master != null)
                            {
                                // Merge all into master.
                                foreach (var c in vm.SearchResults.Except(new[] { master }).ToList())
                                {
                                    vm.Merge(c, master);
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
                                var master = Disambiguate(vm.SearchResults);
                                if (master != null)
                                {
                                    // Merge into master.
                                    foreach (var c in vm.SearchResults.Except(new[] { master }).ToList())
                                    {
                                        vm.Merge(c, master);
                                    }
                                }
                            }
                        }
                        vm.Save();
                    }
                    break;
                #endregion

                #region Defer
                case "defer":
                    // Move the item and its sub-items to the "someday" file.
                    DeferSubOptions deferopts = ((DeferSubOptions)argsubs);
                    vm.SearchSpecification = deferopts.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(deferopts.ItemId));
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
                    break;
                #endregion

                #region Delete
                case "delete":
                    // Find a matching item to delete.
                    vm.SearchSpecification = ((SingleSearchSubOptions)argsubs).SearchSpecification;
                    selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(((SingleSearchSubOptions)argsubs).ItemId));
                    vm.Delete(selected);
                    break;
                #endregion

                #region Done
                case "done":
                    {
                        // If there is a next action, create a new item and add it to the correct context.
                        var doneopts = (DoneSubOptions)argsubs;
                        vm.SearchSpecification = doneopts.SearchSpecification;
                        selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(doneopts.ItemId));
                        if (selected != null)
                        {
                            vm.MarkDone(doneopts.DoneDate, selected);
                        }
                    }
                    break;
                #endregion

                #region Export
                case "export":
                    // TODO: Work with Mustache# to externalise the formatting.
                    // TODO: Write to the console if the filename is empty.
                    IExporter exporter = null;
                    var exportOptions = (ExportSubOptions)argsubs;
                    if (!string.IsNullOrEmpty(exportOptions.TemplateFilename))
                    {
                        exporter = new TemplateExporter(exportOptions.Filename, exportOptions.TemplateFilename);
                    }
                    else
                    {
                        switch (exportOptions.Format)
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
                            default:
                                Console.WriteLine("Unknown output file format.");
                                break;
                        }
                    }
                    if (exporter != null)
                    {
                        vm.SearchSpecification = exportOptions.SearchSpecification;
                        if (string.IsNullOrEmpty(exportOptions.SortTag))
                        {
                            exporter.Export(vm.SearchResults.ToList());
                        }
                        else
                        {
                            exporter.Export(ApplySort(exportOptions.SortTag, vm.SearchResults).ToList());
                        }
                    }
                    break;
                #endregion

                #region Import
                case "import":
                    {
                        IImporter importer = null;
                        ImportSubOptions importOptions = (ImportSubOptions)argsubs;
                        switch (importOptions.Format)
                        {
                            case "todosort":
                                importer = new TextImporter { Filename = importOptions.Filename };
                                break;
                        }
                        if (importer != null)
                        {
                            vm.AddAllItems(importOptions.Context, importer.GetAllItems());
                        }
                    }
                    break;
                #endregion

                #region Merge
                case "merge":
                    var mergeOptions = (MergeSubOptions)argsubs;
                    Console.WriteLine("Confirm child item:");
                    vm.SearchTerm = mergeOptions.ChildSearchTerm ?? mergeOptions.TargetSearchTerm;
                    var mergevictim = Disambiguate(vm.SearchResults);
                    if (mergevictim != null)
                    {
                        Console.WriteLine("Confirm item to merge into:");
                        vm.SearchTerm = mergeOptions.TargetSearchTerm;
                        var combined = Disambiguate(vm.SearchResults.Where(x => x.ID != mergevictim.ID));
                        if (mergevictim != null && combined != null)
                        {
                            vm.Merge(mergevictim, combined);
                            selected = combined;
                        }
                    }
                    break;
                #endregion

                #region Move
                case "move":
                    {
                        var moveOptions = (MoveSubOptions)argsubs;
                        vm.SearchSpecification = moveOptions.SearchSpecification;
                        selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(moveOptions.ItemId));
                        if (selected != null)
                        {
                            vm.SetContext(selected, moveOptions.NewContext);
                            if (moveOptions.Unrank)
                            {
                                vm.ResetPriorityParents(selected);
                            }
                        }
                    }
                    break;
                #endregion

                #region Move All
                case "move-all":
                    {
                        var moveAllOptions = (MoveAllSubOptions)argsubs;
                        vm.ShowHeadOnly = false; // Make sure we move all items in the context, not just the head.
                        vm.SearchSpecification = moveAllOptions.SearchSpecification;
                        var items = vm.SearchResults.ToList();
                        int counter = 0;
                        while (items.Count > 0)
                        {
                            vm.SetContext(items[0], moveAllOptions.NewContext);
                            items.RemoveAt(0);
                            counter++;
                        }
                        Console.WriteLine(string.Format("{0} items moved to @{1}", counter, moveAllOptions.NewContext));
                    }
                    break;
                #endregion

                #region Note
                case "note":
                    // Add a note to a task.
                    NoteSubOptions noteOptions = (NoteSubOptions)argsubs;
                    vm.SearchSpecification = noteOptions.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(noteOptions.ItemId));
                    if (selected != null)
                    {
                        // Force verbose mode to display all notes and tags.
                        vm.AddNote(selected, noteOptions.NewNote);
                        verbose = true;
                    }
                    break;
                #endregion

                #region Open Tag
                case "open-tag":
                    // Read a tag and pass it through to the "start" argverb. Intended for URLs and file names.
                    OpenTagSubOptions opentagOptions = (OpenTagSubOptions)argsubs;
                    vm.SearchSpecification = opentagOptions.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(opentagOptions.ItemId));
                    if (selected != null)
                    {
                        if (selected.Tags.ContainsKey(opentagOptions.Tag))
                        {
                            string tagvalue = selected.Tags[opentagOptions.Tag];
                            OpenItemTag(tagvalue);
                            if (opentagOptions.Rename)
                            {
                                Console.WriteLine("What new title should this item have?");
                                var newtitle = Console.ReadLine();
                                if (!string.IsNullOrWhiteSpace(newtitle))
                                {
                                    vm.Rename(selected, newtitle);
                                }
                            }
                            if (opentagOptions.Retag)
                            {
                                TagItem(vm, selected);
                            }
                            if (opentagOptions.MarkAsDone)
                            {
                                vm.MarkDone(null, selected);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Tag not found: {0}", opentagOptions.Tag);
                        }
                    }
                    break;
                #endregion

                #region Process
                case "process":
                    // Go over the @someday items and look for tickle dates.
                    force_save = (argsubs as ProcessOptions).Force;
                    vm.SomedaySearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
                    vm.Undefer("inbox", vm.SomedaySearchResults.ToArray());

                    vm.SearchSpecification = new ContextSearchSpecification("inbox");
                    var inbox = vm.SearchResults.ToList();
                    for (int i = 0; i < inbox.Count; i++)
                    {
                        // Assign the @inbox items to contexts.
                        Console.WriteLine("To which context should this item go?");
                        ActionItem first = inbox[i];
                        PrintItem(first, null);
                        Console.WriteLine();
                        PrintContexts(vm);
                        string newcontext = Console.ReadLine();
                        vm.SetContext(first, newcontext);
                        Console.WriteLine();
                    }

                    // Need to find projects for which there is no next action.
                    vm.SearchSpecification = new ContextSearchSpecification("projects");
                    var projects = vm.SearchResults.ToList();
                    for (int i = 0; i < projects.Count; i++)
                    {
                        vm.ShowHeadOnly = false;
                        vm.SearchSpecification = new ProjectChildrenSearchSpecification(projects[i]);
                        if (vm.SearchResults.Count() == 0)
                        {
                            // Add next actions for projects.
                            Console.WriteLine("What is the next action required on this project?");
                            ActionItem first = projects[i];
                            PrintItem(first, null);
                            string nextaction = Console.ReadLine();
                            Console.WriteLine("...and to what context does it belong?");
                            PrintContexts(vm);
                            string newcontext = Console.ReadLine();
                            if (nextaction == newcontext)
                            {
                                // Wrote something like "someday"/"someday". Assume it is a new context.
                                vm.SetContext(first, newcontext);
                            }
                            else
                            {
                                var next = new ActionItem(newcontext, nextaction);
                                next.Project = first;
                                vm.AddItem(next);
                            }
                            Console.WriteLine();
                        }
                    }
                    
                    break;
                #endregion

                #region Defer All
                case "defer-all":
                    // Delete items below a specified depth.
                    var pruneOptions = (MultiSearchSubOptions)argsubs;
                    vm.SearchSpecification = pruneOptions.SearchSpecification;
					Console.WriteLine("About to defer {0} items. Continue [Y/N]?", vm.SearchResults.Count());
					var k = Console.ReadKey();
					if (k.KeyChar.ToString().ToLower() == "y")
					{
						vm.Defer(vm.SearchResults.ToArray());
					}
                    break;
                #endregion

                #region Rank
                case "rank":
                    {
                        var rankOptions = (MultiSearchSubOptions)argsubs;
                        // for each context..
                        bool quitandsave = false;
                        vm.ShowHeadOnly = true;
                        foreach (string con in vm.GetContextNames("inbox", "done"))
                        {
                            if (quitandsave) break;
                            // select all items without rank parents
                            vm.SearchSpecification = new ContextSearchSpecification(con).And(rankOptions.SearchSpecification);
                            var items = vm.SearchResults.ToArray();
                            var index = new List<int>();
                            for (int dex = 0; dex < items.Count(); dex++) index.Add(dex);
                            // randomise an index list
                            var rand = new Random();
                            for (int dex = 0; dex < index.Count; dex++)
                            {
                                int r = rand.Next(index.Count);
                                int b = index[dex];
                                index[dex] = index[r];
                                index[r] = b;
                            }
                            // show pairs of items
                            if (items.Count() > 1)
                            {
                                Console.WriteLine(string.Format("{1}{1}@{0}", con, Environment.NewLine));
                            }
                            for (int x = 0; x < items.Count() - 1; x += 2)
                            {
                                if (quitandsave) break;
                                // get vote
                                Console.WriteLine("{0}/{1} ({2}%) complete", x, items.Count(), 100 * x / items.Count());
                                PrintItem(items.ElementAt(index[x]), 1);
                                PrintItem(items.ElementAt(index[x + 1]), 2);
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
                                                quitandsave = true;
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
                    }
                    break;
                #endregion

                #region Rename
                case "rename":
                    // Rename an item.
                    RenameSubOptions renameOptions = (RenameSubOptions)argsubs;
                    vm.SearchSpecification = renameOptions.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(renameOptions.ItemId));
                    if (selected != null)
                    {
                        vm.Rename(selected, renameOptions.NewTitle);
                        Console.WriteLine("Item renamed.");
                        if (renameOptions.Retag)
                        {
                            TagItem(vm, selected);
                        }
                    }
                    break;
                #endregion

                #region Search
                case "search":
                    // Search for matching items.
                    var searchOptions = ((MultiSearchSubOptions)argsubs);
                    vm.SearchSpecification = searchOptions.SearchSpecification;
                    if (vm.SearchResults.Count() == 0)
                    {
                        Console.WriteLine("No results found.");
                    }
                    else if (searchOptions.PrintTree)
                    {
                        PrintTree(vm.SearchResults.ToList(), true);
                    }
                    else
                    {
                        PrintItems(searchOptions.SortTag, vm.SearchResults);
                        if (!searchOptions.NoCount)
                        {
                            Console.WriteLine("{0} item(s) found.", vm.SearchResults.Count());
                        }
                    }
                    break;
                #endregion

                #region Search Done
                case "search-done":
                    {
                        // Construct a search specification.
                        var search = ((DoneSearchSubOptions)argsubs);
                        // Set the ViewModel property.
                        vm.DoneSearchSpecification = search.SearchSpecification;
                        // Report the results.
                        PrintItems(search.SortTag ?? "done-date", vm.DoneSearchResults);
                        if (!search.NoCount)
                        {
                            Console.WriteLine("{0} item(s) found.", vm.DoneSearchResults.Count());
                        }
                    }
                    break;
                #endregion

                #region Search Someday
                case "search-someday":
                    {
                        // Construct a search specification.
                        var search = ((SomedaySearchSubOptions)argsubs);
                        // Set the ViewModel property.
                        vm.SomedaySearchSpecification = search.SearchSpecification;
                        // Report the results.
                        PrintItems(search.SortTag ?? "tickle-date", vm.SomedaySearchResults);
                        if (!search.NoCount)
                        {
                            Console.WriteLine("{0} item(s) found.", vm.SomedaySearchResults.Count());
                        }
                    }
                    break;
                #endregion

                #region Set Parent
                case "set-parent":
                    {
                        SetParentSubOptions setparentOptions = (SetParentSubOptions)argsubs;
                        Console.WriteLine("Confirm child item:");
                        vm.SearchSpecification = setparentOptions.ChildSearchSpecification;
                        var child = Disambiguate(vm.SearchResults);
                        Console.WriteLine("Confirm parent item:");
                        vm.SearchSpecification = setparentOptions.ParentSearchSpecification;
                        if (child != null)
                        {
                            // TODO: Allow null parent.
                            var parent = Disambiguate(vm.SearchResults.Where(x => x.ID != child.ID));
                            if (parent != null)
                            {
                                vm.SetParent(child, parent);
                                Console.WriteLine();
                                PrintTree(new List<ActionItem> { { child }, { parent } }, false);
                            }
                            else
                            {
                                // Null parent. Confirm to set to nothing.
                                Console.WriteLine("No parent selected. Remove current priority parent? [Y/N]");
                                k = Console.ReadKey();
                                if (k.KeyChar.ToString().ToLower() == "y")
                                {
                                    vm.SetParent(child, null);
                                    Console.WriteLine();
                                    PrintTree(new List<ActionItem> { child }, false);
                                }
                            }
                        }
                    }
                    break;
                #endregion

                #region Set Project
                case "set-project":
                    {
                        SetProjectSubOptions commandOptions = (SetProjectSubOptions)argsubs;
                        Console.WriteLine("Confirm child item:");
                        vm.SearchTerm = commandOptions.ChildSearchTerm;
                        var child = Disambiguate(vm.SearchResults);
                        Console.WriteLine("Confirm project:");
                        vm.SearchTerm = commandOptions.ProjectSearchTerm ?? commandOptions.ChildSearchTerm;
                        var project = Disambiguate(vm.SearchResults);
                        if (child != null && project != null)
                        {
                            vm.SetProject(child, project);
                        }
                    }
                    break;
                #endregion

                #region Someday
                case "someday":
                    {
                        SomedaySubOptions somesub = (SomedaySubOptions)argsubs;
                        verbose = somesub.Verbose;
                        // Display the whole Someday file, [somesub.PageSize] items at a time, and either delete or do one per listing.
                        ActionItem undefer = null;
                        if (somesub.PageSize <= 0) somesub.PageSize = 1;
                        if (somesub.PageSize > 10) somesub.PageSize = 10;
                        var someitems = (from s in vm.SomedayItems where !s.TickleDate.HasValue || somesub.IncludeTickle select s);
                        for (int offset = 0; offset <= someitems.Count(); offset += somesub.PageSize)
                        {
                            Console.Clear();
                            for (int index = 0; index < somesub.PageSize && offset + index < someitems.Count(); index++)
                            {
                                PrintItem(someitems.ElementAt(offset + index), index);
                            }
                            char choice = Console.ReadKey().KeyChar;
                            Console.WriteLine();
                            int dex;
                            if (Int32.TryParse(choice.ToString(), out dex))
                            {
                                undefer = someitems.ElementAt(offset + dex);
                            }
                            if (undefer != null)
                            {
                                EditSomedayItem(vm, undefer);
                                undefer = null;
                            }
                        }
                    }
                    break;
                #endregion

                #region Summary
                case "summary":
                    var summaryArgs = (UniversalOptions)argsubs;
                    vm.SearchSpecification = new TrueSpecification<ActionItem>();
                    var summarydata = (from i in vm.SearchResults group i by i.Context into c select new { Context = c.Key, Count = c.Count() });

                    int maxwidth = (summarydata.Count() > 0 ? (from r in summarydata select r.Context.Length).Max() : 0);
                    var maxnum = Math.Ceiling(Math.Log10((summarydata.Count() > 0 ? (from c in summarydata select c.Count).Max() : 0)));
                    int total = 0;
                    foreach (var c in summarydata)
                    {
                        total += c.Count;

                        // @context         n item(s)
                        string format = string.Format("@{{0}}\t{{1,{0}}} item{1}", maxnum, (c.Count == 1 ? "" : "s"));
                        Console.WriteLine(format, c.Context.PadRight(maxwidth), c.Count);

                        if (summaryArgs.Verbose)
                        {
                            // Show a summary of numbers at each depth.
                            vm.SearchSpecification = new ContextSearchSpecification(c.Context);
                            var detailed = (from r in vm.SearchResults group r by r.RankDepth into g select new { Depth = g.Key, Count = g.Count() });
                            foreach (var d in detailed)
                            {
                                format = string.Format("\t{{0}}\t{{1,{0}}} item{1}", maxnum, (d.Count == 1 ? "" : "s"));
                                Console.WriteLine(format, d.Depth, d.Count);
                            }
                            Console.WriteLine();
                        }
                    }
                    Console.WriteLine("Total\t{0}", total);
                    break;
                #endregion

                #region Tag
                case "tag":
                    // Search for a matching item.
                    var tagOptions = (SingleSearchSubOptions)argsubs;
                    vm.SearchSpecification = tagOptions.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(tagOptions.ItemId));
                    if (selected != null)
                    {
                        TagItem(vm, selected);
                    }
                    verbose = true; // Display the newly-added tags.
                    break;
                #endregion

                #region Tag All
                case "tag-all":
                    {
                        var tagAllOptions = (TagAllSubOptions)argsubs;
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
                    }
                    break;
                #endregion

                #region Undefer
                case "undefer":
                    {
                        var undeferOptions = (SingleSearchSubOptions)argsubs;
                        vm.SomedaySearchSpecification = undeferOptions.SearchSpecification;
                        selected = Disambiguate(vm.SomedaySearchResults, !string.IsNullOrEmpty(undeferOptions.ItemId));
                        if (selected != null)
                        {
                            vm.Undefer("inbox", selected);
                        }
                    }
                    break;
                #endregion

                #region Undo
                case "undo":
                    {
                        var undoOptions = (UndoSubOptions)argsubs;
                        vm.DoneSearchSpecification = undoOptions.SearchSpecification;
                        selected = Disambiguate(vm.DoneSearchResults, !string.IsNullOrEmpty(undoOptions.ItemId));
                        if (selected != null)
                        {
                            vm.Undo(undoOptions.NewContext, selected);
                        }
                    }
                    break;
                #endregion

                #region Unrank
                case "unrank":
                    var unrankOptions = (SingleSearchSubOptions)argsubs;
                    vm.SearchSpecification = unrankOptions.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults, !string.IsNullOrEmpty(unrankOptions.ItemId));
                    if (selected != null)
                    {
                        vm.ResetPriorityParents(selected);
                    }
                    break;
                #endregion

                #region Unrank All
                case "unrank-all":
                    {
                        var unrankAllOptions = (MultiSearchSubOptions)argsubs;
                        vm.SearchSpecification = unrankAllOptions.SearchSpecification;
                        Console.WriteLine("About to delete all ranking data for {0} items. Continue [Y/N]?", vm.SearchResults.Count());
                        if (Console.ReadKey().KeyChar.ToString().ToLower() == "y")
                        {
                            vm.ResetPriorityParents(vm.SearchResults.ToArray());
                        }
                    }
                    break;
                #endregion

                #region Version
                case "version":
                    {
                        Console.WriteLine("TodoSort {0}", Assembly.GetExecutingAssembly().GetName().Version);
                        Console.WriteLine(((AssemblyCopyrightAttribute)Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright);
                    }
                    break;
                #endregion
            }

            #region Tidy up
            // Delete any items with a context of "delete".
            vm.SearchSpecification = new ContextSearchSpecification("delete");
            vm.Delete(vm.SearchResults.ToArray());
            #endregion

            if (selected != null)
            {
                PrintItem(selected, null);
            }

			// Rewrite the files
            vm.Save(force_save);
        }

        private static void PrintContexts(ViewModel vm)
        {
            foreach (var c in vm.GetContextNames())
            {
                Console.WriteLine(c);
            }
        }

        private static void OpenItemTag(string tagvalue)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = tagvalue;
            p.Start();
        }

        private static void EditSomedayItem(ViewModel vm, ActionItem item)
        {
            while(true)
            {
                PrintItem(item, null);
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
                            string newcontext = Console.ReadLine();
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
                        if (item.Tags.ContainsKey(tag))
                        {
                            OpenItemTag(item.Tags[tag]);
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
            string tagname;
            do
            {
                Console.WriteLine("What should this new tag be called? (ENTER to quit)");
                tagname = Console.ReadLine().ToLower().Trim();
                if (tagname.Length > 0)
                {
                    Console.WriteLine("What is the value of the tag?");
                    var value = Console.ReadLine();
                    if (value.Trim().Length > 0)
                    {
                        vm.SetTag(selected, tagname, value);
                    }
                    else if (selected.Tags.ContainsKey(tagname))
                    {
                        vm.RemoveTag(selected, tagname);
                    }
                    Console.WriteLine();
                }
            } while (tagname.Length > 0);
        }

        private static DateTime? ConfigureDate(DateTime preset, string prompt)
        {
            Console.WriteLine(prompt);
            Console.WriteLine("Type correct value or [Enter] to accept default ('null' for null).");
            Console.WriteLine(preset.ToString("yyyy-MM-dd"));
            var response = Console.ReadLine();
            if (response.Trim().Length > 0)
            {
                if (response.ToLower() == "null")
                {
                    return null;
                }
                else
                {
                    DateTime result;
                    if (DateTime.TryParse(response, out result))
                    {
                        Console.WriteLine();
                        return result;
                    }
                    else
                    {
                        Console.WriteLine();
                        return preset;
                    }
                }
            }
            Console.WriteLine();
            return preset;
        }

        private static IOrderedEnumerable<ActionItem> ApplySort(string sorttag, IEnumerable<ActionItem> list)
        {
            var sortedlist = from a in list orderby a.Context select a;
            if (sorttag == "done-date")
            {
                sortedlist = sortedlist.ThenBy(i => i.DoneDate ?? DateTime.Now);
            }
            else if (sorttag == "tickle-date")
            {
                sortedlist = sortedlist.ThenBy(i => i.TickleDate ?? DateTime.Now);
            }
			else if (sorttag == "upvotes")
			{
				sortedlist = sortedlist.ThenBy(i => i.Upvotes);
			}
            else if (sorttag == "title" || string.IsNullOrEmpty(sorttag))
            {
                sortedlist = sortedlist.ThenBy(i => i.Title);
            }
            else
            {
                sortedlist = sortedlist.ThenBy(a => a.Tags.ContainsKey(sorttag) ? a.Tags[sorttag] : "0", new SemiNumericComparer());
            }
            return sortedlist;
        }

        private static void PrintItems(string sorttag, IEnumerable<ActionItem> list)
        {
            string last_context = string.Empty;
            var sortedlist = ApplySort(sorttag, list);
            foreach (ActionItem i in sortedlist)
            {
                if (i.Context != last_context)
                {
                    Console.WriteLine(string.Format("@{0}", i.Context));
                }
                PrintItem(i, null);
                last_context = i.Context;
            }
        }

        private static void PrintTree(List<ActionItem> list, bool showAncestors)
        {
            List<ActionItem> ancestors = new List<ActionItem>();
            ancestors.AddRange(list);
            if (showAncestors)
            {
                // Fill out the results.
                for (int i = 0; i < ancestors.Count; i++)
                {
                    if (ancestors[i].RankParent != null && !ancestors.Contains(ancestors[i].RankParent))
                    {
                        ancestors.Add(ancestors[i].RankParent);
                    }
                }
            }

            // Find the roots.
            var roots = ancestors.Where(t => t.RankParent == null || !ancestors.Contains(t.RankParent));

            foreach (var r in roots)
            {
                PrintTree(r, list, ancestors);
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private struct PrintTreeItem
        {
            public ActionItem Item;
            public int Depth;
            public string PadLine;
        }

        private static void PrintTree(ActionItem root, List<ActionItem> tree, List<ActionItem> ancestors)
        {
            var conwide = Console.WindowWidth;
            var stack = new Stack<PrintTreeItem>();
            stack.Push(new PrintTreeItem { Item = root, Depth = 1, PadLine = null });
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                int indent = node.Depth;
                ActionItem focus = node.Item;
                var children = ancestors.Where(i => i.RankParent == focus);
                var prefix = new StringBuilder();
                var padline = new StringBuilder();
                for (int i = 0; i < indent; i++)
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
                string name = focus.Title.Substring(0, Math.Min(conwide - prefix.Length, focus.Title.Length));
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

                for (int j = 0; j < children.Count(); j++)
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
        private static void PrintItem(ActionItem i, int? index)
        {
            int wrapwidth = Console.WindowWidth - 1;
            string title = i.Title;
            if (i.Tags.ContainsKey("type"))
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

            if (index.HasValue)
            {
                StringBuilder prefix = new StringBuilder();
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
                    for (int x = 0; x < i.Notes.Count; x++)
                    {
                        WrapOutput("        - ", i.Notes[x], wrapwidth);
                    }
                }
                if (i.Tags.Count > 0)
                {
                    foreach (var k in i.Tags)
                    {
                        WrapOutput(string.Format("        #{0}:", k.Key), k.Value, wrapwidth);
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
                if (i.Project != null)
                {
                    WrapOutput("        #project:", string.Format("{0} - {1}", i.Project.ID, i.Project.Title), wrapwidth);
                }
                // TODO: Any other special tags?
            }
        }

        private static void WrapOutput(string indent, string content, int width)
        {
            var printwidth = width - indent.Length;
            var breaks = " \t-/=&+_";
            StringBuilder line = new StringBuilder();
            line.Append(indent);
            while (content.Length > printwidth)
            {
                int snip = Math.Min(printwidth, content.Length);
                for (int i = snip; i > 0; i--)
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

		private static ActionItem Disambiguate(IEnumerable<ActionItem> todolist, bool autoAcceptOne = false)
		{
			ActionItem selected = null;

            // Disambiguate or verify search results.
            if (todolist.Count() == 0)
			{
				Console.WriteLine("No search matches. No action will be taken.");
			}
            else if (todolist.Count() > 9)
			{
                // Print the items so we know what to narrow down.
                for (int i = 0; i < todolist.Count(); i++)
                {
                    PrintItem(todolist.ElementAt(i), null);
                }
                Console.WriteLine("Too many search matches. Try to be more specific. No action will be taken this time.");
			}
            else if (todolist.Count() == 1 && autoAcceptOne)
            {
                PrintItem(todolist.ElementAt(0), null);
                Console.WriteLine("Auto-accepting...");
                selected = todolist.ElementAt(0);
            }
			else
			{
                for (int i = 0; i < todolist.Count(); i++)
				{
                    PrintItem(todolist.ElementAt(i), i);
				}
				char choice = Console.ReadKey().KeyChar;
                // Write a blank line to prevent whatever comes next from printing right after this on the same line.
                Console.WriteLine();
				int dex;
                if (Int32.TryParse(choice.ToString(), out dex))
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
