using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.CLI.Options
{

    public class MultiSearchSubOptions : UniversalOptions
    {
        // Sort
        [Option("sort", HelpText = "The name of a tag to sort by.")]
        public string? SortTag { get; set; }

        // Context
        [Option('c', "context", HelpText = "The context to search in.")]
        public string? Context { get; set; }

        // Partial prefix
        [Option('n', "name", HelpText = "Partial name to search for.")]
        public string? Title { get; set; }

        // Keyword (full-text search)
        [Option('s', "search", HelpText = "Full-text search keyword.")]
        public string? Keyword { get; set; }

        // Tag name
        [Option('t', "tag", HelpText = "Tag name.")]
        public string? TagName { get; set; }

        // Tag value
        [Option('u', "value", HelpText = "Tag value.")]
        public string? TagValue { get; set; }

        // Partial note
        [Option('o', "note", HelpText = "Partial contents of a note.")]
        public string? Note { get; set; }

        // Partial ID
        [Option('i', "id", HelpText = "The beginning of an item ID.")]
        public string? ID { get; set; }

        // Project ID
        [Option("project", HelpText = "The beginning of a project ID.")]
        public string? ProjectID { get; set; }

        // Priority parent ID
        [Option("parent", HelpText = "The beginning of a priority parent ID.")]
        public string? PriorityParentID { get; set; }

        // Minimum depth }
        [Option("mindepth", HelpText = "The minimum priority depth for results.", Default = 0)]
        public int MinDepth { get; set; }

        // Maximum depth } Can be set together by one Depth option (i.e. "set { MinDepth = value; MaxDepth = value; }")
        private int _maxdepth;
        [Option("maxdepth", HelpText = "The maximum priority depth for results.", Default = 0)]
        public int MaxDepth
        {
            get
            {
                if (ShowAllItems)
                {
                    return Int32.MaxValue;
                }
                else
                {
                    return _maxdepth;
                }
            }
            set
            {
                _maxdepth = value;
            }
        }

        [Option('d', "depth", HelpText = "Absolute depth to search at (sets min and max).")]
        public int Depth
        {
            get
            {
                return -1;
            }
            set
            {
                MinDepth = value;
                MaxDepth = value;
            }
        }

        public virtual ISearchSpecification<ActionItem> GetSearchSpecification(ITodoRepository repo)
        {
            ISearchSpecification<ActionItem> result = new TagValueSpecification(TagName, TagValue);

            if (MaxDepth == 0)
            {
                result = result.And(new HeadOnlySearchSpecification());
            }
            else if (MaxDepth == Int32.MaxValue)
            {
                // No restriction. DepthRangeSearchSpecification is very slow.
            }
            else
            {
                result = result.And(new DepthRangeSearchSpecification(MinDepth, MaxDepth, repo));
            }
            if (!string.IsNullOrEmpty(Context))
            {
                result = result.And(new ContextSearchSpecification(Context));
            }

            if (!string.IsNullOrEmpty(Title))
            {
                result = result.And(new PartialPropertyValueSpecification<string>(i => i.Title, Title));
            }

            if (!string.IsNullOrEmpty(Note))
            {
                result = result.And(new NoteSearchSpecification(Note));
            }

            if (!string.IsNullOrEmpty(ID))
            {
                // TODO: PartialIdSearchSpecification to handle GUIDs better.
                result = result.And(new IdSearchSpecification(ID));
            }

            if (!string.IsNullOrEmpty(ProjectID))
            {
                result = result.And(new ProjectChildrenSearchSpecification(ProjectID));
            }

            if (!string.IsNullOrEmpty(PriorityParentID))
            {
                result = result.And(new PriorityChildrenSearchSpecification(PriorityParentID));
            }

            if (!string.IsNullOrEmpty(Keyword))
            {
                result = result.And(new FullTextSearchSpecification(Keyword));
            }

            return result;
        }

        [Option("tree", HelpText = "Display results in a tree format.")]
        public bool PrintTree { get; set; }

        [Option("nocount", HelpText = "Hide the item count after printing results", Default = false)]
        public bool NoCount { get; set; }
    }
}
