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

        [Option('g', "target", HelpText = "A search term to find the parent item.", Required = true)]
        public string ParentSearchTerm { get; set; }
    }
}
