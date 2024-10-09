using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("dedupe", HelpText = "Searches for duplicates and offers to merge them")]
    public class DedupeOptions : UniversalOptions
    {
        // Specify searching by name or a specific tag?
        [Option('t', "tag", HelpText = "A tag to search for duplicate values")]
        public string? Tag { get; set; }
    }
}
