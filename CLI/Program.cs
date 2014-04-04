using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.CLI.Properties;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Mappers;
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
        private static bool showHeadOnly = false;

        static void Main(string[] args)
        {
			// Check settings
            if (!Settings.Default.Configured || args.Contains("reconfigure"))
            {
                foreach (string name in new string[] { "todo", "someday", "done" })
                {
                    Settings.Default[name] = ConfigurePath(Settings.Default[name].ToString(), string.Format("Configure path to '{0}' file:", name));
                    Console.WriteLine();
                }
				// Save settings.
                Settings.Default.Configured = true;
				Settings.Default.Save();
                return;
			}

            ViewModel vm = new ViewModel(new TodoTxtFileMapper(Settings.Default.Todo), new TodoTxtFileMapper(Settings.Default.Done), new TodoTxtFileMapper(Settings.Default.Someday));

            // Set universal options.
            if (args.Contains("--verbose"))
            {
                verbose = true;
            }
            if (args.Contains("--head"))
            {
                showHeadOnly = true;
            }

            #region Manipulate the file items
            if (args.Length > 0)
            {
                string command = args[0];
                // Search for a matching item in all contexts.
                ActionItem selected = null;
                switch (command)
                {
                    case "help":
                        PrintHelp(null);
                        break;
                    case "search":
                        // Search for matching items.
                        if (args.Count() < 2) { PrintHelp(command); break; }
                        vm.SearchTerm = args[1];
                        PrintItems(vm.SearchResults);
                        break;
                    case "add":
                        // Add a new item.
                        Console.WriteLine("What is the new action?");
                        string title = Console.ReadLine();
                        Console.WriteLine("What context does it belong to?");
                        string context = Console.ReadLine();
                        var item = new ActionItem(context, title);
                        vm.AddItem(item);
                        break;
                    case "delete":
                        // Find a matching item to delete.
                        if (args.Count() < 2) { PrintHelp(command); break; }
                        vm.SearchTerm = args[1];
                        selected = Disambiguate(vm.SearchResults);
                        vm.Delete(selected);
                        break;
                    case "open-tag":
                        // Read a tag and pass it through to the "start" command. Intended for URLs.
                        if (args.Count() < 3) { PrintHelp(command); break; }
                        vm.SearchTerm = args[1];
                        selected = Disambiguate(vm.SearchResults);
                        if (selected != null && selected.Tags.ContainsKey(args[2]))
                        {
                            string tagvalue = selected.Tags[args[2]];
                            System.Diagnostics.Process p = new System.Diagnostics.Process();
                            p.StartInfo.FileName = tagvalue;
                            p.Start();
                        }
                        break;
					case "show":
						// Display one context.
                        if (args.Count() < 2) { PrintHelp(command); break; }
                        var list = vm.GetContextItems(args[1]);
                        PrintItems(list);
						break;
                    case "process":
						// Go over the @someday items and look for tickle dates.
                        vm.Undefer("inbox", vm.GetTickleDueItems().ToArray());

                        var inbox = vm.GetContextItems("inbox").ToList();
                        for (int i = 0; i < inbox.Count; i++)
                        {
                            // Assign the @inbox items to contexts.
                            Console.WriteLine("To which context should this item go?");
                            ActionItem first = inbox[i];
                            Console.WriteLine(first.Title);
                            string newcontext = Console.ReadLine();
                            Console.WriteLine();
                            first.Context = newcontext;
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
                                Console.WriteLine(first.Title);
                                string nextaction = Console.ReadLine();
                                Console.WriteLine("...and to what context does it belong?");
                                string newcontext = Console.ReadLine();
                                if (nextaction == newcontext)
                                {
                                    // Wrote something like "someday"/"someday". Assume it is a new context.
                                    first.Context = newcontext;
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
                    case "defer":
                        // Move the item and its sub-items to the "someday" file.
                        if (args.Count() < 2) { PrintHelp(command); break; }
                        vm.SearchTerm = args[1];
						selected = Disambiguate(vm.SearchResults);
                        if (selected != null)
                        {
                            var tickle = ConfigureDate(DateTime.Now.AddDays(7), "When should this item be returned to the inbox? (blank for manual)");
                            if (tickle.HasValue)
                            {
                                vm.Defer(selected, tickle.Value);
                            }
                            else
                            {
                                vm.Defer(selected);
                            }
                        }
                        break;
                    case "done":
                        // If there is a next action, create a new item and add it to the correct context.
                        if (args.Count() < 2) { PrintHelp(command); break; }
                        vm.SearchTerm = args[1];
						selected = Disambiguate(vm.SearchResults);
						if (selected != null)
                        {
							vm.MarkDone(selected);
                        }
                        break;
					case "someday":
						// Display the whole Someday file, 10 items at a time, and either delete or do one per listing.
                        for (int offset = 0; offset <= vm.SomedayItems.Count; offset += 10)
						{
							Console.Clear();
                            for (int index = 0; index < 10 && offset + index < vm.SomedayItems.Count; index++)
							{
                                Console.WriteLine("{0}: {1}", index, vm.SomedayItems.ElementAt(offset + index).Title);
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
                    case "rank":
                        // for each context..
                        foreach (string con in vm.GetContextNames("inbox"))
                        {
                            // select all items without rank parents
                            var items = (from i in vm.GetContextItems(con) where i.PriorityParent == null select i).ToList();
                            // TODO: randomise an index list
                            // show pairs of items
                            if (items.Count > 1)
                            {
                                Console.WriteLine(string.Format("{1}{1}@{0}", con, Environment.NewLine));
                            }
                            for (int x = 0; x < items.Count - 1; x += 2)
                            {
                                // get vote
                                Console.WriteLine(string.Format("\t1: {0}", items[x].Title));
                                Console.WriteLine(string.Format("\t2: {0}", items[x + 1].Title));
                                Console.Write("Which of these is more important? ");
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
                                    default:
                                        break;
                                }
                                Console.WriteLine();
                            }
                        }
                        break;
                    case "unrank":
                        vm.ResetPriorityParents();
                        break;
                    case "tag":
                        // Search for a matching item.
                        if (args.Count() < 2) { PrintHelp(command); break; }
                        vm.SearchTerm = args[1];
						selected = Disambiguate(vm.SearchResults);
                        Console.WriteLine();
                        if (selected != null)
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
                                    vm.SetTag(selected, tagname, value);
                                }
                            } while (tagname.Length > 0);
                        }
                        break;
                    case "viz":
                        // Write GraphViz source.
                        if (args.Count() < 2) { PrintHelp(command); break; }
                        Console.WriteLine("digraph {");
                        foreach (var n in vm.GetContextItems(args[1]))
                        {
                            var line = n.Title.Replace("\"", "");
                            Console.WriteLine(string.Format("    ID{0} [label=\"{1}\"];", n.ID.ToString().Replace("-", ""), line));
                            if (n.PriorityParent != null)
                            {
                                Console.WriteLine(string.Format("    ID{0} -> ID{1};", n.PriorityParent.ID.ToString().Replace("-", ""), n.ID.ToString().Replace("-", "")));
                            }
                            else
                            {
                            }
                        }
                        Console.WriteLine("}");
                        break;
                    default:
                        PrintHelp(null);
                        break;
                }
            }
            #endregion

			#region Tidy up
			// Move to the "done" file any items with a context of @done.
            vm.MarkDone(vm.GetContextItems("done").ToArray());

            // Move any "someday" items in the main list to the someday file.
            vm.Defer(vm.GetContextItems("someday").ToArray());

            // Delete any items with a context of "delete".
            vm.Delete(vm.GetContextItems("delete").ToArray());
			#endregion

			// Rewrite the files
            vm.Save();
        }

        private static DateTime? ConfigureDate(DateTime preset, string prompt)
        {
            Console.WriteLine(prompt);
            Console.WriteLine("Type correct value or [Enter] to accept default (blank for null).");
            Console.WriteLine(preset.ToString("yyyy-MM-dd"));
            var response = Console.ReadLine();
            if (response.Trim().Length > 0)
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
                    return null;
                }
            }
            Console.WriteLine();
            return null;
        }

        private static void PrintItems(IEnumerable<ActionItem> list)
        {
            if (showHeadOnly)
            {
                list = (from i in list where i.PriorityParent == null select i);
            }
            string last_context = string.Empty;
            foreach (ActionItem i in from a in list orderby a.Context, a.Title select a)
            {
                if (i.Context != last_context)
                {
                    Console.WriteLine(string.Format("@{0}", i.Context));
                }
                Console.WriteLine(string.Format("\t{0}", i.Title));
                if (verbose)
                {
                    if (i.Notes.Count > 0)
                    {
                        Console.WriteLine(string.Format("\t\t{0}", string.Join("\n\t\t- ", i.Notes)));
                    }
                    if (i.Tags.Count > 0)
                    {
                        Console.WriteLine(string.Format("\t\t{0}", string.Join("\n\t\t", from t in i.Tags select string.Format("#{0}:{1}", t.Key, t.Value))));
                    }
                }
                last_context = i.Context;
            }
        }

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
    open-tag    Opens (with Windows Explorer) a given tag for a given item.
                    eg 'open-tag searchterm url'.
    process     Housekeeping:
                    + Assign inbox items to a context
                    + Move done items to the done file
                    + Ensure each project has a next action.
    search      Search for matching text items.
    show        Display all items in a context.
    someday     Review the someday file, assigning 10% to an active context.
    rank        Vote on the relative importance of items to assign priorities.
    unrank      Reset all ranking data.
    tag         Adds a tag to an item.
    viz         Print a Graphviz DOT language representation of one context's priorities.
", Assembly.GetExecutingAssembly().GetName().Version));
        }

		private static ActionItem Disambiguate(List<ActionItem> todolist)
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
                    Console.WriteLine("{0}: {1}", i, todolist.ElementAt(i).Title);
				}
				char choice = Console.ReadKey().KeyChar;
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
        public static string ConfigurePath(string path, string prompt)
        {
            // Special folder replacements.
            path = path.Replace("{MyDocs}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            path = path.Replace("{MyPictures}", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            path = path.Replace("{MachineName}", Environment.MachineName);

            Console.WriteLine("Configure path to {0}:", prompt);
            Console.WriteLine("Type correct value or [Enter] to accept default.");
            Console.WriteLine(Path.GetFullPath(path));
            var response = Console.ReadLine();
            if (response.Trim().Length > 0)
            {
                path = response;
                Console.WriteLine();
            }
            return path;
        }
    }
}
