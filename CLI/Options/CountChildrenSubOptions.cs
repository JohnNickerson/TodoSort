using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class CountChildrenSubOptions : UniversalOptions
    {
        [Option('c', "context", HelpText = "The context to process.", DefaultValue = "todo")]
        public string Context { get; set; }
    }
}
