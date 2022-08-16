using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("summary", HelpText = "Show context names and number of items in each.")]
    public class SummaryOptions : UniversalOptions { }

    public class UniversalOptions
    {

        #region Global options
        [Option('v', "verbose", Default = false, HelpText = "Show all details of items in search results.")]
        public bool Verbose { get; set; }
		
		[Option("nsfw", Default = false, HelpText = "Display actual titles for items tagged as Not Safe For Work.")]
		public bool NSFW {get;set;}

        [Option('a', "all", Default = false, HelpText = "Show all items rather than just the head of the tree.")]
        public bool ShowAllItems { get; set; }
        #endregion
    }
}
