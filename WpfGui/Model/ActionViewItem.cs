using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
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
        private RelayCommand _bumpCommand;

        #endregion // Fields

        #region Constructors

        public ActionViewItem(ActionItem source, MainViewModel api)
        {
            Source = source;
            Api = api;
        }

        #endregion // Constructors

        #region Methods

        protected override void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            // Dependent properties.
            if (propertyName == nameof(IsDone))
            {
                Api.CheckForContext(this.Source.Context);
            }
        }

        #endregion // Methods

        #region Properties

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
                return false;
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

        public string ToggleDeferTitle => Source.Context == "someday" ? "Undefer" : "Defer Indefinitely";

        public string ToolTip
        {
            get
            {
                var toolTip = new StringBuilder();
                toolTip.AppendLine(Title + (Tags.ContainsKey("type") ? $" [{Tags["type"].ToUpper()}]" : string.Empty));
                foreach (var note in Notes)
                {
                    toolTip.AppendLine(note);
                }

                foreach (var tag in Tags)
                {
                    toolTip.AppendLine(string.Format("#{0}:{1}", tag.Key, tag.Value));
                }

                if (Upvotes > 0)
                {
                    toolTip.AppendLine(string.Format("#upvotes:{0}", Upvotes));
                }

                toolTip.AppendLine(string.Format("#depth:{0}", Source.RankDepth));

                toolTip.AppendLine(string.Format("#ID:{0}", Source.ID));
                if (Source.Project != null)
                {
                    toolTip.AppendLine(string.Format("#project:{0} - {1}", Source.Project.ID,
                        Source.Project.Title));
                }

                return toolTip.ToString().TrimEnd();
            }
        }

        public Visibility CanDefer => Source.Context == "someday" ? Visibility.Collapsed : Visibility.Visible;

        public TimeSpan ShortDeferDelay => new TimeSpan(14, 0, 0, 0);

        public TimeSpan LongDeferDelay => new TimeSpan(60, 0, 0, 0);

        public bool IsInChain => Tags.ContainsKey("order") && (Tags.ContainsKey("series") || Source.Project != null);

        public string TypeIcon
        {
            get
            {
                if (Source.Tags.ContainsKey("type"))
                {
                    switch (Source.Tags["type"])
                    {
                        case "movie":
                            return "Resources/Movie-32.png";
                        case "book":
                            return "Resources/Book-32.png";
                        case "game":
                            return "Resources/Game-Boy-black-48.png";
                        case "tv":
                            return "Resources/TV-256.png";
                        default:
                            return "";
                    }
                }

                return "";
            }
        }
        #endregion

        #region Command Properties

        public ICommand EditCommand => _editCommand ?? (_editCommand = new RelayCommand(EditExecuted));

        public ICommand ToggleDeferCommand => _deferCommand ?? (_deferCommand = new RelayCommand<TimeSpan?>(DeferExecuted));

        public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new RelayCommand(DeleteExecuted));

        public ICommand FixTitleCommand => _fixTitleCommand ?? (_fixTitleCommand = new RelayCommand(FixTitleExecuted));

        public ICommand OpenUrlCommand => _openUrlCommand ?? (_openUrlCommand = new RelayCommand<string>(OpenUrlExecuted, s => !string.IsNullOrEmpty(s)));

        public ICommand BumpCommand => _bumpCommand ?? (_bumpCommand = new RelayCommand(BumpExecuted, () => this.Source.RankDepth > 0));

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
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Url));
            OnPropertyChanged(nameof(UrlNotNull));
            OnPropertyChanged(nameof(IsNsfw));
            OnPropertyChanged(nameof(ItemDate));
            OnPropertyChanged(nameof(ToolTip));
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
            var success = false;
            try
            {
                var client = new WebClient();
                // TODO: Also get the redirected URL, if any.
                var source = client.DownloadString(Source.Tags["url"]);
                var title = Regex.Match(source, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
                if (ValidateTitle(title))
                {
                    Title = title;
                    Api.Update(Source);
                    success = true;
                }
            }
            catch
            {
                success = false;
            }

            if (success) return;
            // 2. If we fail, open the URL and the edit window to fix manually.
            OpenUrlExecuted(Source.Tags["url"]);
            EditExecuted();
        }

        /// <summary>
        /// Checks for a few common failures of URL page titles.
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private bool ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return false;
            switch (title.ToLower())
            {
                case "update your browser | facebook":
                case "youtube":
                    return false;
                default:
                    return true;
            }
        }

        private void OpenUrlExecuted(string url)
        {
            Process.Start(new ProcessStartInfo(url));
        }

        private void BumpExecuted()
        {
            var targetParentDepth = (Source.RankDepth / 2) - 1;
            if (targetParentDepth < 0)
            {
                Source.RankParent = null;
            }
            else
            {
                var newParent = Source.RankParent;
                while (newParent != null && newParent.RankDepth > targetParentDepth && newParent.RankParent != null)
                {
                    newParent = newParent.RankParent;
                }
                Source.RankParent = newParent;
            }
            Api.Update(Source);
            OnPropertyChanged(nameof(ToolTip));
        }
        #endregion // Command Handlers
    }
}
