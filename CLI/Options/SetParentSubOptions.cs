using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class SetParentSubOptions : UniversalOptions
    {
        [Option('s', "search", HelpText = "A search term to find the child item.", Required = true)]
        public string ChildSearchTerm { get; set; }

        [Option('g', "target", HelpText = "A search term to find the parent item. Defaults to same as 'search' option.")]
        public string ParentSearchTerm { get; set; }

        public ISearchSpecification<ActionItem> ChildSearchSpecification
        {
            get
            {
                ISearchSpecification<ActionItem> spec = new FullTextSearchSpecification(ChildSearchTerm);
                if (ShowAllItems)
                {
                    spec = spec.And(new DepthRangeSearchSpecification(-1, int.MaxValue));
                }
                return spec;
            }
        }

        public ISearchSpecification<ActionItem> ParentSearchSpecification
        {
            get
            {
                ISearchSpecification<ActionItem> spec = new FullTextSearchSpecification(ParentSearchTerm ?? ChildSearchTerm);
                if (ShowAllItems)
                {
                    spec = spec.And(new DepthRangeSearchSpecification(0, int.MaxValue));
                }
                return spec;
            }
        }
    }
}
