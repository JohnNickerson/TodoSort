using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("tag", HelpText = "Adds tags to an item.")]
    public class TagOptions : SingleSearchSubOptions { }
}
