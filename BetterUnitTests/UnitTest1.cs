using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BetterUnitTests
{
    [TestClass]
    public class ViewModelTests
    {
        [TestMethod]
        public void FindOnlyRealDuplicates()
        {
            // Arrange
            var marp = new MockRepository();
            var vm = new ViewModel(marp);

            marp.Create(new ActionItem { Context = "inbox", Title = "Not matching" });
            marp.Create(new ActionItem { Context = "inbox", Title = "also not a match" });
            marp.Create(new ActionItem { Context = "inbox", Title = "no chance" });
            marp.Create(new ActionItem { Context = "inbox", Title = "well, it's something" });

            // Act
            var d = vm.GetDuplicateTags("url");

            // Assert
            Assert.AreEqual(0, d.Count());
        }
    }
}
