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
                    .WithParsed<AddSubOptions>(opts => new ItemController().AddItem(opts, vm, repo, consoleView))
                    .WithParsed<AdvancedSearchOptions>(opts => new SearchController().AdvancedSearch(opts, vm, repo, consoleView))
                    .WithParsed<BalanceOptions>(opts => new RankingController().Balance(opts, vm, repo, consoleView))
                    .WithParsed<BumpOptions>(opts => new RankingController().Bump(opts, vm, repo, consoleView))
                    .WithParsed<CheckChainOptions>(opts => new IntegrityController().CheckChain(opts, vm, repo, consoleView))
                    .WithParsed<CommitOptions>(opts => new IntegrityController().Commit(opts, vm, repo, consoleView))
                    .WithParsed<DedupeOptions>(opts => new IntegrityController().Dedupe(opts, vm, repo, consoleView))
                    .WithParsed<DeferAllOptions>(opts => new WorkflowController().DeferAll(opts, vm, repo, consoleView))
                    .WithParsed<DeferSubOptions>(opts => new WorkflowController().Defer(opts, vm, repo, consoleView))
                    .WithParsed<DeleteOptions>(opts => new ItemController().Delete(opts, vm, repo, consoleView))
                    .WithParsed<DoneSearchSubOptions>(opts => new SearchController().SearchDone(opts, vm, repo, consoleView))
                    .WithParsed<DoneSubOptions>(opts => new WorkflowController().Done(opts, vm, repo, consoleView))
                    .WithParsed<ExportSubOptions>(opts => new ImportExportController().Export(opts, vm, repo, consoleView))
                    .WithParsed<FixTitlesOptions>(opts => new IntegrityController().FixTitles(opts, vm, repo, consoleView))
                    .WithParsed<ImportSubOptions>(opts => new ImportExportController().Import(opts, vm, repo, consoleView))
                    .WithParsed<InitSubOptions>(opts => Init(opts, settingsPath))
                    .WithParsed<MergeSubOptions>(opts => new ItemController().Merge(opts, vm, repo, consoleView))
                    .WithParsed<MoveAllSubOptions>(opts => new ItemController().MoveAll(opts, vm, repo, consoleView))
                    .WithParsed<MoveSubOptions>(opts => new ItemController().Move(opts, vm, repo, consoleView))
                    .WithParsed<NoteSubOptions>(opts => new ItemController().Note(opts, vm, repo, consoleView))
                    .WithParsed<OpenTagSubOptions>(opts => new NavigationController().OpenTag(opts, vm, repo, consoleView))
                    .WithParsed<RankOptions>(opts => new RankingController().Rank(opts, vm, repo, consoleView))
                    .WithParsed<RenameSubOptions>(opts => new ItemController().Rename(opts, vm, repo, consoleView))
                    .WithParsed<SearchOptions>(opts => new SearchController().Search(opts, vm, repo, consoleView))
                    .WithParsed<SetParentSubOptions>(opts => new RankingController().SetParent(opts, vm, repo, consoleView))
                    .WithParsed<SetProjectSubOptions>(opts => new ItemController().SetProject(opts, vm, repo, consoleView))
                    .WithParsed<SomedaySearchSubOptions>(opts => new SearchController().SearchSomeday(opts, vm, repo, consoleView))
                    .WithParsed<TagAllSubOptions>(opts => new ItemController().TagAll(opts, vm, repo, consoleView))
                    .WithParsed<TagOptions>(opts => new ItemController().Tag(opts, vm, repo, consoleView))
                    .WithParsed<UndeferOptions>(opts => new WorkflowController().Undefer(opts, vm, repo, consoleView))
                    .WithParsed<UndoSubOptions>(opts => new WorkflowController().Undo(opts, vm, repo, consoleView))
                    .WithParsed<SummaryOptions>(opts => new SearchController().Summary(opts, vm, repo, consoleView))
                    .WithParsed<UnrankAllOptions>(opts => new RankingController().UnrankAll(opts, vm, repo, consoleView))
                    .WithParsed<UnRankOptions>(opts => new RankingController().Unrank(opts, vm, repo, consoleView))
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
