using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("add", HelpText = "Adds a new action to a list")]
    public class AddSubOptions
    {
        [Option('n', "name", HelpText = "The name of the new action.", Required = true)]
        public string ActionTitle { get; set; }

        [Option('c', "context", HelpText = "The context for the new action.", Default = "inbox")]
        public string Context { get; set; }

        [Option('o', "note", HelpText = "A note to add to the new item.")]
        public string Note { get; set; }
    }
}
