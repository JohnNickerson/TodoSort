using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("import", HelpText = "Imports items from an external source.")]
    public class ImportSubOptions
    {
        [Option('e', "format", HelpText = "The import format to use (todosort, instapaper).", Default = "todosort")]
        public string? Format { get; set; }

        [Option('f', "file", HelpText = "The filename or folder to read from.", Required = true)]
        public string? Filename { get; set; }

        [Option('c', "context", HelpText = "The context to assign to imported items.")]
        public string? Context { get; set; }
    }
}
