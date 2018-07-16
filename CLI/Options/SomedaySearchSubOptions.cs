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
    public class SomedaySearchSubOptions : MultiSearchSubOptions
    {
        [Option("mindate", HelpText = "The minimum tickle date to look for.")]
        public DateTime? FromTickleDate { get; set; }

        [Option("maxdate", HelpText = "The maximum tickle date to look for.")]
        public DateTime? ToTickleDate { get; set; }

        public override ISearchSpecification<ActionItem> SearchSpecification
        {
            get
            {
                if (FromTickleDate.HasValue)
                {
                    if (ToTickleDate.HasValue)
                    {
                        return base.SearchSpecification
                            .And(new TickleDateLessThanSpecification(ToTickleDate))
                            .And(new TickleDateGreaterThanSpecification(FromTickleDate));
                    }
                    else
                    {
                        return base.SearchSpecification
                            .And(new TickleDateGreaterThanSpecification(FromTickleDate));
                    }
                }
                else
                {
                    if (ToTickleDate.HasValue)
                    {
                        return base.SearchSpecification
                            .And(new TickleDateLessThanSpecification(ToTickleDate));
                    }
                    else
                    {
                        return base.SearchSpecification;
                    }
                }
            }
        }
    }
}
