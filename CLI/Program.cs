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
                    .WithParsed<AddSubOptions>(opts => new ItemController(vm, consoleView).AddItem(opts))
                    .WithParsed<AdvancedSearchOptions>(opts => new SearchController(vm, consoleView).AdvancedSearch(opts))
                    .WithParsed<BalanceOptions>(opts => new RankingController(vm, consoleView).Balance(opts))
                    .WithParsed<BumpOptions>(opts => new RankingController(vm, consoleView).Bump(opts))
                    .WithParsed<CheckChainOptions>(opts => new IntegrityController(vm, consoleView).CheckChain(opts))
                    .WithParsed<CommitOptions>(opts => new IntegrityController(vm, consoleView).Commit(opts))
                    .WithParsed<DedupeOptions>(opts => new IntegrityController(vm, consoleView).Dedupe(opts))
                    .WithParsed<DeferAllOptions>(opts => new WorkflowController(vm, consoleView).DeferAll(opts))
                    .WithParsed<DeferSubOptions>(opts => new WorkflowController(vm, consoleView).Defer(opts))
                    .WithParsed<DeleteOptions>(opts => new ItemController(vm, consoleView).Delete(opts))
                    .WithParsed<DoneSearchSubOptions>(opts => new SearchController(vm, consoleView).SearchDone(opts))
                    .WithParsed<DoneSubOptions>(opts => new WorkflowController(vm, consoleView).Done(opts))
                    .WithParsed<ExportSubOptions>(opts => new ImportExportController(vm, consoleView).Export(opts))
                    .WithParsed<FixTitlesOptions>(opts => new IntegrityController(vm, consoleView).FixTitles(opts))
                    .WithParsed<ImportSubOptions>(opts => new ImportExportController(vm, consoleView).Import(opts))
                    .WithParsed<InitSubOptions>(opts => Init(opts, settingsPath))
                    .WithParsed<MergeSubOptions>(opts => new ItemController(vm, consoleView).Merge(opts))
                    .WithParsed<MoveAllSubOptions>(opts => new ItemController(vm, consoleView).MoveAll(opts))
                    .WithParsed<MoveSubOptions>(opts => new ItemController(vm, consoleView).Move(opts))
                    .WithParsed<NoteSubOptions>(opts => new ItemController(vm, consoleView).Note(opts))
                    .WithParsed<OpenTagSubOptions>(opts => new NavigationController(vm, consoleView).OpenTag(opts))
                    .WithParsed<RankOptions>(opts => new RankingController(vm, consoleView).Rank(opts))
                    .WithParsed<RenameSubOptions>(opts => new ItemController(vm, consoleView).Rename(opts))
                    .WithParsed<SearchOptions>(opts => new SearchController(vm, consoleView).Search(opts))
                    .WithParsed<SetParentSubOptions>(opts => new RankingController(vm, consoleView).SetParent(opts))
                    .WithParsed<SetProjectSubOptions>(opts => new ItemController(vm, consoleView).SetProject(opts))
                    .WithParsed<SomedaySearchSubOptions>(opts => new SearchController(vm, consoleView).SearchSomeday(opts))
                    .WithParsed<TagAllSubOptions>(opts => new ItemController(vm, consoleView).TagAll(opts))
                    .WithParsed<TagOptions>(opts => new ItemController(vm, consoleView).Tag(opts))
                    .WithParsed<UndeferOptions>(opts => new WorkflowController(vm, consoleView).Undefer(opts))
                    .WithParsed<UndoSubOptions>(opts => new WorkflowController(vm, consoleView).Undo(opts))
                    .WithParsed<SummaryOptions>(opts => new SearchController(vm, consoleView).Summary(opts))
                    .WithParsed<UnrankAllOptions>(opts => new RankingController(vm, consoleView).UnrankAll(opts))
                    .WithParsed<UnRankOptions>(opts => new RankingController(vm, consoleView).Unrank(opts))
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
