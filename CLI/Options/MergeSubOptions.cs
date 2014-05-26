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
        [Option('f', "first", HelpText = "A search term to find the first item.", Required = true)]
        public string FirstSearchTerm { get; set; }

        [Option('s', "second", HelpText = "A search term to find the second item.", Required = true)]
        public string SecondSearchTerm { get; set; }
    }
}
