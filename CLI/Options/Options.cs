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

        [VerbOption("advanced-search", HelpText = "Advanced search.")]
        public AdvancedSearchSubOptions AdvancedSearchVerb { get; set; }

        [VerbOption("count-children", HelpText = "Counts the children of a given item.")]
        public CountChildrenSubOptions CountChildrenVerb { get; set; }

        [VerbOption("defer", HelpText = "Move an item to the someday file.")]
        public DeferSubOptions DeferVerb { get; set; }

        [VerbOption("delete", HelpText = "Delete an item without doing it.")]
        public DeleteSubOptions DeleteVerb { get; set; }

        [VerbOption("done", HelpText = "Move an item to the done file.")]
        public DoneSubOptions DoneVerb { get; set; }

        [VerbOption("export", HelpText = "Save a formatted copy of a list to file.")]
        public ExportSubOptions ExportVerb { get; set; }

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
        public ProcessSubOptions ProcessVerb { get; set; }

        [VerbOption("prune", HelpText = "Defer all items at or below a given depth.")]
        public PruneSubOptions PruneVerb { get; set; }

        [VerbOption("rank", HelpText = "Vote on the relative importance of items to assign priorities.")]
        public RankSubOptions RankVerb { get; set; }

        [VerbOption("rename", HelpText = "Change the name of an item.")]
        public RenameSubOptions RenameVerb { get; set; }

        [VerbOption("search", HelpText = "Search for matching text items.")]
        public SearchSubOptions SearchVerb { get; set; }

        [VerbOption("set-parent", HelpText = "Assigns one item to be the priority parent of another.")]
        public SetParentSubOptions SetParentVerb { get; set; }

        [VerbOption("set-project", HelpText = "Sets one item to be the project parent of another.")]
        public SetProjectSubOptions SetProjectVerb { get; set; }

        [VerbOption("show", HelpText = "Display all items in a context.")]
        public ShowSubOptions ShowVerb { get; set; }

        [VerbOption("show-parents", HelpText = "Shows an item and its parent chain up to the root of the tree.")]
        public ShowParentsSubOptions ShowParentsVerb { get; set; }

        [VerbOption("someday", HelpText = "Review the someday file, assigning 10% to an active context.")]
        public SomedaySubOptions SomedayVerb { get; set; }

        [VerbOption("summary", HelpText = "Show context names and number of items in each.")]
        public SummarySubOptions SummaryVerb { get; set; }

        [VerbOption("tag", HelpText = "Adds tags to an item.")]
        public TagSubOptions TagVerb { get; set; }

        [VerbOption("unrank", HelpText = "Remove priority ranking data for one particular item.")]
        public UnrankSubOptions UnrankVerb { get; set; }

        [VerbOption("unrank-all", HelpText = "Removes all ranking data from a particular context or all items.")]
        public UnrankAllSubOptions UnrankAllVerb { get; set; }
        #endregion


        [HelpVerbOption()]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
