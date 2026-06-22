using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("search-snoozed", HelpText = "Search through the collection of snoozed items.")]
    public class SnoozedSearchSubOptions : MultiSearchSubOptions
    {
        [Option("mindate", HelpText = "The minimum return date to look for.")]
        public DateTime? FromReturnDate { get; set; }

        [Option("maxdate", HelpText = "The maximum return date to look for.")]
        public DateTime? ToReturnDate { get; set; }

        public override ISearchSpecification<ActionItem> GetSearchSpecification(ITodoRepository repo)
        {
            {
                if (FromReturnDate.HasValue)
                {
                    if (ToReturnDate.HasValue)
                    {
                        return base.GetSearchSpecification(repo)
                            .And(new ReturnDateLessThanSpecification(ToReturnDate))
                            .And(new ReturnDateGreaterThanSpecification(FromReturnDate));
                    }
                    else
                    {
                        return base.GetSearchSpecification(repo)
                            .And(new ReturnDateGreaterThanSpecification(FromReturnDate));
                    }
                }
                else
                {
                    if (ToReturnDate.HasValue)
                    {
                        return base.GetSearchSpecification(repo)
                            .And(new ReturnDateLessThanSpecification(ToReturnDate));
                    }
                    else
                    {
                        return base.GetSearchSpecification(repo);
                    }
                }
            }
        }
    }
}
