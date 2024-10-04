namespace AssimilationSoftware.TodoSort.Sync;

internal sealed partial class Settings
{
    private string _accessCode;
    public string AccessCode
    {
        get => _accessCode;
        set
        {
            _accessCode = value;
        }
    }
}