using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("delete", HelpText = "Delete an item without doing it.")]
    public class DeleteOptions : SingleSearchSubOptions { }
}
