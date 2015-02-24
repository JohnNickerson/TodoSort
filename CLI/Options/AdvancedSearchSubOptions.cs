using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class AdvancedSearchSubOptions : UniversalOptions
    {
        // Context
        [Option('c', "context", HelpText = "The context to search in.")]
        public string Context { get; set; }

        // Partial prefix
        [Option('n', "name", HelpText = "Partial name to search for.")]
        public string Title { get; set; }

        // Tag name
        [Option('t', "tag", HelpText = "Tag name.")]
        public string TagName { get; set; }

        // Tag value
        [Option('u', "value", HelpText = "Partial tag value.")]
        public string TagValue { get; set; }

        // Partial note
        [Option('o', "note", HelpText = "Partial contents of a note.")]
        public string Note { get; set; }

        // Partial ID
        [Option('i', "id", HelpText = "The beginning of an item ID.")]
        public string ID { get; set; }

        // Minimum depth }
        [Option("mindepth", HelpText = "The minimum priority depth for results.", DefaultValue = 0)]
        public int MinDepth { get; set; }

        // Maximum depth } Can be set together by one Depth option (ie "set { MinDepth = value; MaxDepth = value; }")
        [Option("maxdepth", HelpText = "The maximum priority depth for results.", DefaultValue = 0)]
        public int MaxDepth { get; set; }

        [Option('d', "depth", HelpText = "Absolute depth to search at.")]
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

        public ISearchSpecification<ActionItem> SearchSpecification
        {
            get
            {
                ISearchSpecification<ActionItem> result = new TagValueSpecification(TagName, TagValue)
                    .And(new DepthRangeSearchSpecification(MinDepth, MaxDepth));
                if (!string.IsNullOrEmpty(Context))
                {
                    result = result.And(new ExactPropertyValueSpecification<ActionItem, string>(i => i.Context, Context));
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
                    result = result.And(new PartialPropertyValueSpecification<string>(i => i.ID.ToString(), ID));
                }
                return result;
            }
        }
    }
}
