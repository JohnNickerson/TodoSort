using AssimilationSoftware.Maroon.Model;
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
    }
}
