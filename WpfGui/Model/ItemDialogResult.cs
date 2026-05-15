using AssimilationSoftware.TodoSort.WpfGui.ViewModels;

namespace AssimilationSoftware.TodoSort.WpfGui.Model;

public class ItemDialogResult
{
    public string Title { get; set; }
    public string Context { get; set; }
    public bool? DialogResult { get; set; }
    public string Notes { get; set; }
    public IEnumerable<TagViewModel> Tags { get; set; }
    public Guid? ProjectId { get; set; }
    public bool IsDeferred { get; set; }
    public DateTime? TickleDate { get; set; }
}