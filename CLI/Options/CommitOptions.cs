using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("commit", HelpText = "Commits pending changes to the main data file")]
    public class CommitOptions : UniversalOptions
    {
    }
}
