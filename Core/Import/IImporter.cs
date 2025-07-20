namespace AssimilationSoftware.TodoSort.Core.Import
{
    public interface IImporter
    {
        Maroon.Model.ActionItem[] GetAllItems();
        bool IsValid { get; }
    }
}
