using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AssimilationSoftware.Maroon.Model;
namespace UnitTests.Scaffolding;

public class MockTodoRepository : AssimilationSoftware.TodoSort.Core.Data.ITodoRepository
{
    private IFileSystem mockFileSystem;
    private List<ActionItem> _somedayItems = new();
    private List<ActionItem> _doneItems = new();
    private List<ActionItem> _items = new();


    public MockTodoRepository(IFileSystem mockFileSystem)
    {
        this.mockFileSystem = mockFileSystem;
    }

    public IEnumerable<ActionItem> SomedayItems => _somedayItems;

    public IEnumerable<ActionItem> DoneItems => _doneItems;

    public IEnumerable<ActionItem> Items => _items;

    public int CommitChanges()
    {
        throw new NotImplementedException();
    }

    public void Create(ActionItem entity)
    {
        if (entity.Done)
            _doneItems.Add(entity);
        else
            _items.Add(entity);

    }

    public void Delete(ActionItem entity)
    {
        throw new NotImplementedException();
    }

    public ActionItem Find(Guid id)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ActionItem> FindAll()
    {
        throw new NotImplementedException();
    }

    public List<PendingChange<ActionItem>> FindConflicts()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ActionItem> GetChildren(ActionItem selected)
    {
        yield break;
    }

    public IEnumerable<string> GetContexts(params string[] exclude)
    {
        throw new NotImplementedException();
    }

    public List<PendingChange<ActionItem>> GetPendingChanges()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ActionItem> GetProjectItems(ActionItem i)
    {
        throw new NotImplementedException();
    }

    public void ResolveByDelete(Guid id)
    {
        throw new NotImplementedException();
    }

    public void ResolveConflict(ActionItem item)
    {
        throw new NotImplementedException();
    }

    public void Revert(Guid id)
    {
        throw new NotImplementedException();
    }

    public void SaveChanges()
    {
        // Do nothing. We are working in memory.
    }

    public void Update(ActionItem entity)
    {
        _items.RemoveAll(i => i.ID == entity.ID);
        if (entity.Done)
        {
            _doneItems.Add(entity);
        }
        else
        {
            _items.Add(entity);
        }
    }
}