namespace AssimilationSoftware.TodoSort.CoreGui.Interfaces
{
    public interface IDialogWindow
    {
        bool? DialogResult { get; set; }
        void Close();
        bool? ShowDialog(ITaskListView parent);
    }
}