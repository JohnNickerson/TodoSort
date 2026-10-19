using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using CommandLine;
using System.Diagnostics;
using System.Reflection;
using Spectre.Console;
using AssimilationSoftware.TodoSort.CLI.Views;
using AssimilationSoftware.TodoSort.CLI.Controllers;

namespace AssimilationSoftware.TodoSort.CLI
{
    public class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.AutoFlush = true;
#endif
            var argVerb = string.Empty;
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ".todosort");
            var f = FolderSettings.LoadFrom(settingsPath);
            var todoMapper = new ActionItemDiskMapper(f.TodoPath);
            var repo = new TodoRepository(todoMapper, Path.GetDirectoryName(f.TodoPath), Environment.MachineName);
            var consoleView = new ConsoleItemDisplay();

            var vm = new TodoService(repo);


            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();

            var parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; with.HelpWriter = Console.Error; });
            parser.ParseArguments(args, types)
                    .WithParsed<AddSubOptions>(opts => new ItemController(consoleView).AddItem(opts, vm))
                    .WithParsed<AdvancedSearchOptions>(opts => new SearchController(consoleView).AdvancedSearch(opts, vm))
                    .WithParsed<BalanceOptions>(opts => new RankingController(consoleView).Balance(opts, vm))
                    .WithParsed<BumpOptions>(opts => new RankingController(consoleView).Bump(opts, vm))
                    .WithParsed<CheckChainOptions>(opts => new IntegrityController(consoleView).CheckChain(opts, vm))
                    .WithParsed<CommitOptions>(opts => new IntegrityController(consoleView).Commit(opts, vm))
                    .WithParsed<DedupeOptions>(opts => new IntegrityController(consoleView).Dedupe(opts, vm))
                    .WithParsed<DeferAllOptions>(opts => new WorkflowController(consoleView).DeferAll(opts, vm))
                    .WithParsed<DeferSubOptions>(opts => new WorkflowController(consoleView).Defer(opts, vm))
                    .WithParsed<DeleteOptions>(opts => new ItemController(consoleView).Delete(opts, vm))
                    .WithParsed<DoneSearchSubOptions>(opts => new SearchController(consoleView).SearchDone(opts, vm))
                    .WithParsed<DoneSubOptions>(opts => new WorkflowController(consoleView).Done(opts, vm))
                    .WithParsed<ExportSubOptions>(opts => new ImportExportController(consoleView).Export(opts, vm))
                    .WithParsed<FixTitlesOptions>(opts => new IntegrityController(consoleView).FixTitles(opts, vm))
                    .WithParsed<ImportSubOptions>(opts => new ImportExportController(consoleView).Import(opts, vm))
                    .WithParsed<InitSubOptions>(opts => Init(opts, settingsPath))
                    .WithParsed<MergeSubOptions>(opts => new ItemController(consoleView).Merge(opts, vm))
                    .WithParsed<MoveAllSubOptions>(opts => new ItemController(consoleView).MoveAll(opts, vm))
                    .WithParsed<MoveSubOptions>(opts => new ItemController(consoleView).Move(opts, vm))
                    .WithParsed<NoteSubOptions>(opts => new ItemController(consoleView).Note(opts, vm))
                    .WithParsed<OpenTagSubOptions>(opts => new NavigationController(consoleView).OpenTag(opts, vm))
                    .WithParsed<RankOptions>(opts => new RankingController(consoleView).Rank(opts, vm))
                    .WithParsed<RenameSubOptions>(opts => new ItemController(consoleView).Rename(opts, vm))
                    .WithParsed<SearchOptions>(opts => new SearchController(consoleView).Search(opts, vm))
                    .WithParsed<SetParentSubOptions>(opts => new RankingController(consoleView).SetParent(opts, vm))
                    .WithParsed<SetProjectSubOptions>(opts => new ItemController(consoleView).SetProject(opts, vm))
                    .WithParsed<SomedaySearchSubOptions>(opts => new SearchController(consoleView).SearchSomeday(opts, vm))
                    .WithParsed<TagAllSubOptions>(opts => new ItemController(consoleView).TagAll(opts, vm))
                    .WithParsed<TagOptions>(opts => new ItemController(consoleView).Tag(opts, vm))
                    .WithParsed<UndeferOptions>(opts => new WorkflowController(consoleView).Undefer(opts, vm))
                    .WithParsed<UndoSubOptions>(opts => new WorkflowController(consoleView).Undo(opts, vm))
                    .WithParsed<SummaryOptions>(opts => new SearchController(consoleView).Summary(opts, vm))
                    .WithParsed<UnrankAllOptions>(opts => new RankingController(consoleView).UnrankAll(opts, vm))
                    .WithParsed<UnRankOptions>(opts => new RankingController(consoleView).Unrank(opts, vm))
                    .WithNotParsed(errs => HandleErrors(errs));
        }

        private static void Init(InitSubOptions initOpts, string settingsPath)
        {
            var initSettings = new FolderSettings
            {
                TodoPath = initOpts.TodoFile
            };

            // Save settings.
            FolderSettings.SaveTo(settingsPath, initSettings);
            string rawDirectory = Path.GetDirectoryName(initOpts.TodoFile) ?? ".";
            var dirName = Path.GetFullPath(string.IsNullOrEmpty(rawDirectory) ? "." : rawDirectory);
            if (dirName is not null && !Directory.Exists(dirName) && AnsiConsole.Confirm($"{initOpts.TodoFile} does not exist. Create it?", false))
            {
                Directory.CreateDirectory(dirName);
            }
        }

        private static int HandleErrors(object errs)
        {
            return 1;
        }
    }
}
