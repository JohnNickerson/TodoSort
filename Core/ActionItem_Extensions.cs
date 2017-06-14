using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core
{
    public static class ActionItem_Extensions
    {
        public static int GetIntTag(this ActionItem item, string tagname, int fallback)
        {
            if (item == null)
            {
                return fallback;
            }
            else if (item.Tags.ContainsKey(tagname))
            {
                int val = fallback;
                var success = int.TryParse(item.Tags[tagname], out val);
                if (success)
                {
                    return val;
                }
                else
                {
                    return fallback;
                }
            }
            return fallback;
        }

        public static int Depth(this ActionItem item)
        {
            int deep = 0;
            var chain = new List<ActionItem>();
            chain.Add(item);
            var parent = item.RankParent;
            while (parent != null && !chain.Contains(parent))
            {
                deep++;
                parent = parent.RankParent;
                chain.Add(parent);
            }
            return deep;
        }
    }
}
