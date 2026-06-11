using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("rename", HelpText = "Change the name of an item.")]
    public class RenameSubOptions : SingleSearchSubOptions
    {
        [Option('n', "name", HelpText = "New title.", Required = true)]
        public string? NewTitle { get; set; }

        [Option("retag", HelpText = "Apply new tags after opening.", Default = false)]
        public bool ReTag { get; set; }
    }
}
