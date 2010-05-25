using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TodoSort
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (string filename in new string[] {
                @"C:\Users\jnickerson.SNOWDEN\Documents\My Dropbox\Thoughts\todo.txt",
                @"C:\Users\jnickerson.SNOWDEN\Documents\My Dropbox\Actions\someday.txt"
            })
            {

                string[] items = File.ReadAllLines(filename);

                List<Block> allblocks = new List<Block>();
                Item curitem = new Item();
                curitem.Text = "(item out of order)";
                for (int x = 0; x < items.Length; x++)
                {
                    if (items[x].StartsWith("@"))
                    {
                        // New context.
                        allblocks.Add(new Block());
                        allblocks[allblocks.Count - 1].Title = items[x];
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
                        allblocks[allblocks.Count - 1].Items.Add(curitem);
                    }
                }

                File.Delete(filename);
                Block inbox = new Block();
                foreach (Block b in from s in allblocks orderby s.Title select s)
                {
                    if (b.Title.Equals("@inbox"))
                    {
                        inbox = b;
                    }
                    else
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
            }
        }
    }
}
