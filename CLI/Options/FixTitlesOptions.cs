using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("fix-titles", HelpText = "Attempts to repair titles for items based on their URL tag.")]
    public class FixTitlesOptions : MultiSearchSubOptions
    {
        [Option("move", HelpText = "The context to move items to when they are successfully renamed.")]
        public string? MoveTo { get; set; }
    }
}
