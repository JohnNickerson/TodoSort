using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class ExportSubOptions : MultiSearchSubOptions
    {
        [Option('e', "format", HelpText = "The export format to use: html or graphviz.", DefaultValue = "html")]
        public string Format { get; set; }

        [Option('f', "file", HelpText = "The filename to write to.")]
        public string Filename { get; set; }
    }
}
