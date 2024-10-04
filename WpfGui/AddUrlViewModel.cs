using System.Windows.Input;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using System.Windows;
using System.Diagnostics;

namespace AssimilationSoftware.TodoSort.WpfGui;

public class AddUrlViewModel : ViewModelBase
{
    private string _url;
    private ActionViewItem _sourceItem;

    private ICommand _okCommand;
    private ICommand _fetchTitleCommand;

    public AddUrlViewModel(MainViewModel api, ActionViewItem item, Window window)
    {
        Window = window;
        if (item != null)
        {
            item.Tags.TryGetValue("url", out _url);
        }
        _sourceItem = item;
    }

    private void OkExecuted()
    {
        Window.DialogResult = true;
        Window.Close();
    }

    private void FetchTitleExecuted()
    {
        try
        {
            _sourceItem.FixTitleCommand.Execute(this);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            _sourceItem.Title = _url;
        }
    }

    public string Title
    {
        get => _sourceItem.Title;
        set
        {
            if (_sourceItem.Title == value) return;
            _sourceItem.Title = value;
            OnPropertyChanged();
        }
    }

    public string Url
    {
        get => _url;
        set
        {
            if (_url == value) return;
            _url = value;
            OnPropertyChanged();
        }
    }

    public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(OkExecuted));
    public ICommand FetchTitleCommand => _fetchTitleCommand ?? (_fetchTitleCommand = new RelayCommand(FetchTitleExecuted));
}