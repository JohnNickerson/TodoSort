using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("review-snoozed", HelpText = "Review the snoozed file, assigning 10% to an active context.")]
    public class SnoozedSubOptions
    {
        [Option("pagesize", Default = 10, HelpText = "The number of items to show per page.")]
        public int PageSize { get; set; }

        [Option('i', "includedates", Default = false, HelpText = "Include items that have return dates assigned.")]
        public bool IncludeReturn { get; set; }

        [Option("nsfw", Default = false, HelpText = "Display actual titles for items tagged as Not Safe For Work.")]
        public bool NSFW { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Show all details of items.")]
        public bool Verbose { get; set; }
    }
}
