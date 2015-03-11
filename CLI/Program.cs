using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Mappers;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.CLI.Properties;
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
			// Check settings
            string settingspath = Path.Combine(Directory.GetCurrentDirectory(), ".todosort");
            FolderSettings f = FolderSettings.LoadFrom(settingspath);

            string argverb = string.Empty;
            object argsubs = null;
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


            bool changesettings = false;
            if (f == null || argverb == "init")
            {
                InitSubOptions initty = (InitSubOptions)argsubs;
                f.TodoPath = initty.TodoFile;
                f.SomedayPath = initty.SomedayFile;
                f.DonePath = initty.DoneFile;

                changesettings = true;
			}

            // Fix possible configuration problems.
            if (f.SomedayPath == f.TodoPath || f.SomedayPath == string.Empty)
            {
                f.SomedayPath = null;
                changesettings = true;
            }
            if (f.DonePath == f.TodoPath || f.DonePath == string.Empty)
            {
                f.DonePath = null;
                changesettings = true;
            }

            if (changesettings)
            {
                // Save settings.
                FolderSettings.SaveTo(settingspath, f);
            }

            ActionItemDiskMapper todomapper = new ActionItemDiskMapper(f.TodoPath);
            ActionItemDiskMapper somedaymapper = (f.SomedayPath == null ? null : new ActionItemDiskMapper(f.SomedayPath));
            ActionItemDiskMapper donemapper = (f.DonePath == null ? null : new ActionItemDiskMapper(f.DonePath));

            ViewModel vm = new ViewModel(todomapper, donemapper, somedaymapper);

            // Set universal options.
            if (argsubs is UniversalOptions)
            {
                verbose = ((UniversalOptions)argsubs).Verbose;
                vm.ShowHeadOnly = !((UniversalOptions)argsubs).ShowAllItems;
            }

            // Search for a matching item in all contexts.
            ActionItem selected = null;
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

                #region Count Children
                case "count-children":
                    {
                        var countOptions = (MultiSearchSubOptions)argsubs;
                        vm.SearchSpecification = countOptions.SearchSpecification;
                        var childcounts = from p in vm.SearchResults select new { Item = p, ChildCount = (from c in vm.Items where c.RankParent == p select c).Count() };
                        foreach (var item in childcounts)
                        {
                            PrintItem(item.Item, null);
                            Console.WriteLine(string.Format("\t{0} children", item.ChildCount));
                            vm.SetTag(item.Item, "children", item.ChildCount.ToString());
                        }
                        selected = null;
                    }
                    break;
                #endregion

                #region Defer
                case "defer":
                    // Move the item and its sub-items to the "someday" file.
                    DeferSubOptions deferopts = ((DeferSubOptions)argsubs);
                    vm.SearchSpecification = deferopts.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults);
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
                    selected = Disambiguate(vm.SearchResults);
                    vm.Delete(selected);
                    break;
                #endregion

                #region Done
                case "done":
                    {
                        // If there is a next action, create a new item and add it to the correct context.
                        var doneopts = (DoneSubOptions)argsubs;
                        vm.SearchSpecification = doneopts.SearchSpecification;
                        selected = Disambiguate(vm.SearchResults);
                        if (selected != null)
                        {
                            vm.MarkDone(doneopts.DoneDate, selected);
                        }
                    }
                    break;
                #endregion

                #region Export
                case "export":
                    // Write GraphViz source.
                    // TODO: Work with Mustache# to externalise the formatting.
                    // TODO: Write to the console if the filename is empty.
                    IExporter exporter = null;
                    var exportOptions = (ExportSubOptions)argsubs;
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
                    if (exporter != null)
                    {
                        vm.SearchSpecification = exportOptions.SearchSpecification;
                        exporter.Export(vm.SearchResults.ToList());
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
                    vm.SearchTerm = mergeOptions.FirstSearchTerm;
                    var mergevictim = Disambiguate(vm.SearchResults);
                    if (mergevictim != null)
                    {
                        Console.WriteLine("Confirm item to merge into:");
                        vm.SearchTerm = mergeOptions.SecondSearchTerm ?? mergeOptions.FirstSearchTerm;
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
                        selected = Disambiguate(vm.SearchResults);
                        if (selected != null)
                        {
                            vm.SetContext(selected, moveOptions.NewContext);
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
                    selected = Disambiguate(vm.SearchResults);
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
                    selected = Disambiguate(vm.SearchResults);
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
                    vm.SearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
                    vm.Undefer("inbox", vm.SearchResults.ToArray());

                    vm.SearchSpecification = new ContextSearchSpecification("inbox");
                    var inbox = vm.SearchResults.ToList();
                    for (int i = 0; i < inbox.Count; i++)
                    {
                        // Assign the @inbox items to contexts.
                        Console.WriteLine("To which context should this item go?");
                        ActionItem first = inbox[i];
                        PrintItem(first, null);
                        string newcontext = Console.ReadLine();
                        vm.SetContext(first, newcontext);
                        Console.WriteLine();
                    }

                    // Need to find projects for which there is no next action. I hate that kind of query. It's a "where not exists (subquery)".
                    vm.SearchSpecification = new ContextSearchSpecification("projects");
                    var projects = vm.SearchResults.ToList();
                    for (int i = 0; i < projects.Count; i++)
                    {
                        vm.SearchSpecification = new ProjectChildrenSearchSpecification(projects[i]);
                        if (vm.SearchResults.Count() == 0)
                        {
                            // Add next actions for projects.
                            Console.WriteLine("What is the next action required on this project?");
                            ActionItem first = projects[i];
                            PrintItem(first, null);
                            string nextaction = Console.ReadLine();
                            Console.WriteLine("...and to what context does it belong?");
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
                        // for each context..
                        bool quitandsave = false;
                        vm.ShowHeadOnly = true;
                        foreach (string con in vm.GetContextNames("inbox", "done"))
                        {
                            if (quitandsave) break;
                            // select all items without rank parents
                            vm.SearchSpecification = new ContextSearchSpecification(con);
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
                    selected = Disambiguate(vm.SearchResults);
                    if (selected != null)
                    {
                        vm.Rename(selected, renameOptions.NewTitle);
                        Console.WriteLine("Item renamed.");
                    }
                    break;
                #endregion

                #region Search
                case "search":
                    // Search for matching items.
                    var searchOptions = ((MultiSearchSubOptions)argsubs);
                    vm.SearchSpecification = searchOptions.SearchSpecification;
                    PrintItems(searchOptions.SortTag, vm.SearchResults);
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
                        PrintItems(search.SortTag, vm.DoneSearchResults);
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
                        PrintItems(search.SortTag, vm.SomedaySearchResults);
                    }
                    break;
                #endregion

                #region Set Parent
                case "set-parent":
                    {
                        SetParentSubOptions setparentOptions = (SetParentSubOptions)argsubs;
                        Console.WriteLine("Confirm child item:");
                        vm.SearchTerm = setparentOptions.ChildSearchTerm;
                        var child = Disambiguate(vm.SearchResults);
                        Console.WriteLine("Confirm parent item:");
                        vm.SearchTerm = setparentOptions.ParentSearchTerm ?? setparentOptions.ChildSearchTerm;
                        var parent = Disambiguate(vm.SearchResults);
                        if (child != null && parent != null)
                        {
                            vm.SetParent(child, parent);
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

                #region Show Parents
                case "show-parents":
                    {
                        var showParentOptions = (SingleSearchSubOptions)argsubs;
                        vm.SearchSpecification = showParentOptions.SearchSpecification;
                        selected = Disambiguate(vm.SearchResults);
                        if (selected != null)
                        {
                            // Build the chain of parent items up the tree.
                            List<ActionItem> ancestors = new List<ActionItem>();
                            ancestors.Add(selected);
                            while (selected.RankParent != null)
                            {
                                selected = selected.RankParent;
                                ancestors.Add(selected);
                            }
                            // Show the tree.
                            Console.WriteLine();
                            while (ancestors.Count > 0)
                            {
                                PrintItem(ancestors[ancestors.Count - 1], null);
                                ancestors.RemoveAt(ancestors.Count - 1);
                                if (ancestors.Count > 0)
                                {
                                    Console.WriteLine("\t/|\\");
                                    Console.WriteLine("\t |");
                                }
                            }
                            Console.WriteLine();
                            selected = null;
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
                    var summarydata = (from i in vm.SearchResults group i by i.Context into c select new { Context = c.Key, Count = c.Count() });

                    int maxwidth = (summarydata.Count() > 0 ? (from r in summarydata select r.Context.Length).Max() : 0);
                    int maxnum = (summarydata.Count() > 0 ? (from c in summarydata select c.Count).Max() : 0);
                    foreach (var c in summarydata)
                    {
                        // @context         n item(s)
                        string format = string.Format("@{{0}}\t{{1,{0}}} item{1}", Math.Ceiling(Math.Log10(maxnum)), (c.Count == 1 ? "" : "s"));
                        Console.WriteLine(format, c.Context.PadRight(maxwidth), c.Count);

                        if (summaryArgs.Verbose)
                        {
                            // Show a summary of numbers at each depth.
                            vm.SearchSpecification = new ContextSearchSpecification(c.Context);
                            var detailed = (from r in vm.SearchResults group r by r.RankDepth into g select new { Depth = g.Key, Count = g.Count() });
                            foreach (var d in detailed)
                            {
                                format = string.Format("\t{{0}}\t{{1,{0}}} item{1}", Math.Ceiling(Math.Log10(maxnum)), (d.Count == 1 ? "" : "s"));
                                Console.WriteLine(format, d.Depth, d.Count);
                            }
                            Console.WriteLine();
                        }
                    }
                    break;
                #endregion

                #region Tag
                case "tag":
                    // Search for a matching item.
                    var tagOptions = (SingleSearchSubOptions)argsubs;
                    vm.SearchSpecification = tagOptions.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults);
                    if (selected != null)
                    {
                        TagItem(vm, selected);
                    }
                    break;
                #endregion

                #region Undefer
                case "undefer":
                    {
                        var undeferOptions = (SingleSearchSubOptions)argsubs;
                        vm.SomedaySearchSpecification = undeferOptions.SearchSpecification;
                        selected = Disambiguate(vm.SomedaySearchResults);
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
                        var undoOptions = (SingleSearchSubOptions)argsubs;
                        vm.DoneSearchSpecification = undoOptions.SearchSpecification;
                        selected = Disambiguate(vm.DoneSearchResults);
                        if (selected != null)
                        {
                            vm.Undo("inbox", selected);
                        }
                    }
                    break;
                #endregion

                #region Unrank
                case "unrank":
                    var unrankOptions = (SingleSearchSubOptions)argsubs;
                    vm.SearchSpecification = unrankOptions.SearchSpecification;
                    selected = Disambiguate(vm.SearchResults);
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
                        Console.WriteLine("Copyright {0}", ((AssemblyCopyrightAttribute)Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright);
                    }
                    break;
                #endregion
            }

            #region Tidy up
            // Move to the "done" file any items with a context of @done.
            if (donemapper != null)
            {
                vm.SearchSpecification = new ContextSearchSpecification("done");
                vm.MarkDone(null, vm.SearchResults.ToArray());
            }

            // Move any "someday" items in the main list to the someday file.
            if (somedaymapper != null)
            {
                vm.SearchSpecification = new ContextSearchSpecification("someday");
                vm.Defer(vm.SearchResults.ToArray());
            }

            // Delete any items with a context of "delete".
            vm.SearchSpecification = new ContextSearchSpecification("delete");
            vm.Delete(vm.SearchResults.ToArray());
            #endregion

            if (selected != null)
            {
                PrintItem(selected, null);
            }

			// Rewrite the files
            vm.Save();
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

        private static void PrintItems(string sorttag, IEnumerable<ActionItem> list)
        {
            string last_context = string.Empty;
            var sortedlist = from a in list orderby a.Context, a.Title select a;
            if (!string.IsNullOrEmpty(sorttag) && sorttag != "title")
            {
                sortedlist = list.OrderBy(a => a.Context).ThenBy(a => a.Tags.ContainsKey(sorttag) ? a.Tags[sorttag] : string.Empty, new SemiNumericComparer());
            }
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

        /// <summary>
        /// Prints an individual item to the console.
        /// </summary>
        /// <param name="i">The item to print.</param>
        /// <remarks>
        /// TODO: Refactor into a Console View class?
        /// </remarks>
        private static void PrintItem(ActionItem i, int? index)
        {
            int wrapwidth = 79;
            string title = i.Title;
            if (i.Tags.ContainsKey("type"))
            {
                title = string.Format("{1} [{0}]", i.Tags["type"].ToUpper(), title);
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
                if (i.DoneDate.HasValue)
                {
                    WrapOutput("        #done-date:", i.DoneDate.Value.ToString("yyyy-MM-dd"), wrapwidth);
                }
                if (i.TickleDate.HasValue)
                {
                    WrapOutput("        #tickle-date:", i.TickleDate.Value.ToString("yyyy-MM-dd"), wrapwidth);
                }
                WrapOutput("        #ID:", i.ID.ToString(), wrapwidth);
                // TODO: priority-parent and other special tags.
            }
        }

        private static void WrapOutput(string indent, string content, int width)
        {
            var printwidth = width - indent.Length;
            Console.Write(indent);
            Console.WriteLine(content.Substring(0, Math.Min(printwidth, content.Length)));
            content = content.Remove(0, Math.Min(printwidth, content.Length));
            while (content.Length > 0)
            {
                var line = new StringBuilder();
                line.Append(' ', indent.Length);
                line.Append(content.Substring(0, Math.Min(printwidth, content.Length)));
                Console.WriteLine(line);
                content = content.Remove(0, Math.Min(printwidth, content.Length));
            }
        }

		private static ActionItem Disambiguate(IEnumerable<ActionItem> todolist)
		{
			ActionItem selected = null;

            // Disambiguate or verify search results.
            if (todolist.Count() == 0)
			{
				Console.WriteLine("No search matches. No action will be taken.");
			}
            else if (todolist.Count() > 9)
			{
				Console.WriteLine("Too many search matches. Try to be more specific. No action will be taken this time.");
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

        /// <summary>
        /// Prompts to configure a path based on an existing value.
        /// </summary>
        /// <param name="path">The path as it exists. May include "{MyDocs}" as a placeholder.</param>
        /// <param name="prompt">The human-friendly name of the folder to be used as a cue.</param>
        /// <returns>The correct path as provided by the user.</returns>
        public static string ConfigurePath(string path, string prompt, bool allowNull)
        {
            // Special folder replacements.
            path = path.Replace("{MyDocs}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            path = path.Replace("{MyPictures}", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            path = path.Replace("{MachineName}", Environment.MachineName);

            Console.WriteLine("Configure path to {0}:", prompt);
            if (allowNull)
            {
                Console.WriteLine("Type correct value, [Enter] to accept default or type the word \"null\" for null.");
            }
            else
            {
                Console.WriteLine("Type correct value or [Enter] to accept default.");
            }
            if (path != null && path.Length > 0)
            {
                Console.WriteLine(Path.GetFullPath(path));
            }
            else
            {
                Console.WriteLine("[no default]");
            }
            var response = Console.ReadLine();
            if (response.Trim().Length > 0)
            {
                if (allowNull && response.ToLower().Trim() == "null")
                {
                    path = null;
                }
                else
                {
                    path = response;
                }
                Console.WriteLine();
            }
            return path;
        }
    }
}
