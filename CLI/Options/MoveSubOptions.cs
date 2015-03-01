using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class MoveSubOptions : SingleSearchSubOptions
    {
        [Option('c', "context", HelpText = "The new context to which the item should be moved.", Required = true)]
        public string NewContext { get; set; }
    }
}
