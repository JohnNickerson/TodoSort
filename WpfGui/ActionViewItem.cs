using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.WpfGui.Annotations;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    public class ActionViewItem : INotifyPropertyChanged
    {
        #region Fields

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion // Fields

        #region Constructors

        public ActionViewItem(ActionItem source, ViewModel api)
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
                    Api.MarkDone(null, Source);
                }
                else
                {
                    Api.Undo("inbox", Source);
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

        public ViewModel Api { get; set; }

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

        #endregion // Data Properties (Bindings)

        #region Command Properties

        #endregion // Command Properties

        #region Command Handlers

        #endregion // Command Handlers

        #region Private Helpers

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // Private Helpers
    }
}
