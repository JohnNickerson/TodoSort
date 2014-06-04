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
        [Option('t', "title", HelpText = "The name of the new action.", Required = true)]
        public string ActionTitle { get; set; }

        [Option('c', "context", HelpText = "The context for the new action.", DefaultValue = "inbox")]
        public string Context { get; set; }

        [Option('n', "note", HelpText = "A note to add to the new item.")]
        public string Note { get; set; }
    }
}
