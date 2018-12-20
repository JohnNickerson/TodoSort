using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class FullTextSearchSpecification : ISearchSpecification<ActionItem>
    {
        private string _searchTerm;

        public FullTextSearchSpecification(string search)
        {
            _searchTerm = search?.ToLower() ?? string.Empty;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.Title.ToLower().Contains(_searchTerm)
                || b.Notes.Any(n => n.ToLower().Contains(_searchTerm))
				|| b.ID.ToString().ToLower().StartsWith(_searchTerm)
                || string.Join(Environment.NewLine, b.Tags.Keys).ToLower().Contains(_searchTerm)
                || string.Join(Environment.NewLine, b.Tags.Values).ToLower().Contains(_searchTerm);
        }
    }
}
