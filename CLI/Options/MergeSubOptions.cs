using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class MergeSubOptions : UniversalOptions
    {
        [Option('g', "target", HelpText = "A search term for the item to merge into.", Required = true)]
        public string FirstSearchTerm { get; set; }

        [Option('s', "search", HelpText = "A search term for the item to be merged.", Required = true)]
        public string SecondSearchTerm { get; set; }
    }
}
