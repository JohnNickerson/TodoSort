using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("set-parent", HelpText = "Assigns one item to be the priority parent of another.")]
    public class SetParentSubOptions : UniversalOptions
    {
        [Option('s', "search", HelpText = "A search term to find the child item.", Required = true)]
        public string ChildSearchTerm { get; set; }

        [Option('g', "target", HelpText = "A search term to find the parent item. Defaults to same as 'search' option.")]
        public string ParentSearchTerm { get; set; }

        public ISearchSpecification<ActionItem> GetChildSearchSpecification(ITodoRepository repo)
        {
            ISearchSpecification<ActionItem> spec = new FullTextSearchSpecification(ChildSearchTerm);
            if (!ShowAllItems)
            {
                spec = spec.And(new HeadOnlySearchSpecification());
            }

            return spec;
        }

        public ISearchSpecification<ActionItem> GetParentSearchSpecification(ITodoRepository repo)
        {
            ISearchSpecification<ActionItem> spec = new FullTextSearchSpecification(ParentSearchTerm ?? ChildSearchTerm);
            if (!ShowAllItems)
            {
                spec = spec.And(new HeadOnlySearchSpecification());
            }

            return spec;
        }
    }
}
