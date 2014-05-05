using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI
{
    public class Options
    {
        #region Commands
        [VerbOption("add", HelpText = "Adds a new action to a list")]
        public AddSubOptions AddVerb { get; set; }
        #endregion

        #region Global options
        [Option('v', "verbose", DefaultValue = false, HelpText = "Show all details of items in search results.")]
        public bool Verbose { get; set; }

        [Option('a', "all", DefaultValue = false, HelpText = "Show all items rather than just the head of the tree.")]
        public bool ShowAllItems { get; set; }
        #endregion
    }

    public class AddSubOptions
    {
        [Option('n', "name", HelpText = "The name of the new action.", Required = true)]
        public string ActionTitle { get; set; }

        [Option('c', "context", HelpText = "The context for the new action.", DefaultValue = "inbox")]
        public string Context { get; set; }
    }
}
