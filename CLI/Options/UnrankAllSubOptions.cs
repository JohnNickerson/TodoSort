using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class UnrankAllSubOptions
    {
        [Option('c', "context", HelpText = "The context to operate on.")]
        public string Context { get; set; }
    }
}
