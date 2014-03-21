using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using TodoSort.Properties;
using AssimilationSoftware.PimData;
using AssimilationSoftware.PimData.Mappers;
using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using System.Reflection;

namespace AssimilationSoftware.TodoSort
{
    class Program
    {
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

            #region Manipulate the file items
            if (args.Length > 0)
            {
                string command = args[0];
                // Search for a matching item in all contexts.
                ActionItem selected = null;
                switch (command)
                {
                    case "help":
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
    process     Housekeeping:
                    + Assign inbox items to a context
                    + Move done items to the done file
                    + Ensure each project has a next action.
    search      Search for matching text items.
    show        Display all items in a context.
    someday     Review the someday file, assigning 10% to an active context.
", Assembly.GetExecutingAssembly().GetName().Version));
                        break;
                    case "search":
                        // Search for matching items.
                        var results = vm.Search(args[1]);
                        string last_context = string.Empty;
                        foreach (ActionItem i in from a in results orderby a.Context, a.Title select a)
                        {
                            if (i.Context == last_context)
                            {
                                Console.WriteLine(string.Format("\t{0}", i.Title));
                            }
                            else
                            {
                                Console.WriteLine(string.Format("@{0}\n\t{1}", i.Context, i.Title));
                                last_context = i.Context;
                            }
                        }
                        break;
                    case "add":
                        // Add a new item.
                        string title = Console.ReadLine();
                        string context = Console.ReadLine();
                        var item = new ActionItem(context, title);
                        vm.AddItem(item);
                        break;
                    case "delete":
                        // Find a matching item to delete.
                        selected = Disambiguate(vm.Search(args[1]));
                        vm.Delete(selected);
                        break;
					case "show":
						// Display one context.
						Console.WriteLine("@{0}", args[1]);
						foreach (ActionItem i in vm.GetContext(args[1]))
						{
							Console.WriteLine(string.Format("\t{0}", i.Title));
						}
						break;
                    case "process":
						// Go over the @someday items and look for tickle dates.
						for (int i = 0; i < vm.SomedayItems.Count; i++)
						{
                            if (vm.SomedayItems[i].TickleDate.HasValue && vm.SomedayItems[i].TickleDate.Value <= DateTime.Now)
                            {
                                vm.SomedayItems[i].Context = "@inbox";
                                vm.SomedayItems[i].TickleDate = null;
                                vm.Undefer(vm.SomedayItems[i]);
                            }
						}
                        var inbox = vm.GetContext("inbox").ToList();
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
                        var projects = vm.GetContext("projects").ToList();
                        for (int i = 0; i < projects.Count; i++)
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
                        break;
                    case "defer":
                        // Move the item and its sub-items to the "someday" file.
						selected = Disambiguate(vm.Search(args[1]));
						vm.Defer(selected);
                        break;
                    case "done":
                        // If there is a next action, create a new item and add it to the correct context.
						selected = Disambiguate(vm.Search(args[1]));
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
							int dex;
							if (Int32.TryParse(choice.ToString(), out dex))
							{
								selected = vm.SomedayItems.ElementAt(offset + dex);
							}
							Console.WriteLine("To which context should this item go?");
							string newcontext = Console.ReadLine();
							selected.Context = newcontext;
							vm.Undefer(selected);
						}
						break;
                    default:
                        break;
                }
            }
            #endregion

			#region Tidy up
            while (vm.GetContext("done").Count() > 0)
            {
				// Move to the "done" file any items with a context of @done.
                vm.MarkDone(vm.GetContext("done").ElementAt(0));
            }
            while (vm.GetContext("someday").Count() > 0)
            {
                // Move any "someday" items in the main list to the someday file.
                vm.Defer(vm.GetContext("someday").ElementAt(0));
            }
			#endregion

			// Rewrite the files
            vm.Save();
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
            Console.WriteLine(path);
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
