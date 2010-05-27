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

            #region Manipulate the file items
            if (args.Length > 0)
            {
                string command = args[0];
                // Search for a matching item in all contexts.
                Item selected = null;
                if (args.Length > 1)
                {
                    var matches = from i in todolist
                                  where i.Text.Contains(args[1])
                                  select i;
                    // Disambiguate if required.
                    if (matches.Count() == 1)
                    {
                        selected = matches.First();
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
           

            // Rewrite the files
            Item.WriteToFile(ConfigurationManager.AppSettings["todo"], todolist, true);
            // Sort the Someday file.
            Item.WriteToFile(ConfigurationManager.AppSettings["someday"], someday, false);
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
