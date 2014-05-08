using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class AddSubOptions
    {
        [Option('n', "name", HelpText = "The name of the new action.", Required = true)]
        public string ActionTitle { get; set; }

        [Option('c', "context", HelpText = "The context for the new action.", DefaultValue = "inbox")]
        public string Context { get; set; }
    }
}
