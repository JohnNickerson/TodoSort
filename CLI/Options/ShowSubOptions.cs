using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class ShowSubOptions : UniversalOptions
    {
        [Option('c', "context", HelpText = "The context to show.", DefaultValue = "todo")]
        public string Context { get; set; }
    }
}
