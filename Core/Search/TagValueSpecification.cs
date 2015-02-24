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
                    return b.Tags.ContainsKey(_tagname);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(_tagname))
                {
                    // No tag, just value.
                    return b.Tags.ContainsValue(_tagvalue);
                }
                else
                {
                    // Both tag and value.
                    return b.Tags.ContainsKey(_tagname) && b.Tags[_tagname] == _tagvalue;
                }
            }
        }
    }
}
