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

namespace TodoSort
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

            // Deserialise the files
            ActionItemDiskMapper mapper = new ActionItemDiskMapper();
            List<ActionItem> todolist = (File.Exists(Settings.Default.Todo) ? mapper.Deserialise(Settings.Default.Todo) : new List<ActionItem>());
            List<ActionItem> someday = (File.Exists(Settings.Default.Someday) ? mapper.Deserialise(Settings.Default.Someday) : new List<ActionItem>());
			// Track whether changes have been made to the "someday" file, to avoid rewriting it if possible.
			bool someday_changes = false;

            #region Manipulate the file items
            if (args.Length > 0)
            {
                string command = args[0];
                // Search for a matching item in all contexts.
                ActionItem selected = null;
                switch (command)
                {
					case "show":
						// Display one context.
						Console.WriteLine("Showing context @{0}", args[1]);
						foreach (ActionItem i in from m in todolist where m.Context.EndsWith(args[1]) select m)
						{
							Console.WriteLine(i.Title);
						}
						break;
                    case "process":
						// Go over the @someday items and look for tickle dates.
						for (int i = 0; i < someday.Count; i++)
						{
                            if (someday[i].TickleDate.HasValue && someday[i].TickleDate.Value <= DateTime.Now)
                            {
                                someday[i].Context = "@inbox";
                                someday[i].TickleDate = null;
                                Defer(someday, todolist, someday[i]);
                                someday_changes = true;
                            }
						}
                        for (int i = 0; i < todolist.Count; i++)
                        {
                            // Assign the @inbox items to contexts.
                            if (todolist[i].Context == "@inbox")
                            {
                                Console.WriteLine("To which context should this item go?");
                                ActionItem first = todolist[i];
                                Console.WriteLine(first.Title);
                                string newcontext = Console.ReadLine();
                                Console.WriteLine();
                                first.Context = newcontext;
                            }
                            // Add next actions for projects.
                            else if (todolist[i].Context == "@projects")
                            {
                                Console.WriteLine("What is the next action required on this project?");
                                ActionItem first = todolist[i];
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
                                    first.SubTasks.Insert(0, new ActionItem(first.Context, string.Format("&@projects {0}", first.Title)));
                                    first.Title = nextaction;
                                    first.Context = newcontext;
                                }
                                Console.WriteLine();
                            }
                        }
                        break;
                    case "defer":
                        // Move the item and its sub-items to the "someday" file.
						selected = Disambiguate(args[1], todolist);
						Defer(todolist, someday, selected);
						someday_changes = true;
                        break;
                    case "done":
                        // If there is a next action, create a new item and add it to the correct context.
						selected = Disambiguate(args[1], todolist);
						if (selected != null)
                        {
							MarkDone(todolist, selected);
                        }
                        break;
					case "someday":
						// Display the whole Someday file, 10 items at a time, and either delete or do one per listing.
						for (int offset = 0; offset <= someday.Count; offset += 9)
						{
							Console.Clear();
							for (int index = 0; index < 10 && offset + index < someday.Count; index++)
							{
								Console.WriteLine("{0}: {1}", index, someday[offset + index].Title);
							}
							char choice = Console.ReadKey().KeyChar;
							int dex;
							if (Int32.TryParse(choice.ToString(), out dex))
							{
								selected = someday[offset + dex];
							}
							Console.WriteLine("To which context should this item go?");
							string newcontext = Console.ReadLine();
							selected.Context = newcontext;
							Defer(someday, todolist, selected);
							someday_changes = true;
						}
						break;
                    default:
                        break;
                }
            }
            #endregion

			#region Tidy up
			for (int x = 0; x < todolist.Count;)
			{
				ActionItem i = todolist[x];
				if (i.Context.Equals("@done"))
				{
					// Move to the "done" file any items with a context of @done.
					MarkDone(todolist, i);
				}
                else if (i.Context.Equals("@defer") || i.Context.Equals("@someday") || (i.TickleDate.HasValue && i.TickleDate.Value > DateTime.Now))
				{
					// Move to the someday file any items with a context of "defer" or "someday".
					Defer(todolist, someday, i);
					someday_changes = true;
				}
				else
				{
					x++;
				}
			}

			// Change all items in the someday file to have "someday" as a context.
			foreach (ActionItem i in someday)
			{
                if (i.Context != "@someday")
                {
                    i.Context = "@someday";
                    someday_changes = true;
                }
			}
			#endregion

			// Rewrite the files
            mapper.Serialise(Settings.Default.Todo, todolist);
			if (someday_changes)
			{
                mapper.Serialise(Settings.Default.Someday, someday);
			}
        }

		/// <summary>
		/// Mark an item as done.
		/// </summary>
		/// <param name="todolist">The list in which the item is found.</param>
		/// <param name="doneitem">The item to mark as done.</param>
		private static void MarkDone(List<ActionItem> todolist, ActionItem doneitem)
		{
            IActionItemMapper mapper = new ActionItemDiskMapper();
            List<ActionItem> donelist = (File.Exists(Settings.Default.Done) ? mapper.Deserialise(Settings.Default.Done) : new List<ActionItem>());
            doneitem.Done(todolist, donelist);
            mapper.Serialise(Settings.Default.Done, donelist);
		}

		private static ActionItem Disambiguate(string search, List<ActionItem> todolist)
		{
			ActionItem selected = null;
			var matches = from i in todolist
						  where i.Title.ToLower().Contains(search.ToLower())
						  select i;
			// Disambiguate or verify search results.
			if (matches.Count() == 0)
			{
				Console.WriteLine("No search matches. No action will be taken.");
			}
			else if (matches.Count() > 5)
			{
				Console.WriteLine("Too many search matches. Try to be more specific. No action will be taken this time.");
			}
			else
			{
				for (int i = 0; i < matches.Count(); i++)
				{
					Console.WriteLine("{0}: {1}", i, matches.ElementAt(i).Title);
				}
				char choice = Console.ReadKey().KeyChar;
				int dex;
                if (Int32.TryParse(choice.ToString(), out dex))
                {
                    if (matches.Count() > dex)
                    {
                        selected = matches.ElementAt(dex);
                    }
                }
			}
			return selected;
		}

        private static void Defer(List<ActionItem> from, List<ActionItem> to, ActionItem selected)
        {
            to.Add(selected);
            from.Remove(selected);
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
