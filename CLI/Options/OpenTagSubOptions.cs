using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class OpenTagSubOptions : SingleSearchSubOptions
    {
        [Option('t', "tag", HelpText = "The tag to open.", Required = true)]
        public string Tag { get; set; }

        [Option('m', "done", HelpText = "Mark as done, too.", DefaultValue = false)]
        public bool MarkAsDone { get; set; }

        [Option("rename", HelpText = "Rename after opening.", DefaultValue = false)]
        public bool Rename { get; set; }

        [Option("retag", HelpText = "Apply new tags after opening.", DefaultValue = false)]
        public bool Retag { get; set; }
    }
}
