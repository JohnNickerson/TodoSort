using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class RenameSubOptions : SingleSearchSubOptions
    {
        [Option('n', "name", HelpText = "New title.", Required = true)]
        public string NewTitle { get; set; }

        [Option("retag", HelpText = "Apply new tags after opening.", DefaultValue = false)]
        public bool Retag { get; set; }
    }
}
