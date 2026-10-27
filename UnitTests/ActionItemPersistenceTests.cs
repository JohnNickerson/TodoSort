using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.UnitTests;

public class ActionItemPersistenceTests
{
    [Fact]
    public void Saving_ActionItem_Assigns_NonEmpty_RevisionGuid()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var fileName = Path.Combine(tempDirectory.FullName, "todos.csv");
            var repository = new TodoRepository(
                new ActionItemDiskMapper(fileName),
                tempDirectory.FullName,
                Environment.MachineName);
            var item = new ActionItem
            {
                ID = Guid.NewGuid(),
                Title = "Test item",
                Context = "inbox",
                RevisionGuid = Guid.Empty
            };

            repository.Create(item);
            repository.SaveChanges();

            var savedItem = new TodoRepository(
                    new ActionItemDiskMapper(fileName),
                    tempDirectory.FullName,
                    Environment.MachineName)
                .FindAll()
                .Single();

            Assert.NotEqual(Guid.Empty, savedItem.RevisionGuid);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Saving_ActionItem_Read_From_File_Without_Revision_Assigns_NonEmpty_RevisionGuid()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var fileName = Path.Combine(tempDirectory.FullName, "todos.txt");
            var id = Guid.NewGuid();
            File.WriteAllText(fileName, $"@inbox{Environment.NewLine}\tLegacy item{Environment.NewLine}\t\t#id:{id}");

            var repository = new TodoRepository(
                new ActionItemDiskMapper(fileName),
                tempDirectory.FullName,
                Environment.MachineName);
            var item = repository.FindAll().Single();

            Assert.Equal(id, item.ID);
            Assert.Equal(Guid.Empty, item.RevisionGuid);

            item.Title = "Updated legacy item";
            repository.Update(item);
            repository.SaveChanges();

            var savedItem = new TodoRepository(
                    new ActionItemDiskMapper(fileName),
                    tempDirectory.FullName,
                    Environment.MachineName)
                .FindAll()
                .Single();

            Assert.NotEqual(Guid.Empty, savedItem.RevisionGuid);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Saving_ActionItem_Read_From_File_With_Empty_Revision_Assigns_NonEmpty_RevisionGuid()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var fileName = Path.Combine(tempDirectory.FullName, "todos.txt");
            var id = Guid.NewGuid();
            File.WriteAllText(
                fileName,
                $"@inbox{Environment.NewLine}\tLegacy item{Environment.NewLine}\t\t#id:{id}{Environment.NewLine}\t\t#revision:{Guid.Empty}");

            var repository = new TodoRepository(
                new ActionItemDiskMapper(fileName),
                tempDirectory.FullName,
                Environment.MachineName);
            var item = repository.FindAll().Single();

            Assert.Equal(id, item.ID);
            Assert.Equal(Guid.Empty, item.RevisionGuid);

            item.Title = "Updated legacy item";
            repository.Update(item);
            repository.SaveChanges();

            var savedItem = new TodoRepository(
                    new ActionItemDiskMapper(fileName),
                    tempDirectory.FullName,
                    Environment.MachineName)
                .FindAll()
                .Single();

            Assert.NotEqual(Guid.Empty, savedItem.RevisionGuid);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Saving_Unedited_ActionItem_Read_From_File_Without_Revision_Assigns_NonEmpty_RevisionGuid()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var fileName = Path.Combine(tempDirectory.FullName, "todos.txt");
            var id = Guid.NewGuid();
            File.WriteAllText(fileName, $"@inbox{Environment.NewLine}\tLegacy item{Environment.NewLine}\t\t#id:{id}");

            var repository = new TodoRepository(
                new ActionItemDiskMapper(fileName),
                tempDirectory.FullName,
                Environment.MachineName);
            var service = new TodoService(repository);

            Assert.Equal(Guid.Empty, service.Find(id).RevisionGuid);

            service.Save();

            var savedItem = new TodoRepository(
                    new ActionItemDiskMapper(fileName),
                    tempDirectory.FullName,
                    Environment.MachineName)
                .FindAll()
                .Single();

            Assert.NotEqual(Guid.Empty, savedItem.RevisionGuid);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
