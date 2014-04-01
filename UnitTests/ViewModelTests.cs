using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AssimilationSoftware.TodoSort.UnitTests
{
    public class ViewModelTests
    {
        /// <summary>
        /// Checks that a ViewModel search will return results for tag names.
        /// </summary>
        [Fact]
        public void Search_Tag_Names()
        {
            var mockmapper = new MockMapper();
            ViewModel vm = new ViewModel(mockmapper, mockmapper, mockmapper);

            var a = new ActionItem("inbox", "This item should match");
            a.Tags["spice"] = "melange";
            mockmapper.Save(a);

            var b = new ActionItem("inbox", "This item should not match");
            b.Tags["sugar"] = "caster";
            mockmapper.Save(b);

            var result = vm.Search("spice");

            Assert.Contains<ActionItem>(a, result);
            Assert.DoesNotContain<ActionItem>(b, result);
        }
    }
}
