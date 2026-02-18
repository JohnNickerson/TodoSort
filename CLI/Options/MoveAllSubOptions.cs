using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("move-all", HelpText = "Moves all items from one context to another.")]
    public class MoveAllSubOptions : MultiSearchSubOptions
    {
        [Option('g', "target", HelpText = "The new context to which the items should be moved.", Required = true)]
        public string? NewContext { get; set; }
    }
}
