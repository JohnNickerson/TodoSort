using AssimilationSoftware.TodoSort.WpfGui.ViewModels;

namespace AssimilationSoftware.TodoSort.WpfGui.Model;

public class ItemDialogResult
{
    public string Title { get; set; }
    public string Context { get; set; }
    public bool? DialogResult { get; set; }
    public string Notes { get; internal set; }
    public IEnumerable<TagViewModel> Tags { get; internal set; }
    public Guid? ProjectId { get; internal set; }
    public bool IsDeferred { get; internal set; }
    public DateTime? TickleDate { get; internal set; }
}