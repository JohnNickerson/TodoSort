using System.IO;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Import;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssimilationSoftware.TodoSort.CoreGui.ViewModels
{
    public class ImportViewModel : ObservableObject
    {
        private readonly ViewModel _vm;
        private readonly IDialogWindow _view;
        private readonly INavigationService _navigationService;
        private string? _fileName;
        private ImportFileType _fileType;
        private string? _targetContext;
        private RelayCommand? _importCommand;
        private RelayCommand? _cancelCommand;
        private RelayCommand? _browseCommand;

        public ImportViewModel(Core.ViewModel vm, IDialogWindow view, INavigationService navigationService)
        {
            _vm = vm;
            _view = view;
            _navigationService = navigationService;
            _targetContext = "import";
        }

        private void ImportExecuted()
        {
            IImporter importer = null;
            switch (FileType)
            {
                case ImportFileType.TodoSort:
                    importer = new TextImporter { Filename = Filename };
                    break;
                case ImportFileType.TodoSortFolder:
                    importer = new TextFolderImporter { Folder = Filename };
                    break;
                case ImportFileType.Instapaper:
                    importer = new InstapaperImporter(Filename);
                    break;
            }
            if (importer != null && importer.IsValid)
            {
                _vm.AddAllItems(TargetContext, true, importer.GetAllItems());
                _view.Close();
            }
        }

        private void BrowseExecuted()
        {
            if (FileType == ImportFileType.TodoSortFolder)
            {
                // Open a folder browse dialogue box.
                var answer = _navigationService.ShowOpenFolderDialog(Filename ?? Directory.GetCurrentDirectory());
                if (answer.DialogResult == true)
                {
                    Filename = answer.SelectedPath;
                }
            }
            else
            {
                // Configure open file dialog box
                var result = _navigationService.ShowImportFileDialog(FileType);
                if (result.DialogResult == true)
                {
                    Filename = result.FileName;
                }
            }
        }

        public string Filename
        {
            get => _fileName;
            set
            {
                if (value == _fileName) return;
                _fileName = value;
                OnPropertyChanged();
            }
        }

        public ImportFileType FileType
        {
            get => _fileType;
            set
            {
                if (value == _fileType) return;
                _fileType = value;
                OnPropertyChanged();
            }
        }

        public string? TargetContext
        {
            get => _targetContext;
            set
            {
                if (value == _targetContext) return;
                _targetContext = value;
                OnPropertyChanged();
            }
        }

        public ICommand ImportCommand => _importCommand ?? (_importCommand = new RelayCommand(ImportExecuted));

        public ICommand CancelCommand => _cancelCommand ?? (_cancelCommand = new RelayCommand(() => _view.Close()));

        public ICommand BrowseCommand => _browseCommand ?? (_browseCommand = new RelayCommand(BrowseExecuted));
    }

    public enum ImportFileType
    {
        TodoSort,
        Instapaper,
        TodoSortFolder,
        Unknown
    }
}
