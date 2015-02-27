using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;
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

            vm.SearchTerm = "spice";
            var result = vm.SearchResults;

            Assert.Contains<ActionItem>(a, result);
            Assert.DoesNotContain<ActionItem>(b, result);
        }

        /// <summary>
        /// Make sure deferring items from the @someday context works.
        /// </summary>
        [Fact]
        public void Defer_Someday_Items()
        {
            var todo = new MockMapper();
            var someday = new MockMapper();
            var done = new MockMapper();
            ViewModel vm = new ViewModel(todo, done, someday);

            // Add an item to the "someday" context.
            var a = new ActionItem("someday", "An item to defer");
            todo.Save(a);

            vm.SearchSpecification = new ContextSearchSpecification("someday");
            vm.Defer(vm.SearchResults.ToArray());

            var b = someday.Load(a.ID);
            Assert.NotNull(b);
            var c = todo.Load(a.ID);
            Assert.Null(c);
        }

        [Fact]
        public void Defer_Whole_Projects()
        {
            var todo = new MockMapper();
            var someday = new MockMapper();
            var done = new MockMapper();
            ViewModel vm = new ViewModel(todo, done, someday);

            // Add an item to the "someday" context.
            var project = new ActionItem("someday", "A project item to defer");
            var child = new ActionItem("computer", "Maybe someday");
            child.Project = project;
            todo.Save(project);
            todo.Save(child);

            vm.SearchSpecification = new ContextSearchSpecification("someday");
            vm.Defer(vm.SearchResults.ToArray());

            var b = someday.Load(project.ID);
            var c = someday.Load(child.ID);
            Assert.NotNull(b);
            Assert.NotNull(c);

            var d = todo.Load(project.ID);
            var e = todo.Load(child.ID);
            Assert.Null(d);
            Assert.Null(e);
        }

        [Fact]
        public void Defer_With_Date()
        {
            var todo = new MockMapper();
            var someday = new MockMapper();
            var done = new MockMapper();
            ViewModel vm = new ViewModel(todo, done, someday);

            var deferitem = new ActionItem("inbox", "Waiting for a good Superman movie");
            vm.AddItem(deferitem);

            vm.Defer(deferitem, DateTime.Now.AddDays(30));

            var a = someday.Load(deferitem.ID);
            Assert.NotNull(a);
            Assert.Equal(deferitem.Context, a.Context);
            Assert.Equal(deferitem.Title, a.Title);
            Assert.Equal(deferitem.TickleDate, a.TickleDate);
        }

        [Fact]
        public void Low_Priority_Next_Project_Action()
        {
            var todo = new MockMapper();
            ViewModel vm = new ViewModel(todo, null, null);

            var testproject = new ActionItem("projects", "The test project");
            var testitemhigh = new ActionItem("todo", "High priority item");
            var testitemlow = new ActionItem("todo", "Low priority item") { RankParent = testitemhigh, Project = testproject };

            vm.AddItem(testproject);
            vm.AddItem(testitemhigh);
            vm.AddItem(testitemlow);

            // When looking for project children, always return all, regardless of ShowHeadOnly setting?
            vm.ShowHeadOnly = true;
            vm.SearchSpecification = new ProjectChildrenSearchSpecification(testproject);
            Assert.Contains(testitemlow, vm.SearchResults);
        }
    }
}
