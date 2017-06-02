using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class SingleSearchSubOptions : UniversalOptions
    {
        [Option('s', "search", HelpText = "A partial name, tag value or note contents to search for.")]
        public string SearchTerm { get; set; }

        [Option('i', "id", HelpText = "The beginning of the ID of the item to work on. If present, overrides full-text search.")]
        public string ItemId { get; set; }

        public ISearchSpecification<ActionItem> SearchSpecification
        {
            get
            {
                if (!string.IsNullOrEmpty(ItemId))
                {
                    return new IdSearchSpecification(ItemId);
                }
                else if (!string.IsNullOrEmpty(SearchTerm))
                {
                    return new FullTextSearchSpecification(SearchTerm);
                }
                else
                {
                    return new TrueSpecification<ActionItem>();
                }
            }
        }
    }
}
