using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("tag-all", HelpText = "Applies a particular tag to a set of items.")]
    public class TagAllSubOptions : SingleSearchSubOptions
    {
        // Tag name
        [Option('t', "tag", HelpText = "New tag name to add.", Required = true)]
        public string TagName { get; set; }

        // Tag value
        [Option('u', "value", HelpText = "Tag value to assign. If empty, tag will be removed.")]
        public string TagValue { get; set; }

    }
}
