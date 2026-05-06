namespace AssimilationSoftware.TodoSort.WpfGui.Interfaces
{
    public interface IDialogWindow
    {
        bool? DialogResult { get; set; }
        void Close();
    }
}