using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    public class UniversalOptions
    {

        #region Global options
        [Option('v', "verbose", DefaultValue = false, HelpText = "Show all details of items in search results.")]
        public bool Verbose { get; set; }
		
		[Option("nsfw", DefaultValue = false, HelpText = "Display actual titles for items tagged as Not Safe For Work."]
		public bool NSFW {get;set;}

        [Option('a', "all", DefaultValue = false, HelpText = "Show all items rather than just the head of the tree.")]
        public bool ShowAllItems { get; set; }
        #endregion
    }
}
