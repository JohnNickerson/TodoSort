namespace AssimilationSoftware.TodoSort.CoreGui.Interfaces
{
    public interface ISettings
    {
        string[] RecentFiles { get; set; }

        bool MaskNsfwItems { get; set; }
        
        string Todo { get; set; }

        void Save();
    }
}
