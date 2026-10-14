using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
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

            var vm = new ViewModel(repo);


            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();

            var parser = new Parser(with => { with.CaseInsensitiveEnumValues = true; with.HelpWriter = Console.Error; });
            parser.ParseArguments(args, types)
                    .WithParsed<AddSubOptions>(opts => new ItemController(consoleView).AddItem(opts, vm, repo))
                    .WithParsed<AdvancedSearchOptions>(opts => new SearchController(consoleView).AdvancedSearch(opts, repo))
                    .WithParsed<BalanceOptions>(opts => new RankingController(consoleView).Balance(opts, vm, repo))
                    .WithParsed<BumpOptions>(opts => new RankingController(consoleView).Bump(opts, vm, repo))
                    .WithParsed<CheckChainOptions>(opts => new IntegrityController(consoleView).CheckChain(opts, vm, repo))
                    .WithParsed<CommitOptions>(opts => new IntegrityController(consoleView).Commit(opts, vm, repo))
                    .WithParsed<DedupeOptions>(opts => new IntegrityController(consoleView).Dedupe(opts, vm, repo))
                    .WithParsed<DeferAllOptions>(opts => new WorkflowController(consoleView).DeferAll(opts, vm, repo))
                    .WithParsed<DeferSubOptions>(opts => new WorkflowController(consoleView).Defer(opts, vm, repo))
                    .WithParsed<DeleteOptions>(opts => new ItemController(consoleView).Delete(opts, vm, repo))
                    .WithParsed<DoneSearchSubOptions>(opts => new SearchController(consoleView).SearchDone(opts, repo))
                    .WithParsed<DoneSubOptions>(opts => new WorkflowController(consoleView).Done(opts, vm, repo))
                    .WithParsed<ExportSubOptions>(opts => new ImportExportController(consoleView).Export(opts, repo))
                    .WithParsed<FixTitlesOptions>(opts => new IntegrityController(consoleView).FixTitles(opts, repo))
                    .WithParsed<ImportSubOptions>(opts => new ImportExportController(consoleView).Import(opts, vm, repo))
                    .WithParsed<InitSubOptions>(opts => Init(opts, settingsPath))
                    .WithParsed<MergeSubOptions>(opts => new ItemController(consoleView).Merge(opts, vm, repo))
                    .WithParsed<MoveAllSubOptions>(opts => new ItemController(consoleView).MoveAll(opts, vm, repo))
                    .WithParsed<MoveSubOptions>(opts => new ItemController(consoleView).Move(opts, vm, repo))
                    .WithParsed<NoteSubOptions>(opts => new ItemController(consoleView).Note(opts, vm, repo))
                    .WithParsed<OpenTagSubOptions>(opts => new NavigationController(consoleView).OpenTag(opts, vm, repo))
                    .WithParsed<RankOptions>(opts => new RankingController(consoleView).Rank(opts, vm, repo))
                    .WithParsed<RenameSubOptions>(opts => new ItemController(consoleView).Rename(opts, vm, repo))
                    .WithParsed<SearchOptions>(opts => new SearchController(consoleView).Search(opts, repo))
                    .WithParsed<SetParentSubOptions>(opts => new RankingController(consoleView).SetParent(opts, vm, repo))
                    .WithParsed<SetProjectSubOptions>(opts => new ItemController(consoleView).SetProject(opts, vm, repo))
                    .WithParsed<SomedaySearchSubOptions>(opts => new SearchController(consoleView).SearchSomeday(opts, repo))
                    .WithParsed<TagAllSubOptions>(opts => new ItemController(consoleView).TagAll(opts, vm, repo))
                    .WithParsed<TagOptions>(opts => new ItemController(consoleView).Tag(opts, vm, repo))
                    .WithParsed<UndeferOptions>(opts => new WorkflowController(consoleView).Undefer(opts, vm, repo))
                    .WithParsed<UndoSubOptions>(opts => new WorkflowController(consoleView).Undo(opts, vm, repo))
                    .WithParsed<SummaryOptions>(opts => new SearchController(consoleView).Summary(opts, vm))
                    .WithParsed<UnrankAllOptions>(opts => new RankingController(consoleView).UnrankAll(opts, vm, repo))
                    .WithParsed<UnRankOptions>(opts => new RankingController(consoleView).Unrank(opts, vm, repo))
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
