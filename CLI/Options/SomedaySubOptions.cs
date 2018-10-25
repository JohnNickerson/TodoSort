using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class SomedaySubOptions
    {
        [Option("pagesize", DefaultValue = 10, HelpText = "The number of items to show per page.")]
        public int PageSize { get; set; }

        [Option('i', "includedates", DefaultValue = false, HelpText = "Include items that have 'tickler' dates assigned.")]
        public bool IncludeTickle { get; set; }

        [Option("nsfw", DefaultValue = false, HelpText = "Display actual titles for items tagged as Not Safe For Work.")]
        public bool NSFW { get; set; }

        [Option('v', "verbose", DefaultValue = false, HelpText = "Show all details of items.")]
        public bool Verbose { get; set; }
    }
}
