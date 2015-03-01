using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class DeferSubOptions : SingleSearchSubOptions
    {
        [Option('r', "tickledate", HelpText = "The date when this item should return to the main list.", Required = false)]
        public DateTime? TickleDate { get; set; }
    }
}
