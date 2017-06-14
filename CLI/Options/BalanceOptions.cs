using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class BalanceOptions : MultiSearchSubOptions
    {
        [Option('b', "branch", HelpText = "Branching factor - the desired number of children for each node.", DefaultValue = 7)]
        public int BranchFactor { get; set; }
    }
}
