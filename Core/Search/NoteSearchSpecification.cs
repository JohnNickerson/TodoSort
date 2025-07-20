using AssimilationSoftware.Maroon.Model;
using System.Linq;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class NoteSearchSpecification : ISearchSpecification<ActionItem>
    {
        private string _notesearch;

        public NoteSearchSpecification(string notesearch)
        {
            _notesearch = notesearch.ToLower();
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.Notes.Any(n => n.ToLower().Contains(_notesearch));
        }
    }
}
