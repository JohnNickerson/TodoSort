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
    public class DoneSearchSubOptions : MultiSearchSubOptions
    {
        [Option("mindate", HelpText = "The minimum tickle date to look for.")]
        public DateTime? FromDoneDate { get; set; }

        [Option("maxdate", HelpText = "The maximum tickle date to look for.")]
        public DateTime? ToDoneDate { get; set; }

        public override ISearchSpecification<ActionItem> GetSearchSpecification(ITodoRepository repo)
        {
            if (FromDoneDate.HasValue)
            {
                if (ToDoneDate.HasValue)
                {
                    return base.GetSearchSpecification(repo)
                        .And(new DoneDateLessThanSpecification(ToDoneDate))
                        .And(new DoneDateGreaterThanSpecification(FromDoneDate));
                }
                else
                {
                    return base.GetSearchSpecification(repo)
                        .And(new DoneDateGreaterThanSpecification(FromDoneDate));
                }
            }
            else
            {
                if (ToDoneDate.HasValue)
                {
                    return base.GetSearchSpecification(repo)
                        .And(new DoneDateLessThanSpecification(ToDoneDate));
                }
                else
                {
                    return base.GetSearchSpecification(repo);
                }
            }
        }
    }
}
