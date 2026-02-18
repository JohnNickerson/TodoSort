using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("init", HelpText = "Initialise for the current folder.")]
    public class InitSubOptions
    {
        [Option('f', "filename", HelpText = "The main action list file name.", Required = true)]
        public string? TodoFile { get; set; }
    }
}
