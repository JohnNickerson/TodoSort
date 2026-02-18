using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("defer", HelpText = "Move an item to the someday file.")]
    public class DeferSubOptions : SingleSearchSubOptions
    {
        [Option('r', "tickledate", HelpText = "The date when this item should return to the main list.", Required = false)]
        public DateTime? TickleDate { get; set; }
    }
}
