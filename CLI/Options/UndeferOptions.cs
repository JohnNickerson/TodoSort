using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("undefer", HelpText = "Move an item from the Someday list to the main list.")]
    public class UndeferOptions : SingleSearchSubOptions { }
}
