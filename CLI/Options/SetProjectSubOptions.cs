using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class SetProjectSubOptions : UniversalOptions
    {
        [Option('s', "search", HelpText = "A search term to find the child item.", Required = true)]
        public string ChildSearchTerm { get; set; }

        [Option('g', "target", HelpText = "A search term to find the project item. Defaults to same as 'search' option.")]
        public string ProjectSearchTerm { get; set; }
    }
}
