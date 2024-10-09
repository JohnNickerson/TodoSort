using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("undo", HelpText = "Move an item from the Done list back to the main list.")]
    public class UndoSubOptions : SingleSearchSubOptions
    {
        [Option('g', "target", HelpText = "The context to which the item should be restored.", Default = "inbox")]
        public string? NewContext { get; set; }
    }
}
