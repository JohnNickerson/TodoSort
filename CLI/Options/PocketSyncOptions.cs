using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("pocket-sync", HelpText = "Synchronises items with the Pocket API")]
    public class PocketSyncOptions
    {
        [Option('a', "del-archive", HelpText = "Remove items from Pocket archive", Default = false)]
        public bool ClearArchive { get; set; }

        [Option('i', "import", HelpText = "Copy unknown, unarchived items from Pocket", Default = false)]
        public bool Import { get; set; }

        [Option('e', "export", HelpText = "Copy items from 'playlist' context to Pocket", Default = false)]
        public bool Export { get; set; }
    }
}
