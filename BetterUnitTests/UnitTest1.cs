using AssimilationSoftware.PimData.Model;
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
            var marp = new MockMapper();
            var vm = new ViewModel(marp);

            marp.Save(new AssimilationSoftware.PimData.Model.ActionItem("inbox", "Not matching"));
            marp.Save(new ActionItem("inbox", "also not a match"));
            marp.Save(new ActionItem("inbox", "no chance"));
            marp.Save(new ActionItem("inbox", "well, it's something"));

            // Act
            var d = vm.GetDuplicateTags("url");

            // Assert
            Assert.AreEqual(0, d.Count());
        }
    }
}
