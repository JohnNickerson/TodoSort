using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.Maroon.Model;
using CommunityToolkit.Mvvm.Input;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class ActionViewItem : ViewModelBase
    {
        #region Fields

        private RelayCommand _editCommand;
        private RelayCommand<TimeSpan?> _deferCommand;
        private RelayCommand _deferUntilCommand;
        private RelayCommand _deleteCommand;
        private RelayCommand _fixTitleCommand;
        private RelayCommand _copyUrlCommand;
        private RelayCommand<string> _openUrlCommand;
        private RelayCommand _bumpCommand;
        private RelayCommand<Context> _moveToContextCommand;
        private RelayCommand _maskItemCommand;

        #endregion // Fields

        #region Constructors

        public ActionViewItem(ActionItem source, MainViewModel api)
        {
            Source = source;
            Api = api;
        }

        #endregion // Constructors

        #region Methods

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            // Dependent properties.
            if (propertyName == nameof(Done))
            {
                Api.CheckForContext(this.Source.Context);
            }
        }

        #endregion // Methods

        #region Properties

        public ActionItem Source { get; private set; }

        public bool Done
        {
            get => Source.DoneDate.HasValue;
            set
            {
                if (Source.DoneDate.HasValue == value) return;
                Source.DoneDate = value ? DateTime.Now : (DateTime?)null;
                // Call the API to mark done.
                if (value)
                {
                    Api.MarkDone(Source);
                }
                else
                {
                    Api.Undo(Source);
                }

                RaisePropertyChanged();
            }
        }

        public string Title
        {
            get
            {
                if (Source.Tags != null && Source.Tags.ContainsKey("type"))
                {
                    switch (Source.Tags["type"].ToLower())
                    {
                        case "movie":
                            return "🎬 " + Source.Title;
                        case "book":
                            return "🕮 " + Source.Title;
                        case "tv":
                            return "📺 " + Source.Title;
                        case "game":
                            return "🎮 " + Source.Title;
                        case "video":
                            return "▶️ " + Source.Title;
                        default:
                            return Source.Title;
                    }
                }
                else
                {
                    return Source.Title;
                }
            }
            set
            {
                if (Source.Title == value) return;
                Source.Title = value;
                RaisePropertyChanged();
            }
        }

        public string Url
        {
            get => Source.Tags.ContainsKey("url") ? Source.Tags["url"] : string.Empty;
            set
            {
                if (Source.Tags["url"] == value) return;
                Source.Tags["url"] = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(UrlNotNull));
            }
        }

        public bool UrlNotNull => !string.IsNullOrEmpty(Url);

        private MainViewModel Api { get; }

        public int UpVotes
        {
            get => Source.Upvotes;
            set
            {
                if (Source.Upvotes == value) return;
                Source.Upvotes = value;
                RaisePropertyChanged();
            }
        }

        public bool IsNsfw => Source.Tags.ContainsKey("nsfw");

        public DateTime? DoneDate
        {
            get => Source.DoneDate;
            set
            {
                if (Source.DoneDate == value) return;
                Source.DoneDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ItemDate));
            }
        }

        public DateTime? TickleDate
        {
            get => Source.TickleDate;
            set
            {
                if (Source.TickleDate == value) return;
                Source.TickleDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ItemDate));
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
                    toolTip.AppendLine($"#{tag.Key}:{tag.Value}");
                }

                if (UpVotes > 0)
                {
                    toolTip.AppendLine($"#upvotes:{UpVotes}");
                }

                toolTip.AppendLine($"#depth:{Source.GetRankDepth(Api.Repository)}");

                toolTip.AppendLine($"#ID:{Source.ID}");
                if (Source.ProjectId != null)
                {
                    toolTip.AppendLine($"#project:{Source.GetProject(Api.Repository).Title}");
                }

                return toolTip.ToString().TrimEnd();
            }
        }

        public Visibility CanDefer => Source.Context == "someday" ? Visibility.Collapsed : Visibility.Visible;

        public TimeSpan ShortDeferDelay => new TimeSpan(14, 0, 0, 0);

        public TimeSpan LongDeferDelay => new TimeSpan(60, 0, 0, 0);

        public bool IsInChain => Tags.ContainsKey("order") && (Tags.ContainsKey("series") || Source.ProjectId != null);

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

        public List<Context> AllOtherContexts => Api.SelectedContext.AllOtherContexts;

        public bool CanMoveFrom => Api.SelectedContext.CanMoveFrom;

        #endregion

        #region Command Properties

        public ICommand EditCommand => _editCommand ?? (_editCommand = new RelayCommand(EditExecuted));

        public ICommand ToggleDeferCommand => _deferCommand ?? (_deferCommand = new RelayCommand<TimeSpan?>(DeferExecuted));

        public ICommand DeferUntilCommand => _deferUntilCommand ?? (_deferUntilCommand = new RelayCommand(DeferUntilExecuted));

        public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new RelayCommand(DeleteExecuted));

        public ICommand FixTitleCommand => _fixTitleCommand ?? (_fixTitleCommand = new RelayCommand(FixTitleExecuted, () => UrlNotNull));

        public ICommand CopyUrlCommand => _copyUrlCommand ?? (_copyUrlCommand = new RelayCommand(CopyUrlExecuted, () => UrlNotNull));

        public ICommand OpenUrlCommand => _openUrlCommand ?? (_openUrlCommand = new RelayCommand<string>(OpenUrlExecuted, s => !string.IsNullOrEmpty(s)));

        public ICommand BumpCommand => _bumpCommand ?? (_bumpCommand = new RelayCommand(BumpExecuted, () => this.Source.GetRankDepth(Api.Repository) > 0));

        public ICommand MoveToContextCommand => _moveToContextCommand ?? (_moveToContextCommand = new RelayCommand<Context>(MoveToContextExecuted));

        public ICommand MaskItemCommand => _maskItemCommand ?? (_maskItemCommand = new RelayCommand(MaskItemExecuted));
        #endregion // Command Properties

        #region Command Handlers

        public void EditExecuted()
        {
            // Show the Edit view.
            var editWindow = new EditItemView();
            var editVm = new EditViewModel(Api, this, editWindow);
            editWindow.DataContext = editVm;
            editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            editWindow.Owner = Api.Window;
            var result = editWindow.ShowDialog();
            var contextChanged = false;
            if (result.HasValue && result.Value)
            {
                // Update the source item.
                Title = editVm.Title.Trim();
                Source.Notes = new List<string>();
                foreach (var line in editVm.Notes.Split('\n'))
                {
                    if (line.Trim().Length > 0)
                    {
                        Source.Notes.Add(line);
                    }
                }
                Source.Tags = new Dictionary<string, string>();
                foreach (var tv in editVm.Tags)
                {
                    if (tv?.Tag is not null || tv?.Value is not null)
                    {
                        // Only add non-empty tags, and overwrite any with duplicate keys.
                        Source.Tags[tv.Tag ?? string.Empty] = tv.Value ?? string.Empty;
                    }
                }
                Source.ProjectId = editVm.Project?.ID;
                if (editVm.IsDeferred)
                {
                    Source.TickleDate = editVm.TickleDate;
                }
                if (Source.Context != editVm.Context)
                {
                    Source.Context = editVm.Context;
                    Api.Api.ResetPriorityParents(Source);
                    contextChanged = true;
                }
                Api.Update(Source);
            }
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Url));
            RaisePropertyChanged(nameof(UrlNotNull));
            RaisePropertyChanged(nameof(IsNsfw));
            RaisePropertyChanged(nameof(ItemDate));
            RaisePropertyChanged(nameof(ToolTip));
            RaisePropertyChanged(nameof(Notes));
            RaisePropertyChanged(nameof(Tags));
            if (contextChanged)
                Api.RefreshContexts();
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

        private void DeferUntilExecuted()
        {
            Source.TickleDate = DateTime.Today.AddDays(2);
            Api.Defer(Source);
            EditExecuted();
        }

        private void DeleteExecuted()
        {
            if (System.Windows.MessageBox.Show("Delete this item. Are you sure?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                title = RestoreUnicode(title);
                if (ValidateTitle(title) && Title != title)
                {
                    Title = title;
                    RaisePropertyChanged(nameof(ToolTip));
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

        private void CopyUrlExecuted()
        {
            System.Windows.Clipboard.SetText(Url.Trim());
        }

        private string RestoreUnicode(string title)
        {
            return WebUtility.HtmlDecode(title);
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
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open URL: {ex.Message}");
            }
        }

        private void BumpExecuted()
        {
            var targetParentDepth = (Source.GetRankDepth(Api.Repository) / 2) - 1;
            if (targetParentDepth < 0)
            {
                Source.ParentId = null;
            }
            else
            {
                var newParent = Source.GetParent(Api.Repository);
                while (newParent != null && newParent.GetRankDepth(Api.Repository) > targetParentDepth && newParent.ParentId != null)
                {
                    newParent = newParent.GetParent(Api.Repository);
                }

                if (newParent != null) Source.ParentId = newParent.ID;
            }
            Api.Update(Source);
            RaisePropertyChanged(nameof(ToolTip));
        }

        private void MoveToContextExecuted(Context toContext)
        {
            if (toContext != null)
                Api.Move(Source, toContext.Title);
        }

        private void MaskItemExecuted()
        {
            if (Source.Tags.ContainsKey("nsfw"))
            {
                if (!Api.Masked)
                {
                    // Asked to mask an already-masked item with masking off.
                    Api.Masked = true;
                }
                else
                {
                    Source.Tags.Remove("nsfw");
                }
            }
            else
            {
                Source.Tags["nsfw"] = "true";
                Api.Masked = true;
            }
            Api.Update(Source);
            RaisePropertyChanged(nameof(IsNsfw));
        }
        #endregion // Command Handlers
    }
}
