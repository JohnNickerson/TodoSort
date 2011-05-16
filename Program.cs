using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using TodoSort.Properties;
using AssimilationSoftware.PimData;
using AssimilationSoftware.PimData.Mappers;

namespace TodoSort
{
    class Program
    {
        static void Main(string[] args)
        {
			// Check settings
            if (Settings.Default.Reconfigure || args[0] == "reconfigure")
            {
                foreach (string name in new string[] { "todo", "someday", "done" })
                {
                    // Replace with path to My Documents.
                    Settings.Default[name] = Settings.Default[name].ToString().Replace("{MyDocs}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                    // Confirm file path.
                    Console.WriteLine("Configure path to '{0}' file:", name);
                    Console.WriteLine(Settings.Default[name]);
                    Console.WriteLine("Is this correct?");
                    var response = Console.ReadKey();
                    if (!response.KeyChar.ToString().ToLower().Equals("y"))
                    {
                        Console.WriteLine("Please provide the correct full path:");
                        Settings.Default[name] = Console.ReadLine();
                    }
                    Console.WriteLine();
                }
				// Save settings.
                Settings.Default.Reconfigure = false;
				Settings.Default.Save();
                return;
			}

            // Deserialise the files
            List<ListItem> todolist = new ListItemDiskMapper().Deserialise(Settings.Default.Todo);
            List<ListItem> someday = new ListItemDiskMapper().Deserialise(Settings.Default.Someday);
			// Track whether changes have been made to the "someday" file, to avoid rewriting it if possible.
			bool someday_changes = false;

            #region Manipulate the file items
            if (args.Length > 0)
            {
                string command = args[0];
                // Search for a matching item in all contexts.
                ListItem selected = null;
                switch (command)
                {
					case "show":
						// Display one context.
						Console.WriteLine("Showing context @{0}", args[1]);
						foreach (ListItem i in from m in todolist where m.Context.EndsWith(args[1]) select m)
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
                                ListItem first = todolist[i];
                                Console.WriteLine(first.Title);
                                string newcontext = Console.ReadLine();
                                Console.WriteLine();
                                first.Context = newcontext;
                            }
                            // Add next actions for projects.
                            else if (todolist[i].Context == "@projects")
                            {
                                Console.WriteLine("What is the next action required on this project?");
                                ListItem first = todolist[i];
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
                                    first.SubItems.Insert(0, string.Format("&@projects {0}", first.Title));
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
				ListItem i = todolist[x];
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
			foreach (ListItem i in someday)
			{
                if (i.Context != "@someday")
                {
                    i.Context = "@someday";
                    someday_changes = true;
                }
			}
			#endregion

			// Rewrite the files
            ListItemDiskMapper mapper = new ListItemDiskMapper();
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
		private static void MarkDone(List<ListItem> todolist, ListItem doneitem)
		{
            ListItemDiskMapper mapper = new ListItemDiskMapper();
            List<ListItem> donelist = mapper.Deserialise(Settings.Default.Done);
            doneitem.Done(todolist, donelist);
            mapper.Serialise(Settings.Default.Done, donelist);
		}

		private static ListItem Disambiguate(string search, List<ListItem> todolist)
		{
			ListItem selected = null;
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

        private static void Defer(List<ListItem> from, List<ListItem> to, ListItem selected)
        {
            to.Add(selected);
            from.Remove(selected);
        }
    }
}
