using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class Options
    {
        #region Commands
        [VerbOption("add", HelpText = "Adds a new action to a list")]
        public AddSubOptions AddVerb { get; set; }

        [VerbOption("advanced-search", HelpText = "Advanced search with Lisp-like expression syntax (and (or (not term another))).")]
        public AdvancedSearchOptions AdvancedSearchVerb { get; set; }

        [VerbOption("balance", HelpText = "Balances a selection of items into a tree with a specified branching factor")]
        public BalanceOptions BalanceVerb { get; set; }

        [VerbOption("bump", HelpText = "Halves the depth of an item, as a way of increasing its priority")]
        public SingleSearchSubOptions BumpVerb { get; set; }

        [VerbOption("check-chain", HelpText = "Checks an implicit chain for missing and excluded items")]
        public MultiSearchSubOptions ChainVerb { get; set; }

        [VerbOption("commit", HelpText = "Commits pending changes to the main data file")]
        public CommitOptions CommitVerb { get; set; }

        [VerbOption("dedupe", HelpText = "Searches for duplicates and offers to merge them")]
        public DedupeOptions DedupeVerb { get; set; }

        [VerbOption("defer", HelpText = "Move an item to the someday file.")]
        public DeferSubOptions DeferVerb { get; set; }

        [VerbOption("defer-all", HelpText = "Defer all items that match given search criteria.")]
        public MultiSearchSubOptions PruneVerb { get; set; }

        [VerbOption("delete", HelpText = "Delete an item without doing it.")]
        public SingleSearchSubOptions DeleteVerb { get; set; }

        [VerbOption("delete-done", HelpText = "Delete an item from the @done list.")]
        public SingleSearchSubOptions DeleteDoneVerb { get; set; }

        [VerbOption("done", HelpText = "Move an item to the done file.")]
        public DoneSubOptions DoneVerb { get; set; }

        [VerbOption("export", HelpText = "Save a formatted copy of a list to file.")]
        public ExportSubOptions ExportVerb { get; set; }

        [VerbOption("fix-titles", HelpText = "Attempts to repair titles for items based on their URL tag.")]
        public FixTitlesOptions FixTitlesVerb { get; set; }

        [VerbOption("import", HelpText = "Imports items from an external source.")]
        public ImportSubOptions ImportVerb { get; set; }

        [VerbOption("init", HelpText = "Initialise for the current folder.")]
        public InitSubOptions InitVerb { get; set; }

        [VerbOption("merge", HelpText = "Merge two items together.")]
        public MergeSubOptions MergeVerb { get; set; }

        [VerbOption("move", HelpText = "Move an item into another context.")]
        public MoveSubOptions MoveVerb { get; set; }

        [VerbOption("move-all", HelpText = "Moves all items from one context to another.")]
        public MoveAllSubOptions MoveAllVerb { get; set; }

        [VerbOption("note", HelpText = "Add a note to an item.")]
        public NoteSubOptions NoteVerb { get; set; }

        [VerbOption("open-tag", HelpText = "Opens (with Windows Explorer) a given tag for a given item.")]
        public OpenTagSubOptions OpenTagVerb { get; set; }

        [VerbOption("process", HelpText = "Housekeeping. Assign each inbox item to a context, ensure each project has a next action.")]
        public ProcessOptions ProcessVerb { get; set; }

        [VerbOption("rank", HelpText = "Vote on the relative importance of items to assign priorities.")]
        public MultiSearchSubOptions RankVerb { get; set; }

        [VerbOption("rename", HelpText = "Change the name of an item.")]
        public RenameSubOptions RenameVerb { get; set; }

        [VerbOption("search", HelpText = "Search through the collection of items.")]
        public MultiSearchSubOptions SearchVerb { get; set; }

        [VerbOption("search-done", HelpText = "Search through the collection of Done items.")]
        public DoneSearchSubOptions SearchDoneVerb { get; set; }

        [VerbOption("search-someday", HelpText = "Search through the collection of Someday/Deferred items.")]
        public SomedaySearchSubOptions SearchSomedayVerb { get; set; }

        [VerbOption("set-parent", HelpText = "Assigns one item to be the priority parent of another.")]
        public SetParentSubOptions SetParentVerb { get; set; }

        [VerbOption("set-project", HelpText = "Sets one item to be the project parent of another.")]
        public SetProjectSubOptions SetProjectVerb { get; set; }

        [VerbOption("someday", HelpText = "Review the someday file, assigning 10% to an active context.")]
        public SomedaySubOptions SomedayVerb { get; set; }

        [VerbOption("summary", HelpText = "Show context names and number of items in each.")]
        public UniversalOptions SummaryVerb { get; set; }

        [VerbOption("tag", HelpText = "Adds tags to an item.")]
        public SingleSearchSubOptions TagVerb { get; set; }

        [VerbOption("tag-all", HelpText = "Applies a particular tag to a set of items.")]
        public TagAllSubOptions TagAllVerb { get; set; }

        [VerbOption("undefer", HelpText = "Move an item from the Someday list to the main list.")]
        public SingleSearchSubOptions UndeferVerb { get; set; }

        [VerbOption("undo", HelpText = "Move an item from the Done list back to the main list.")]
        public UndoSubOptions UndoVerb { get; set; }

        [VerbOption("unrank", HelpText = "Remove priority ranking data for one particular item.")]
        public SingleSearchSubOptions UnrankVerb { get; set; }

        [VerbOption("unrank-all", HelpText = "Removes all ranking data from a set of items.")]
        public MultiSearchSubOptions UnrankAllVerb { get; set; }

        [VerbOption("version", HelpText = "Displays version and copyright information.")]
        public NoSubOptions VersionVerb { get; set; }
        #endregion


        [HelpVerbOption()]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
