using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TodoSort
{
    class Item
    {
        #region Fields
        public string Text;
        public string Context;
        public List<string> SubItems;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new Item with a given context, title and optional sub-items.
        /// </summary>
        /// <param name="context">The context to which this item belongs.</param>
        /// <param name="title">The text of the action item.</param>
        /// <param name="subitems">Optional list of sub-items to go under this one.</param>
        public Item(string context, string title, params string[] subitems)
        {
            this.Context = context;
            this.Text = title;
            this.SubItems = new List<string>(subitems);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Reads a formatted file and returns it as a list of Items.
        /// </summary>
        /// <param name="filename">The full path to the file to load.</param>
        /// <returns>A list of items as represented by the file.</returns>
        public static List<Item> ReadFile(string filename)
        {
            string[] items = File.ReadAllLines(filename);

            List<Item> allitems = new List<Item>();
            string context = string.Empty;
            Item curitem = new Item(context, "(item out of order)");
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
                    curitem = new Item(context, items[x]);
                    allitems.Add(curitem);
                }
            }

            return allitems;
        }
        /// <summary>
        /// Writes a list of items out to a file.
        /// </summary>
        /// <param name="filename">The full path of the file to write.</param>
        /// <param name="items">The items to write out.</param>
        /// <param name="includeinbox">True to add an "@inbox" context at the end, false to leave it out unless already present.</param>
        public static void WriteToFile(string filename, List<Item> items, bool includeinbox)
        {
            File.Delete(filename);
            foreach (string b in (from s in items orderby s.Context select s.Context).Distinct())
            {
                if (!b.Equals("@inbox"))
                {
                    // Write out block.
                    File.AppendAllText(filename, b);
                    File.AppendAllText(filename, Environment.NewLine);
                    foreach (Item i in from t in items orderby t.Text where t.Context == b select t)
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
            if (includeinbox)
            {
                // Write out inbox.
                File.AppendAllText(filename, "@inbox");
                File.AppendAllText(filename, Environment.NewLine);
                foreach (Item i in from t in items orderby t.Text where t.Context == "@inbox" select t)
                {
                    File.AppendAllText(filename, i.Text);
                    File.AppendAllText(filename, Environment.NewLine);
                    // Sub-items not supported for inbox.
                }
            }
        }
        #endregion
    }
}
