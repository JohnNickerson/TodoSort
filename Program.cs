using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using TodoSort.Properties;

namespace TodoSort
{
    class Program
    {
        static void Main(string[] args)
        {
			// Check settings
			bool reconfig = false;
			foreach (string name in new string[] { "todo", "someday", "done" })
			{
				if (Settings.Default[name].ToString().Contains("{MyDocs}"))
				{
					reconfig = true;
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
			}
			if (reconfig)
			{
				// Save settings.
				Settings.Default.Save();
			}

            // Deserialise the files
            List<Item> todolist = Item.ReadFile(Settings.Default.Todo);
            List<Item> someday = Item.ReadFile(Settings.Default.Someday);
			// Track whether changes have been made to the "someday" file, to avoid rewriting it if possible.
			bool someday_changes = false;

            #region Manipulate the file items
            if (args.Length > 0)
            {
                string command = args[0];
                // Search for a matching item in all contexts.
                Item selected = null;
                switch (command)
                {
					case "show":
						// Display one context.
						Console.WriteLine("Showing context @{0}", args[1]);
						foreach (Item i in from m in todolist where m.Context.EndsWith(args[1]) select m)
						{
							Console.WriteLine(i.Text);
						}
						break;
                    case "process":
                        // Go over the @inbox items and assign them to contexts.
						for (int i = 0; i < todolist.Count; i++)
                        {
                            if (todolist[i].Context == "@inbox")
                            {
                                Console.WriteLine("To which context should this item go?");
                                Item first = todolist[i];
                                Console.WriteLine(first.Text);
                                string newcontext = Console.ReadLine();
                                if (!newcontext.StartsWith("@"))
                                {
                                    newcontext = "@" + newcontext;
                                }
                                Console.WriteLine();
                                if (newcontext.Equals("someday"))
                                {
                                    Defer(todolist, someday, first);
									someday_changes = true;
                                }
                                else
                                {
                                    first.Context = newcontext;
                                    if (!first.Text.StartsWith("\t"))
                                    {
                                        first.Text = "\t" + first.Text;
                                    }
                                }
                            }
                        }
                        break;
                    case "defer":
                        // Move the doneitem item and its sub-items to the "someday" file.
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
                    default:
                        break;
                }
            }
            #endregion

			#region Tidy up
			for (int x = 0; x < todolist.Count;)
			{
				Item i = todolist[x];
				if (i.Context.Equals("@defer") || i.Context.Equals("@someday"))
				{
					// Move to the someday file any items with a context of "defer" or "someday".
					Defer(todolist, someday, i);
					someday_changes = true;
				}
				else if (i.Context.Equals("@done"))
				{
					// Move to the "done" file any items with a context of @done.
					MarkDone(todolist, i);
				}
				else
				{
					x++;
				}
			}

			// Change all items in the someday file to have "someday" as a context.
			foreach (Item i in someday)
			{
				i.Context = "@someday";
			}
			#endregion

			// Rewrite the files
            Item.WriteToFile(Settings.Default.Todo, todolist, true);
			if (someday_changes)
			{
				// Sort the Someday file.
				Item.WriteToFile(Settings.Default.Someday, someday, false);
			}
        }

		/// <summary>
		/// Mark an item as done.
		/// </summary>
		/// <param name="todolist">The list in which the item is found.</param>
		/// <param name="doneitem">The item to mark as done.</param>
		private static void MarkDone(List<Item> todolist, Item doneitem)
		{
			if (doneitem.SubItems.Count > 0 && doneitem.SubItems[0].Trim().StartsWith("&@"))
			{
				Item next = new Item(string.Empty, string.Empty);
				next.SubItems = doneitem.SubItems;
				string newcontext = next.SubItems[0].Split(' ')[0].Trim().Remove(0, 1);
				next.Context = newcontext;
				next.Text = next.SubItems[0].Remove(1, 2 + newcontext.Length + 1);
				next.SubItems.RemoveAt(0);
				todolist.Add(next);
			}
			// Log the completed action to the Done file.
			WriteToFile("done", string.Format("{0}: {1}", DateTime.Now, doneitem.Text));
			todolist.Remove(doneitem);
		}

		private static Item Disambiguate(string search, List<Item> todolist)
		{
			Item selected = null;
			var matches = from i in todolist
						  where i.Text.ToLower().Contains(search.ToLower())
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
					Console.WriteLine("{0}: {1}", i, matches.ElementAt(i).Text);
				}
				char choice = Console.ReadKey().KeyChar;
				int dex = Int32.Parse(choice.ToString());
				selected = matches.ElementAt(dex);
			}
			return selected;
		}

        private static void Defer(List<Item> from, List<Item> to, Item selected)
        {
            to.Add(selected);
            from.Remove(selected);
        }

        /// <summary>
        /// Writes an output string to a file, identified by configuration value.
        /// </summary>
        /// <param name="filetag">The configuration setting name for the file.</param>
        /// <param name="output">The string to write out.</param>
        private static void WriteToFile(string filetag, string output)
        {
            File.AppendAllText(Settings.Default[filetag].ToString(), output);
            File.AppendAllText(Settings.Default[filetag].ToString(), Environment.NewLine);
        }
    }
}
