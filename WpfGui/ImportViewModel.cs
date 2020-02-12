using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Import;
using AssimilationSoftware.TodoSort.WpfGui.Annotations;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    public class ImportViewModel : INotifyPropertyChanged
    {
        private readonly ViewModel _vm;
        private readonly Window _view;
        private string _fileName;
        private ImportFileType _fileType;
        private string _targetContext;
        private RelayCommand _importCommand;
        private RelayCommand _cancelCommand;
        private RelayCommand _browseCommand;
        public event PropertyChangedEventHandler PropertyChanged;

        public ImportViewModel(Core.ViewModel vm, Window view)
        {
            _vm = vm;
            _view = view;
            _targetContext = "import";
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                case ImportFileType.Pocket:
                    importer = new PocketImporter { Filename = Filename };
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
                var fdlg = new System.Windows.Forms.FolderBrowserDialog();
                fdlg.SelectedPath = Filename ?? Directory.GetCurrentDirectory();
                fdlg.ShowNewFolderButton = true;
                fdlg.Description = "Import items source";
                var answer = fdlg.ShowDialog();
                if (answer == System.Windows.Forms.DialogResult.OK)
                {
                    Filename = fdlg.SelectedPath;
                }
            }
            else
            {
                // Configure open file dialog box
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
                {
                    FileName = "Document",
                    DefaultExt = "txt",
                    Filter = "Text documents (.txt)|*.txt|Pocket Exports (.html)|*.html|All documents (*.*)|*.*",
                    Title = "Todo file"
                };
                switch (FileType)
                {
                    case ImportFileType.Pocket:
                        dlg.FilterIndex = 2;
                        break;
                    case ImportFileType.TodoSort:
                        dlg.FilterIndex = 1;
                        break;
                    default:
                        dlg.FilterIndex = 3;
                        break;
                }

                // Show open file dialog box
                var result = dlg.ShowDialog();
                if (result == true)
                {
                    Filename = dlg.FileName;
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

        [CanBeNull]
        public string TargetContext
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
        Pocket,
        TodoSortFolder,
        Unknown
    }
}
