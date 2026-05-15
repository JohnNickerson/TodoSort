using System.Windows.Input;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using System.Windows;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AssimilationSoftware.TodoSort.WpfGui.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AssimilationSoftware.TodoSort.WpfGui.ViewModels;

public class AddUrlViewModel : ObservableObject
{
    private string _url;
    private ActionViewModel _sourceItem;

    private ICommand _okCommand;
    private ICommand _fetchTitleCommand;
    public List<ContextViewModel> Contexts { get; }
    private ContextViewModel _selectedContext;
    private readonly IDialogWindow _view;

    public AddUrlViewModel(MainViewModel api, ActionViewModel item, IDialogWindow window)
    {
        _view = window;
        if (item != null)
        {
            item.Tags.TryGetValue("url", out _url);
        }
        _sourceItem = item;
        Contexts = api.Contexts;
        _selectedContext = api.SelectedContext;
    }

    private void OkExecuted()
    {
        _view.DialogResult = true;
        _view.Close();
    }

    private void FetchTitleExecuted()
    {
        try
        {
            _sourceItem.Tags["url"] = _url;
            Title = ActionViewModel.FetchWebTitle(_url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            _sourceItem.Title = _url;
        }
        OnPropertyChanged(nameof(Title));
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

    public ContextViewModel SelectedContext
    {
        get => _selectedContext;
        set
        {
            if (_selectedContext == value) return;
            _selectedContext = value;
            OnPropertyChanged();
        }
    }

    public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(OkExecuted));
    public ICommand FetchTitleCommand => _fetchTitleCommand ?? (_fetchTitleCommand = new RelayCommand(FetchTitleExecuted));
}