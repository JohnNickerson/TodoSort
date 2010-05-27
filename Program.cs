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

                Dictionary<string, Block> allblocks = new Dictionary<string, Block>();
                string context = string.Empty;
                Item curitem = new Item();
                curitem.Text = "(item out of order)";
                for (int x = 0; x < items.Length; x++)
                {
                    if (items[x].StartsWith("@"))
                    {
                        // New context.
                        context = items[x];
                        allblocks.Add(context, new Block());
                        allblocks[context].Title = items[x];
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
                        allblocks[context].Items.Add(curitem);
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
                        var matches = from c in allblocks.Values
                                      from i in c.Items
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
                            while (allblocks["@inbox"].Items.Count > 0)
                            {
                                Console.WriteLine("To which context should this item go?");
                                Item first = allblocks["@inbox"].Items[0];
                                Console.WriteLine(first.Text);
                                string newcontext = Console.ReadLine();
                                if (!newcontext.StartsWith("@"))
                                {
                                    newcontext = "@" + newcontext;
                                }
                                Console.WriteLine();
                                allblocks["@inbox"].Items.Remove(first);
                                if (newcontext.Equals("someday"))
                                {
                                    Defer(allblocks, first);
                                }
                                else
                                {
                                    if (!allblocks.ContainsKey(newcontext))
                                    {
                                        allblocks[newcontext] = new Block();
                                        allblocks[newcontext].Title = newcontext;
                                    }
                                    allblocks[newcontext].Items.Add(first);
                                    first.Context = newcontext;
                                    if (!first.Text.StartsWith("\t"))
                                    {
                                        first.Text = "\t" + first.Text;
                                    }
                                }
                            }
                            break;
                        case "defer":
                            // Move the selected item and its sub-items to the "someday" file.
                            Defer(allblocks, selected);
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
                                    allblocks[newcontext].Items.Add(next);
                                }
                                // Log the completed action to the Done file.
                                WriteToFile("done", string.Format("{0}: {1}", DateTime.Now, selected.Text));
                                allblocks[selected.Context].Items.Remove(selected);
                            }
                            break;
                        default:
                            break;
                    }
                }
                #endregion

                #region Rewrite the file
                File.Delete(filename);
                Block inbox = new Block();
                foreach (Block b in from s in allblocks.Values orderby s.Title select s)
                {
                    if (b.Title.Equals("@inbox"))
                    {
                        inbox = b;
                    }
                    else if (b.Items.Count > 0) // If a context is empty, remove it.
                    {
                        // Write out block.
                        File.AppendAllText(filename, b.Title);
                        File.AppendAllText(filename, Environment.NewLine);
                        foreach (Item i in from t in b.Items orderby t.Text select t)
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
                if (inbox.Title != null)
                {
                    // Write out inbox.
                    File.AppendAllText(filename, inbox.Title);
                    File.AppendAllText(filename, Environment.NewLine);
                    foreach (Item i in inbox.Items)
                    {
                        File.AppendAllText(filename, i.Text);
                        File.AppendAllText(filename, Environment.NewLine);
                        // Sub-items not supported for inbox.
                    }
                }
                #endregion
            }
        }

        private static void Defer(Dictionary<string, Block> allblocks, Item selected)
        {
            WriteToFile("someday", selected.Text);
            foreach (string s in selected.SubItems)
            {
                WriteToFile("someday", s);
            }
            allblocks[selected.Context].Items.Remove(selected);
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
