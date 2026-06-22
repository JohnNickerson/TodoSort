using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("snooze-all", HelpText = "Snooze all items that match given search criteria.")]
    public class SnoozeAllOptions : MultiSearchSubOptions { }
}
