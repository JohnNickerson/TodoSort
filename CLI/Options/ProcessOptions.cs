using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("process", HelpText = "Housekeeping. Assign each inbox item to a context, ensure each project has a next action.")]
    public class ProcessOptions
    {
        [Option('f', "force", HelpText = "Force the program to rewrite the file, even if no changes are made.")]
        public bool Force { get; set; }
    }
}
