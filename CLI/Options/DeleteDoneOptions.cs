using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("delete-done", HelpText = "Delete an item from the @done list.")]
    public class DeleteDoneOptions : SingleSearchSubOptions { }
}
