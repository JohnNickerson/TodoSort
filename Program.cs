using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;

namespace TodoSort
{
    class Program
    {
        static void Main(string[] args)
        {
            // Deserialise the files
            List<Item> todolist = Item.ReadFile(ConfigurationManager.AppSettings["todo"]);
            List<Item> someday = Item.ReadFile(ConfigurationManager.AppSettings["someday"]);
			// Track whether changes have been made to the "someday" file, to avoid rewriting it if possible.
			bool someday_changes = false;

            #region Manipulate the file items
            if (args.Length > 0)
            {
                string command = args[0];
                // Search for a matching item in all contexts.
                Item selected = null;
                if (args.Length > 1)
                {
                    var matches = from i in todolist
                                  where i.Text.ToLower().Contains(args[1].ToLower())
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
                }
                switch (command)
                {
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
                        // Move the selected item and its sub-items to the "someday" file.
                        Defer(todolist, someday, selected);
						someday_changes = true;
                        break;
                    case "done":
                        // If there is a next action, create a new item and add it to the correct context.
                        if (selected != null)
                        {
                            if (selected.SubItems.Count > 0 && selected.SubItems[0].Trim().StartsWith("&@"))
                            {
                                Item next = new Item(string.Empty, string.Empty);
                                next.SubItems = selected.SubItems;
                                string newcontext = next.SubItems[0].Split(' ')[0].Trim().Remove(0, 1);
                                next.Context = newcontext;
                                next.Text = next.SubItems[0].Remove(1, 2 + newcontext.Length + 1);
                                next.SubItems.RemoveAt(0);
                                todolist.Add(next);
                            }
                            // Log the completed action to the Done file.
                            WriteToFile("done", string.Format("{0}: {1}", DateTime.Now, selected.Text));
                            todolist.Remove(selected);
                        }
                        break;
                    default:
                        break;
                }
            }
            #endregion

			#region Tidy up
			// Move to the someday file any items with a context of "defer" or "someday".
			for (int x = 0; x < todolist.Count;)
			{
				Item i = todolist[x];
				if (i.Context.Equals("@defer") || i.Context.Equals("@someday"))
				{
					Defer(todolist, someday, i);
					someday_changes = true;
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
            Item.WriteToFile(ConfigurationManager.AppSettings["todo"], todolist, true);
			if (someday_changes)
			{
				// Sort the Someday file.
				Item.WriteToFile(ConfigurationManager.AppSettings["someday"], someday, false);
			}
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
            File.AppendAllText(ConfigurationManager.AppSettings[filetag], output);
            File.AppendAllText(ConfigurationManager.AppSettings[filetag], Environment.NewLine);
        }
    }
}
