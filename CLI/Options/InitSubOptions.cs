using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class InitSubOptions
    {
        [Option('f', "filename", HelpText = "The main action list file name.", Required = true)]
        public string TodoFile { get; set; }

        [Option("someday", HelpText = "The file name for deferred items.")]
        public string SomedayFile { get; set; }

        [Option("done", HelpText = "The file name for completed items.")]
        public string DoneFile { get; set; }
    }
}
