using System.Windows.Input;
using AssimilationSoftware.TodoSort.CoreGui.Model;
using System.Windows;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AssimilationSoftware.TodoSort.CoreGui.ViewModels;

public class AddUrlViewModel : ObservableObject
{
    private string? _url;
    private string? _title;

    private ICommand? _okCommand;
    private ICommand? _fetchTitleCommand;
    public List<ContextViewModel> Contexts { get; }
    private ContextViewModel? _selectedContext;
    private readonly IDialogWindow _view;

    public AddUrlViewModel(MainViewModel api, IDialogWindow window)
    {
        _view = window;
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
            Title = ActionViewModel.FetchWebTitle(_url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Title = _url;
        }
        OnPropertyChanged(nameof(Title));
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
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