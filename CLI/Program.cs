using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Mappers;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.CLI.Properties;
using AssimilationSoftware.TodoSort.Core;
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


            if (f == null || argverb == "init")
            {
                f.TodoPath = ConfigurePath("todo.txt", "Configure path to 'todo' file", false);
                f.SomedayPath = ConfigurePath("someday.txt", "Configure path to 'someday' file", true);
                f.DonePath = ConfigurePath("done.txt", "Configure path to 'done' file", true);

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
                    var item = new ActionItem(a.Context, a.ActionTitle);
                    Console.WriteLine("Adding action '{0}' to context @{1}", a.ActionTitle, a.Context);
                    vm.AddItem(item);
                    // Allow tagging right away.
                    TagItem(vm, item);
                    break;
                #endregion

                #region Advanced Search
                case "advanced-search":
                    var searchterms = (AdvancedSearchSubOptions)argsubs;
                    PrintItems(vm.Search(searchterms.Context, searchterms.Title, searchterms.Note, searchterms.ID, searchterms.TagName, searchterms.TagValue, searchterms.MinDepth, searchterms.MaxDepth));
                    break;
                #endregion

                #region Defer
                case "defer":
                    // Move the item and its sub-items to the "someday" file.
                    DeferSubOptions deferopts = ((DeferSubOptions)argsubs);
                    vm.SearchTerm = deferopts.SearchTerm;
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
                    vm.SearchTerm = ((DeleteSubOptions)argsubs).SearchTerm;
                    selected = Disambiguate(vm.SearchResults);
                    vm.Delete(selected);
                    break;
                #endregion

                    #region Done
                    case "done":
                        // If there is a next action, create a new item and add it to the correct context.
                        vm.SearchTerm = ((DoneSubOptions)argsubs).SearchTerm;
                        selected = Disambiguate(vm.SearchResults);
                        if (selected != null)
                        {
                            vm.MarkDone(selected);
                        }
                        break;
                    #endregion

                    #region Export
                    case "export":
                        // Write GraphViz source.
                        IExporter exporter = null;
                        ExportSubOptions exportOptions = (ExportSubOptions)argsubs;
                        switch (exportOptions.Format)
                        {
                            case "html":
                                exporter = new HtmlExporter { Filename = exportOptions.Filename };
                                break;
                            case "graphviz":
                                exporter = new GraphVizExporter { Filename = exportOptions.Filename };
                                break;
                        }
                        if (exporter != null)
                        {
                            exporter.Export(vm.GetContextItems(exportOptions.Context).ToList());
                        }
                        break;
                    #endregion

                    #region Merge
                    case "merge":
                        var mergeOptions = (MergeSubOptions)argsubs;
                        Console.WriteLine("Confirm first item:");
                        vm.SearchTerm = mergeOptions.FirstSearchTerm;
                        var mergefirst = Disambiguate(vm.SearchResults);
                        if (mergefirst != null)
                        {
                            Console.WriteLine("Confirm second item:");
                            vm.SearchTerm = mergeOptions.SecondSearchTerm;
                            var second = Disambiguate(vm.SearchResults.Where(x => x.ID != mergefirst.ID).ToArray());
                            if (mergefirst != null && second != null)
                            {
                                vm.Merge(mergefirst, second);
                            }
                        }
                        break;
                    #endregion

                    #region Note
                    case "note":
                        // Add a note to a task.
                        NoteSubOptions noteOptions = (NoteSubOptions)argsubs;
                        vm.SearchTerm = noteOptions.SearchTerm;
                        selected = Disambiguate(vm.SearchResults);
                        if (selected != null)
                        {
                            // Force verbose mode to display all notes and tags.
                            vm.AddNote(selected, noteOptions.NewNote);
                            verbose = true;
                            PrintItem(selected, null);
                        }
                        break;
                    #endregion

                    #region Open Tag
                    case "open-tag":
                        // Read a tag and pass it through to the "start" argverb. Intended for URLs and file names.
                        OpenTagSubOptions opentagOptions = (OpenTagSubOptions)argsubs;
                        vm.SearchTerm = opentagOptions.SearchTerm;
                        selected = Disambiguate(vm.SearchResults);
                        if (selected != null)
                        {
                            if (selected.Tags.ContainsKey(opentagOptions.Tag))
                            {
                                string tagvalue = selected.Tags[opentagOptions.Tag];
                                System.Diagnostics.Process p = new System.Diagnostics.Process();
                                p.StartInfo.FileName = tagvalue;
                                p.Start();
                                if (opentagOptions.MarkAsDone)
                                {
                                    vm.MarkDone(selected);
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
                        vm.Undefer("inbox", vm.GetTickleDueItems().ToArray());

                        var inbox = vm.GetContextItems("inbox").ToList();
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
                        var projects = vm.GetContextItems("projects").ToList();
                        for (int i = 0; i < projects.Count; i++)
                        {
                            if (vm.GetProjectChildren(projects[i]).Count() == 0)
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

                    #region Prune
                    case "prune":
                        // Delete items below a specified depth.
                        PruneSubOptions pruneOptions = (PruneSubOptions)argsubs;
                        vm.PruneBelowDepth(pruneOptions.Depth);
                        break;
                    #endregion

                    #region Rank
                    case "rank":
                        // for each context..
                        bool quitandsave = false;
                        foreach (string con in vm.GetContextNames("inbox", "done"))
                        {
                            if (quitandsave) break;
                            // select all items without rank parents
                            var items = (from i in vm.GetContextItems(con) where i.RankParent == null select i).ToList();
                            // TODO: randomise an index list
                            // show pairs of items
                            if (items.Count > 1)
                            {
                                Console.WriteLine(string.Format("{1}{1}@{0}", con, Environment.NewLine));
                            }
                            for (int x = 0; x < items.Count - 1; x += 2)
                            {
                                if (quitandsave) break;
                                // get vote
                                Console.WriteLine("{0}/{1} ({2}%) complete", x, items.Count, 100 * x / items.Count);
                                PrintItem(items[x], 1);
                                PrintItem(items[x + 1], 2);
                                Console.Write("Which of these is more important? (q=quit) ");
                                var k = Console.ReadKey();
                                // assign parents based on vote
                                switch (k.KeyChar)
                                {
                                    case '1':
                                        vm.SetParent(items[x + 1], items[x]);
                                        break;
                                    case '2':
                                        vm.SetParent(items[x], items[x + 1]);
                                        break;
                                    case 'q':
                                        Console.WriteLine();
                                        Console.WriteLine("Quitting. Save ranking so far?");
                                        Console.WriteLine("\tY: Quit and save.");
                                        Console.WriteLine("\tN: Quit without saving (all work this session will be lost, no undo).");
                                        Console.WriteLine("\tC: Cancel (default). Return to ranking.");
                                        k = Console.ReadKey();
                                        switch (k.KeyChar)
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
                        break;
                    #endregion

                    #region Rename
                    case "rename":
                        // Rename an item.
                        RenameSubOptions renameOptions = (RenameSubOptions)argsubs;
                        vm.SearchTerm = renameOptions.SearchTerm;
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
                        vm.SearchTerm = ((SearchSubOptions)argsubs).SearchTerm;
                        PrintItems(vm.SearchResults);
                        break;
                    #endregion

                    #region Set Parent
                    case "set-parent":
                        SetParentSubOptions setparentOptions = (SetParentSubOptions)argsubs;
                        Console.WriteLine("Confirm child item:");
                        vm.SearchTerm = setparentOptions.ChildSearchTerm;
                        var child = Disambiguate(vm.SearchResults);
                        Console.WriteLine("Confirm parent item:");
                        vm.SearchTerm = setparentOptions.ParentSearchTerm;
                        var parent = Disambiguate(vm.SearchResults);
                        if (child != null && parent != null)
                        {
                            vm.SetParent(child, parent);
                        }
                        break;
                    #endregion

                    #region Show
                    case "show":
                        // Display one context.
                        ShowSubOptions showOptions = (ShowSubOptions)argsubs;
                        var list = vm.GetContextItems(showOptions.Context);
                        PrintItems(list);
                        break;
                    #endregion

                    #region Someday
                    case "someday":
                        // Display the whole Someday file, 10 items at a time, and either delete or do one per listing.
                        for (int offset = 0; offset <= vm.SomedayItems.Count; offset += 10)
                        {
                            Console.Clear();
                            for (int index = 0; index < 10 && offset + index < vm.SomedayItems.Count; index++)
                            {
                                PrintItem(vm.SomedayItems.ElementAt(offset + index), index);
                            }
                            char choice = Console.ReadKey().KeyChar;
                            Console.WriteLine();
                            int dex;
                            if (Int32.TryParse(choice.ToString(), out dex))
                            {
                                selected = vm.SomedayItems.ElementAt(offset + dex);
                            }
                            Console.WriteLine("To which context should this item go?");
                            string newcontext = Console.ReadLine();
                            vm.Undefer(newcontext, selected);
                        }
                        break;
                    #endregion

                    #region Summary
                    case "summary":
                        var summaryArgs = (SummarySubOptions)argsubs;
                        var summarydata = (from c in vm.GetContextNames() select new { Context = c, Count = vm.GetContextItems(c).Count() });

                        int maxwidth = (from r in summarydata select r.Context.Length).Max();
                        int maxnum = (from c in summarydata select c.Count).Max();
                        foreach (var c in summarydata)
                        {
                            // @context         n item(s)
                            string format = string.Format("@{{0}}\t{{1,{0}}} item{1}", Math.Ceiling(Math.Log10(maxnum)), (c.Count == 1 ? "" : "s"));
                            Console.WriteLine(format, c.Context.PadRight(maxwidth), c.Count);

                            if (summaryArgs.Verbose)
                            {
                                // Show a summary of numbers at each depth.
                                var detailed = (from r in vm.GetContextItems(c.Context) group r by r.RankDepth into g select new { Depth = g.Key, Count = g.Count() });
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
                        TagSubOptions tagOptions = (TagSubOptions)argsubs;
                        vm.SearchTerm = tagOptions.SearchTerm;
                        selected = Disambiguate(vm.SearchResults);
                        if (selected != null)
                        {
                            TagItem(vm, selected);
                        }
                        break;
                    #endregion

                    #region Unrank
                    case "unrank":
                        UnrankSubOptions unrankOptions = (UnrankSubOptions)argsubs;
                        if (unrankOptions.ResetAll)
                        {
                            Console.Write("Do you really want to destroy all ranking data and start over [Y/N]?");
                            var k = Console.ReadKey();
                            if (k.KeyChar.ToString().ToLower() == "y")
                            {
                                vm.ResetPriorityParents();
                            }
                        }
                        else
                        {
                            vm.ShowHeadOnly = unrankOptions.SearchAll;
                            vm.SearchTerm = unrankOptions.SearchTerm;
                            selected = Disambiguate(vm.SearchResults);
                            if (selected != null)
                            {
                                vm.ResetPriorityParents(selected);
                            }
                        }
                        break;
                    #endregion
                }

			#region Tidy up
			// Move to the "done" file any items with a context of @done.
            if (donemapper != null)
            {
                vm.MarkDone(vm.GetContextItems("done").ToArray());
            }

            // Move any "someday" items in the main list to the someday file.
            if (somedaymapper != null)
            {
                vm.Defer(vm.GetContextItems("someday").ToArray());
            }

            // Delete any items with a context of "delete".
            vm.Delete(vm.GetContextItems("delete").ToArray());
			#endregion

			// Rewrite the files
            vm.Save();
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

        private static void PrintItems(params ActionItem[] list)
        {
            string last_context = string.Empty;
            foreach (ActionItem i in from a in list orderby a.Context, a.Title select a)
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
            if (index.HasValue)
            {
                StringBuilder title = new StringBuilder();
                title.Append(' ');
                title.Append(index);
                title.Append(':');
                title.Append(' ', Math.Max(8 - title.Length, 0));
                WrapOutput(title.ToString(), i.Title, 79);
            }
            else
            {
                WrapOutput("        ", i.Title, 79);
            }
            if (verbose)
            {
                if (i.Notes.Count > 0)
                {
                    for (int x = 0; x < i.Notes.Count; x++)
                    {
                        WrapOutput("                - ", i.Notes[x], 79);
                    }
                }
                if (i.Tags.Count > 0)
                {
                    foreach (var k in i.Tags)
                    {
                        WrapOutput(string.Format("                #{0}:", k.Key), k.Value, 79);
                    }
                }
                // TODO: ID, priority-parent and other special tags.
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

        [Obsolete]
        private static void PrintHelp(string command)
        {
            // Print usage text on the console.
            Console.WriteLine(string.Format(@"
TodoSort v{0}

usage:
TodoSort.exe [command] [args]

commands:
    add         Add a new item to the list.
    defer       Move an item to the someday file.
    delete      Delete an item without doing it.
    done        Move an item to the done file.
    note        Add a note to an item.
    open-tag    Opens (with Windows Explorer) a given tag for a given item.
                    eg 'open-tag searchterm url'.
    process     Housekeeping:
                    + Assign each inbox item to a context
                    + Ensure each project has a next action.
    prune       Defer all items at or below a given depth.
    rename      Change the name of an item.
    search      Search for matching text items.
    show        Display all items in a context.
    someday     Review the someday file, assigning 10% to an active context.
    summary     Show context names and number of items in each.
    rank        Vote on the relative importance of items to assign priorities.
    unrank      Reset ranking data for one item or all items.
    tag         Adds tags to an item.
    export      Print a Graphviz DOT language representation of one context's
                priorities, or an HTML page.
", Assembly.GetExecutingAssembly().GetName().Version));
        }

		private static ActionItem Disambiguate(ActionItem[] todolist)
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
