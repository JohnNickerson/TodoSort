using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class OpenTagSubOptions : SearchSubOptions
    {
        [Option('t', "tag", HelpText = "The tag to open.", Required = true)]
        public string Tag { get; set; }
    }
}
