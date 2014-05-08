using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class ShowSubOptions
    {
        [Option('c', "context", HelpText = "The context to show.", Required = true)]
        public string Context { get; set; }
    }
}
