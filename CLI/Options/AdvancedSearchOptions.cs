using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("advanced-search", HelpText = "Advanced search with Lisp-like expression syntax (and (or (not term another))).")]
    public class AdvancedSearchOptions : UniversalOptions
    {
        [Option('s', "search", HelpText = "Search using Lisp-like complex expressions.", Required = true)]
        public string? Expression { get; set; }

        public ISearchSpecification<ActionItem> SearchSpecification
        {
            get
            {
                return ExpressionParser.Parse(Expression);
            }
        }
    }
}
