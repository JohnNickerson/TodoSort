using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.PimData.Model;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class ActionViewItem : ViewModelBase
    {
        #region Fields

        private RelayCommand _editCommand;
        private RelayCommand _deferCommand;

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

        #region Properties

        #endregion // Properties

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
            }
        }

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

        #endregion // Data Properties (Bindings)

        #region Command Properties

        public ICommand EditCommand
        {
            get { return _editCommand ?? (_editCommand = new RelayCommand(EditExecuted)); }
        }

        public ICommand ToggleDeferCommand => _deferCommand ?? (_deferCommand = new RelayCommand(DeferExecuted));
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
                Source.Notes = new List<string>(new[] { editVm.Notes });
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

        private void DeferExecuted()
        {
            if (Source.Context == "someday")
            {
                Api.Undefer(Source);
            }
            else
            {
                Api.Defer(Source);
            }
        }

        #endregion // Command Handlers
    }
}
