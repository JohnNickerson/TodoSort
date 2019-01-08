using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class FixTitlesOptions : MultiSearchSubOptions
    {
        [Option("move", HelpText = "The context to move items to when they are successfully renamed.")]
        public string MoveTo { get; set; }
    }
}
