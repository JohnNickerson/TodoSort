using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("export", HelpText = "Save a formatted copy of a list to file.")]
    public class ExportSubOptions : MultiSearchSubOptions
    {
        [Option('e', "format", HelpText = "The export format to use: HTML, graphviz, JSON or text.", Default = "html")]
        public string Format { get; set; }

        [Option('f', "file", HelpText = "The filename to write to.", Required = true)]
        public string Filename { get; set; }

        [Option("template", HelpText = "A template file to use for the output format. Overrides 'format' if present.")]
        public string TemplateFilename { get; set; }

        [Option("sort-desc", HelpText = "Specifies a tag by which to sort in descending order. Will not be used if 'sort' is present.")]
        public string SortDescTag { get; set; }
    }
}
