using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("done", HelpText = "Move an item to the done file.")]
    public class DoneSubOptions : SingleSearchSubOptions
    {
        [Option("date", HelpText = "The date to record as the completed date.")]
        public DateTime? DoneDate { get; set; }
    }
}
