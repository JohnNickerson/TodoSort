using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class TagValueSpecification : ISearchSpecification<ActionItem>
    {
        private string _tagname;
        private string _tagvalue;

        public TagValueSpecification(string tagname, string tagvalue)
        {
            _tagname = tagname?.Trim();
            _tagvalue = tagvalue?.Trim();
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            // Case-insensitive search. ActionItem.Tags uses a case-insensitive comparer, so we should be good to go.
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
                    // No tag, just value. Need to search one by one.
                    foreach (var v in b.Tags.Values)
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
                    return b.Tags.ContainsKey(_tagname) && b.Tags[_tagname].ToLower() == _tagvalue.ToLower();
                }
            }
        }
    }
}
