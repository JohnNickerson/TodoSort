using System;
using System.Collections.Generic;
using System.IO;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;
using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.TodoSort.Core.Data;

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

        [TestMethod]
        public void Find_Real_Conflicts()
        {
            var baseFile = "todo.txt";
            if (File.Exists(baseFile))
            {
                File.Delete(baseFile);
            }
            foreach (var update in Directory.GetFiles(".", "update-*.xml"))
            {
                File.Delete(update);
            }

            var mapper = new ActionItemDiskMapper(baseFile);
            var repo = new TodoRepository(mapper, ".", Environment.MachineName);
            var item = new ActionItem
            {
                ID = Guid.NewGuid(),
                ParentId = null,
                Tags = new Dictionary<string, string>(),
                Context = "testContext",
                ProjectId = null,
                Title = "A test item",
                Upvotes = 0,
                RevisionGuid = Guid.NewGuid(),
                Notes = new List<string>(),
                TickleDate = null,
                IsDeleted = false,
                Done = false,
                DoneDate = null,
                LastModified = DateTime.Now,
                PrevRevision = null,
                Status = "new"
            };
            repo.Create(item);
            repo.SaveChanges();

            var conflict1 = (ActionItem) item.Clone();
            conflict1.Title = "edited";
            repo.Update(conflict1);

            var conflict2 = (ActionItem) item.Clone();
            conflict2.Context = "moved";
            repo.Update(conflict2);
            
            repo.SaveChanges();
            Assert.AreEqual(1, repo.Items.Count());

            var pendingChanges = repo.FindConflicts();
            Assert.AreEqual(1, pendingChanges.Count);
            Assert.AreEqual(3, pendingChanges[0].Updates.Count);
            // Now all three revision IDs should be different.
            Assert.AreEqual(3, pendingChanges[0].Updates.Select(u => u.RevisionGuid).Distinct().Count());
            // And there should be only two different "previous" revision IDs, titles and contexts.
            Assert.AreEqual(2, pendingChanges[0].Updates.Select(u => u.PrevRevision).Distinct().Count());
            Assert.AreEqual(2, pendingChanges[0].Updates.Select(u => u.Title).Distinct().Count());
            Assert.AreEqual(2, pendingChanges[0].Updates.Select(u => u.Context).Distinct().Count());
        }
    }
}
