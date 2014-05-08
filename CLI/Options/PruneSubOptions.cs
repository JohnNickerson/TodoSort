using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class PruneSubOptions
    {
        [Option('d', "depth", HelpText = "The depth at which to operate.", Required = true)]
        public int Depth { get; set; }
    }
}
