using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Export;
using AssimilationSoftware.TodoSort.Core.Import;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Spectre.Console;
using Humanizer;
using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Views;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Controllers;

namespace AssimilationSoftware.TodoSort.CLI
{
    public class Program
    {
        private static ActionItem? selected = null;

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
                    .WithParsed<OpenTagSubOptions>(opts => OpenTag(opts, vm, repo, consoleView))
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

        private static void OpenTag(OpenTagSubOptions openTagOptions, ViewModel vm, TodoRepository repo, IItemDisplay view)
        {
            SetUniversalOptions(openTagOptions, vm);

            // Read a tag and pass it through to the "start" argVerb. Intended for URLs and file names.
            vm.SearchSpecification = openTagOptions.SearchSpecification;
            selected = view.Disambiguate(vm.SearchResults, repo, view, !string.IsNullOrEmpty(openTagOptions.ItemId));
            if (selected != null)
            {
                if (selected.Tags.TryGetValue(openTagOptions.Tag ?? string.Empty, out string? tagValue))
                {
                    if (openTagOptions.Copy)
                    {
                        //System.Windows.Forms.Clipboard.SetText(tagValue);
                        // ^^ This doesn't work.
                    }
                    else
                    {
                        OpenItemTag(tagValue);
                    }
                    if (openTagOptions.Rename)
                    {
                        view.WriteLine("What new title should this item have?");
                        var newTitle = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(newTitle))
                        {
                            vm.Rename(selected, newTitle);
                        }
                    }
                    if (openTagOptions.ReTag)
                    {
                        TagItem(vm, selected, view);
                    }
                    if (openTagOptions.MarkAsDone)
                    {
                        vm.MarkDone(null, selected);
                    }
                }
                else
                {
                    view.WriteLine("Tag not found: {0}", openTagOptions.Tag);
                }
            }

            TidyUp(vm, repo, view);
        }

        private static void SetUniversalOptions(object argSubs, ViewModel vm)
        {
            // Set universal options.
            if (argSubs is UniversalOptions options)
            {
                if (argSubs is MultiSearchSubOptions)
                {
                    vm.ShowHeadOnly = false;
                }
                else
                {
                    vm.ShowHeadOnly = !options.ShowAllItems;
                }
            }
        }

        private static void TidyUp(ViewModel vm, ITodoRepository repo, IItemDisplay view, bool verbose = false, bool forceSave = false)
        {
            if (selected != null)
            {
                view.PrintItem(selected, null, repo, false, verbose);
            }

            // Rewrite the files
            vm.Save(forceSave);
        }

        private static int HandleErrors(object errs)
        {
            return 1;
        }

        private static void OpenItemTag(string tagValue)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = tagValue;
            p.Start();
        }

        private static void TagItem(ViewModel vm, ActionItem selected, IItemDisplay view)
        {
            string? tagName;
            do
            {
                view.WriteLine("What should this new tag be called? (ENTER to quit)");
                tagName = Console.ReadLine()?.ToLower()?.Trim();
                if (!string.IsNullOrEmpty(tagName))
                {
                    view.WriteLine("What is the value of the tag?");
                    var value = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        vm.SetTag(selected, tagName, value);
                    }
                    else if (selected.Tags.ContainsKey(tagName))
                    {
                        vm.RemoveTag(selected, tagName);
                    }
                    view.WriteLine();
                }
            } while (!string.IsNullOrEmpty(tagName));
        }
    }
}
