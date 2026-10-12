using AssimilationSoftware.TodoSort.CLI.Enums;
using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("export", HelpText = "Save a formatted copy of a list to file.")]
    public class ExportSubOptions : MultiSearchSubOptions
    {
        [Option('e', "format", HelpText = "The export format to use: HTML, graphviz, JSON, todotxt or todosort.", Default = ExportFormat.html)]
        public ExportFormat? Format { get; set; }

        [Option('f', "file", HelpText = "The filename to write to.", Required = true)]
        public string? Filename { get; set; }

        [Option("template", HelpText = "A template file to use for the output format. Overrides 'format' if present.")]
        public string? TemplateFilename { get; set; }

        [Option("sort-desc", HelpText = "Specifies a tag by which to sort in descending order. Will not be used if 'sort' is present.")]
        public string? SortDescTag { get; set; }
    }
}
