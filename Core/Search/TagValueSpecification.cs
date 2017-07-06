using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class TagValueSpecification : ISearchSpecification<ActionItem>
    {
        private string _tagname;
        private string _tagvalue;

        public TagValueSpecification(string tagname, string tagvalue)
        {
            _tagname = tagname;
            _tagvalue = tagvalue;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            // Case-insensitive search. It's not very straightforward.
            // TODO: Start with a case-insensitive dictionary. Needs modification of PimData.
            var caseless = b.Tags;
            try
            {
                caseless = new Dictionary<string, string>(b.Tags, StringComparer.CurrentCultureIgnoreCase);
            }
            catch
            {
                // Tag case collisions. Just use the original.
            }
            if (string.IsNullOrEmpty(_tagvalue))
            {
                if (string.IsNullOrEmpty(_tagname))
                {
                    // No tag name, no value.
                    return true;
                }
                else
                {
                    // Tag name but no value.
                    return caseless.ContainsKey(_tagname);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(_tagname))
                {
                    // No tag, just value. Need to search one by one.
                    foreach (var v in caseless.Values)
                    {
                        if (v.ToLower() == _tagvalue.ToLower())
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    // Both tag and value.
                    return caseless.ContainsKey(_tagname) && caseless[_tagname].ToLower() == _tagvalue.ToLower();
                }
            }
        }
    }
}
