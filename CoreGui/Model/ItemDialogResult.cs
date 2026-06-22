using AssimilationSoftware.TodoSort.CoreGui.ViewModels;

namespace AssimilationSoftware.TodoSort.CoreGui.Model;

public class ItemDialogResult
{
    public string? Title { get; set; }
    public string? Context { get; set; }
    public bool? DialogResult { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<TagViewModel>? Tags { get; set; }
    public Guid? ProjectId { get; set; }
    public bool IsSnoozed { get; set; }
    public DateTime? ReturnDate { get; set; }
}