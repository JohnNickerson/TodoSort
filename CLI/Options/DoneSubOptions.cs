using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("done", HelpText = "Move an item to the done file.")]
    public class DoneSubOptions : SingleSearchSubOptions
    {
        [Option("date", HelpText = "The date to record as the completed date.")]
        public DateTime? DoneDate { get; set; }
    }
}
