using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class ProcessOptions
    {
        [Option('f', "force", HelpText = "Force the program to rewrite the file, even if no changes are made.")]
        public bool Force { get; set; }
    }
}
