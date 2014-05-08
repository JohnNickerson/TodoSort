using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class UnrankSubOptions
    {
        [Option('s', "search", HelpText = "A partial name, tag value, note contents or ID to search for.", Required = true)]
        public string SearchTerm { get; set; }

        [Option('a', "all", DefaultValue = false, HelpText = "Show all items rather than just the head of the tree.")]
        public bool AllItems { get; set; }
    }
}
