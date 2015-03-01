using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class NoteSubOptions : SingleSearchSubOptions
    {
        [Option('o', "note", HelpText = "The note to add.", Required = true)]
        public string NewNote { get; set; }
    }
}
