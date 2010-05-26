using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TodoSort
{
    class Item
    {
        public string Text;
        public string Context;
        public List<string> SubItems = new List<string>();
    }
}
