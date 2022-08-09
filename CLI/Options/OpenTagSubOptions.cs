using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("open-tag", HelpText = "Opens (with Windows Explorer) a given tag for a given item.")]
    public class OpenTagSubOptions : SingleSearchSubOptions
    {
        [Option('t', "tag", HelpText = "The tag to open.", Required = true)]
        public string Tag { get; set; }

        [Option('m', "done", HelpText = "Mark as done, too.", Default = false)]
        public bool MarkAsDone { get; set; }

        [Option("rename", HelpText = "Rename after opening.", Default = false)]
        public bool Rename { get; set; }

        [Option("retag", HelpText = "Apply new tags after opening.", Default = false)]
        public bool Retag { get; set; }

        [Option("copy", HelpText = "Copy the tag value to the clipboard, rather than opening it.", Default = false)]
        public bool Copy { get; set; }
    }
}
