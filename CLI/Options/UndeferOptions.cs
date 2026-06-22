using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("unsnooze", HelpText = "Move an item from the Snoozed list to the main list.")]
    public class UnsnoozeOptions : SingleSearchSubOptions { }
}
