using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("update", HelpText = "Update items from an external source.")]
    public class UpdateSubOptions
    {
        [Option('e', "format", HelpText = "The external format to use (instapaper).", Default = "instapaper")]
        public string? Format { get; set; }

        [Option('f', "file", HelpText = "The filename or folder to read from.", Required = true)]
        public string? Filename { get; set; }

        [Option('c', "context", HelpText = "The context to assign to new items.")]
        public string? Context { get; set; }
    }
}
