using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.PimData.Model;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class ActionViewItem : ViewModelBase
    {
        #region Fields

        private RelayCommand _editCommand;
        private RelayCommand<TimeSpan?> _deferCommand;
        private RelayCommand _deleteCommand;
        private RelayCommand _fixTitleCommand;
        private RelayCommand<string> _openUrlCommand;

        #endregion // Fields

        #region Constructors

        public ActionViewItem(ActionItem source, MainViewModel api)
        {
            Source = source;
            Api = api;
        }

        #endregion // Constructors

        #region Public Methods

        #endregion // Public Methods

        #region Data Properties (Bindings)

        public ActionItem Source { get; set; }

        public bool IsDone
        {
            get => Source.IsDone;
            set
            {
                if (Source.IsDone == value) return;
                Source.IsDone = value;
                // Call the API to mark done.
                if (value)
                {
                    Api.MarkDone(Source);
                }
                else
                {
                    Api.Undo(Source);
                }

                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => Source.Title;
            set
            {
                if (Source.Title == value) return;
                Source.Title = value;
                OnPropertyChanged();
            }
        }

        public string Url
        {
            get { return Source.Tags.ContainsKey("url") ? Source.Tags["url"] : string.Empty; }
            set
            {
                if (Source.Tags["url"] == value) return;
                Source.Tags["url"] = value;
                OnPropertyChanged();
                OnPropertyChanged("UrlNotNull");
            }
        }

        public bool UrlNotNull => !string.IsNullOrEmpty(Url);

        public MainViewModel Api { get; set; }

        public int Upvotes
        {
            get => Source.Upvotes;
            set
            {
                if (Source.Upvotes == value) return;
                Source.Upvotes = value;
                OnPropertyChanged();
            }
        }

        public bool IsNsfw
        {
            get
            {
                if (Source.Tags.ContainsKey("nsfw"))
                {
                    return Source.Tags["nsfw"].ToLower() == "true";
                }
                else
                {
                    return false;
                }
            }
        }

        public DateTime? DoneDate
        {
            get => Source.DoneDate;
            set
            {
                if (Source.DoneDate == value) return;
                Source.DoneDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemDate));
            }
        }

        public DateTime? TickleDate
        {
            get => Source.TickleDate;
            set
            {
                if (Source.TickleDate == value) return;
                Source.TickleDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemDate));
            }
        }

        public DateTime? ItemDate => DoneDate ?? TickleDate;

        public List<string> Notes => Source.Notes;

        public Dictionary<string, string> Tags => Source.Tags;

        public string ToggleDeferTitle => Source.Context == "someday" ? "Undefer" : "Defer";

        public string ChainSummary
        {
            get
            {
                if (Source.Tags.ContainsKey("order"))
                {
                    if (Source.Project != null)
                    {
                        return $"{Source.Project.Title} - #{Source.Tags["order"]}";
                    }

                    return Source.Tags.ContainsKey("series") ? $"{Source.Tags["series"]} - #{Source.Tags["order"]}" : $"#{Source.Tags["order"]}, fix series info";
                }

                return null;
            }
        }

        public Visibility CanDefer => Source.Context == "someday" ? Visibility.Collapsed : Visibility.Visible;

        public TimeSpan ShortDeferDelay => new TimeSpan(14, 0, 0, 0);

        public TimeSpan LongDeferDelay => new TimeSpan(60, 0, 0, 0);

        #endregion // Data Properties (Bindings)

        #region Command Properties

        public ICommand EditCommand => _editCommand ?? (_editCommand = new RelayCommand(EditExecuted));

        public ICommand ToggleDeferCommand => _deferCommand ?? (_deferCommand = new RelayCommand<TimeSpan?>(DeferExecuted));

        public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new RelayCommand(DeleteExecuted));

        public ICommand FixTitleCommand => _fixTitleCommand ?? (_fixTitleCommand = new RelayCommand(FixTitleExecuted));

        public ICommand OpenUrlCommand => _openUrlCommand ?? (_openUrlCommand = new RelayCommand<string>(OpenUrlExecuted, s => !string.IsNullOrEmpty(s)));
        #endregion // Command Properties

        #region Command Handlers

        private void EditExecuted()
        {
            // Show the Edit view.
            var editWindow = new EditItemView();
            var editVm = new EditViewModel(Api, this, editWindow);
            editWindow.DataContext = editVm;
            editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            editWindow.Owner = Api.Window;
            var result = editWindow.ShowDialog();
            if (result.HasValue && result.Value)
            {
                // Update the source item.
                Title = editVm.Title;
                Source.Notes = new List<string>();
                foreach (var line in editVm.Notes.Split('\n'))
                {
                    if (line.Trim().Length > 0)
                    {
                        Source.Notes.Add(line);
                    }
                }
                Source.Tags = editVm.Tags.ToDictionary(k => k.Tag, v => v.Value);
                Source.Project = editVm.Project;
                if (editVm.IsDeferred)
                {
                    Source.TickleDate = editVm.TickleDate;
                }
                if (Source.Context != editVm.Context)
                {
                    Api.Move(Source, editVm.Context);
                }
                Api.Update(Source);
            }
        }

        private void DeferExecuted(TimeSpan? delay)
        {
            if (Source.Context == "someday")
            {
                Api.Undefer(Source);
            }
            else
            {
                if (delay.HasValue)
                {
                    Source.TickleDate = DateTime.Today.Add(delay.Value);
                }
                Api.Defer(Source);
            }
        }

        private void DeleteExecuted()
        {
            if (MessageBox.Show("Delete this item. Are you sure?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Api.Delete(Source);
            }
        }

        private void FixTitleExecuted()
        {
            // 1. Try to get the title automatically.
            try
            {
                var client = new WebClient();
                // TODO: Also get the redirected URL, if any.
                var source = client.DownloadString(Source.Tags["url"]);
                var title = Regex.Match(source, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
                Title = title;
                Api.Update(Source);
            }
            // 2. If we fail, open the URL and the edit window to fix manually.
            catch
            {
                EditExecuted();
            }
        }

        private void OpenUrlExecuted(string url)
        {
            Process.Start(new ProcessStartInfo(url));
        }
        #endregion // Command Handlers
    }
}
