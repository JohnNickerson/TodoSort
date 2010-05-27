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
            foreach (string filename in new string[] {
                ConfigurationManager.AppSettings["todo"]
            })
            {
                #region Deserialise the file
                string[] items = File.ReadAllLines(filename);

                List<Item> allitems = new List<Item>();
                string context = string.Empty;
                Item curitem = new Item();
                curitem.Text = "(item out of order)";
                for (int x = 0; x < items.Length; x++)
                {
                    if (items[x].StartsWith("@"))
                    {
                        // New context.
                        context = items[x];
                    }
                    else if (items[x].StartsWith("\t\t"))
                    {
                        // Sub-item.
                        curitem.SubItems.Add(items[x]);
                    }
                    else
                    {
                        // New item.
                        curitem = new Item();
                        curitem.Text = items[x];
                        curitem.Context = context;
                        allitems.Add(curitem);
                    }
                }
                #endregion

                #region Manipulate the file items
                if (args.Length > 0)
                {
                    string command = args[0];
                    // Search for a matching item in all contexts.
                    Item selected = null;
                    if (args.Length > 1)
                    {
                        var matches = from i in allitems
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
                            for (int i = 0; i < allitems.Count; i++)
                            {
                                if (allitems[i].Context == "@inbox")
                                {
                                    Console.WriteLine("To which context should this item go?");
                                    Item first = allitems[i];
                                    Console.WriteLine(first.Text);
                                    string newcontext = Console.ReadLine();
                                    if (!newcontext.StartsWith("@"))
                                    {
                                        newcontext = "@" + newcontext;
                                    }
                                    Console.WriteLine();
                                    if (newcontext.Equals("someday"))
                                    {
                                        Defer(allitems, first);
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
                            Defer(allitems, selected);
                            break;
                        case "done":
                            // If there is a next action, create a new item and add it to the correct context.
                            if (selected != null)
                            {
                                if (selected.SubItems.Count > 0 && selected.SubItems[0].Trim().StartsWith("&@"))
                                {
                                    Item next = new Item();
                                    next.SubItems = selected.SubItems;
                                    string newcontext = next.SubItems[0].Split(' ')[0].Trim().Remove(0, 1);
                                    next.Text = next.SubItems[0].Remove(1, 2 + newcontext.Length + 1);
                                    next.SubItems.RemoveAt(0);
                                    allitems.Add(next);
                                }
                                // Log the completed action to the Done file.
                                WriteToFile("done", string.Format("{0}: {1}", DateTime.Now, selected.Text));
                                allitems.Remove(selected);
                            }
                            break;
                        default:
                            break;
                    }
                }
                #endregion

                #region Rewrite the file
                File.Delete(filename);
                foreach (string b in (from s in allitems orderby s.Context select s.Context).Distinct())
                {
                    if (!b.Equals("@inbox"))
                    {
                        // Write out block.
                        File.AppendAllText(filename, b);
                        File.AppendAllText(filename, Environment.NewLine);
                        foreach (Item i in from t in allitems orderby t.Text where t.Context == b select t)
                        {
                            File.AppendAllText(filename, i.Text);
                            File.AppendAllText(filename, Environment.NewLine);
                            foreach (string sub in i.SubItems)
                            {
                                File.AppendAllText(filename, sub);
                                File.AppendAllText(filename, Environment.NewLine);
                            }
                        }
                    }
                }
                // Write out inbox.
                File.AppendAllText(filename, "@inbox");
                File.AppendAllText(filename, Environment.NewLine);
                foreach (Item i in from t in allitems orderby t.Text where t.Context == "@inbox" select t)
                {
                    File.AppendAllText(filename, i.Text);
                    File.AppendAllText(filename, Environment.NewLine);
                    // Sub-items not supported for inbox.
                }
                #endregion
            }
        }

        private static void Defer(List<Item> allblocks, Item selected)
        {
            WriteToFile("someday", selected.Text);
            foreach (string s in selected.SubItems)
            {
                WriteToFile("someday", s);
            }
            allblocks.Remove(selected);
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
