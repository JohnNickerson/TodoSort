using AssimilationSoftware.Maroon.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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

        public static string GenerateHash(this ActionItem item)
        {
            try
            {
                using (var cryptoProvider = new SHA1CryptoServiceProvider())
                {
                    var csv = $"@{item.Context},{item.Title},{string.Join("-", item.Notes)},{string.Join("#", item.Tags.Keys)},{string.Join("#", item.Tags.Values)},{item.Upvotes},{item.DoneDate},{item.ProjectId},{item.ParentId}";
                    var hash = cryptoProvider.ComputeHash(Encoding.UTF8.GetBytes(csv));
                    return BitConverter.ToString(hash);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }
    }
}
