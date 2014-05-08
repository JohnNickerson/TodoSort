using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class SearchSubOptions : UniversalOptions
    {
        [Option('s', "search", HelpText = "A partial name, tag value, note contents or ID to search for.", Required = true)]
        public string SearchTerm { get; set; }
    }
}
