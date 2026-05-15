namespace AssimilationSoftware.TodoSort.WpfGui.Properties
{
    public interface ISettings
    {
        string[] RecentFiles { get; set; }

        bool MaskNsfwItems { get; set; }
        
        string Todo { get; set; }

        void Save();
    }
}
