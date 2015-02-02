using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class MoveAllSubOptions
    {
        [Option('c', "context", HelpText = "The new context to which the items should be moved.", Required = true)]
        public string NewContext { get; set; }

        [Option('s', "search", HelpText = "The current context from which items should be moved.", Required = true)]
        public string Search { get; set; }
    }
}
