using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("snooze", HelpText = "Move an item to the snoozed file.")]
    public class SnoozeSubOptions : SingleSearchSubOptions
    {
        [Option('r', "returndate", HelpText = "The date when this item should return to the main list.", Required = false)]
        public DateTime? ReturnDate { get; set; }
    }
}
