using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("balance", HelpText = "Balances a selection of items into a tree with a specified branching factor")]
    public class BalanceOptions : MultiSearchSubOptions
    {
        [Option('b', "branch", HelpText = "Branching factor - the desired number of children for each node.", Default = 7)]
        public int BranchFactor { get; set; }

        [Option("commit", HelpText = "Commit changes immediately from memory when done", Default = false)]
        public bool Commit { get; set; }
    }
}
