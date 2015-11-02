using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;
using System.ComponentModel;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using TAS.Common;

namespace TAS.Client.ViewModels
{
    public class MediaViewViewmodel: ViewmodelBase
    {
        public readonly Media Media;
        public MediaViewViewmodel(Media media)
        {
            Media = media;
            Media.PropertyChanged += OnMediaPropertyChanged;
            if (Media is PersistentMedia)
            {
                (Media as PersistentMedia).MediaSegments.CollectionOperation += new EventHandler<CollectionOperationEventArgs<MediaSegment>>(_mediaSegmentsCollectionOperation);
                foreach (MediaSegment ms in (Media as PersistentMedia).MediaSegments)
                    _mediaSegments.Add(new MediaSegmentViewmodel((Media as PersistentMedia), ms));
            }
        }

        protected override void OnDispose()
        {
            Media.PropertyChanged -= OnMediaPropertyChanged;
            if (_mediaSegments != null && Media is PersistentMedia)
                (Media as PersistentMedia).MediaSegments.CollectionOperation -= new EventHandler<CollectionOperationEventArgs<MediaSegment>>(_mediaSegmentsCollectionOperation);
        }

        public string MediaName { get { return Media.MediaName; } }
        public string FileName { get { return Media.FileName; } }
        public string Location { get { return Media.Directory.DirectoryName; } }
        public TimeSpan TCStart { get { return Media.TCStart; } }
        public TimeSpan TCPlay { get { return Media.TCPlay; } }
        public TimeSpan Duration { get { return Media.Duration; } }
        public TimeSpan DurationPlay { get { return Media.DurationPlay; } }
        public DateTime LastUpdated { get { return Media.LastUpdated.ToLocalTime(); } }
        public TMediaCategory MediaCategory { get { return Media.MediaCategory; } }
        public TMediaStatus MediaStatus { get { return Media.MediaStatus; } }
        public TMediaEmphasis MediaEmphasis { get { return (Media is PersistentMedia) ? (Media as PersistentMedia).MediaEmphasis : TMediaEmphasis.None; } }
        public int SegmentCount { get { return (Media is PersistentMedia) ? (Media as PersistentMedia).MediaSegments.Count : 0; } }
        public bool HasSegments { get { return SegmentCount != 0; } }
        public bool IsTrimmed { get { return TCPlay != TCStart || Duration != DurationPlay; } }
        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    if (!value)
                        SelectedSegment = null;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        private ObservableCollection<MediaSegmentViewmodel> _mediaSegments = new ObservableCollection<MediaSegmentViewmodel>();

        public ObservableCollection<MediaSegmentViewmodel> MediaSegments { get { return _mediaSegments; } }

        private void _mediaSegmentsCollectionOperation(object o, CollectionOperationEventArgs<MediaSegment> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (e.Operation == TCollectionOperation.Insert)
                        _mediaSegments.Add(new MediaSegmentViewmodel((Media as PersistentMedia), e.Item));
                    if (e.Operation == TCollectionOperation.Remove)
                    {
                        var segment = _mediaSegments.FirstOrDefault(ms => ms.MediaSegment == e.Item);
                        if (segment != null)
                            _mediaSegments.Remove(segment);
                    }
                    NotifyPropertyChanged("HasSegments");
                    if ((Media is PersistentMedia) && (Media as PersistentMedia).MediaSegments.Count == 0)
                        IsExpanded = false;
                }));
        }

        private MediaSegmentViewmodel _selectedSegment;
        public MediaSegmentViewmodel SelectedSegment
        {
            get { return _selectedSegment; }
            set
            {
                if (_selectedSegment != value)
                {
                    _selectedSegment = value;
                    NotifyPropertyChanged("SelectedSegment");
                }
            }
        }

        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName) && GetType().GetProperty(e.PropertyName) != null)
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == "TCPlay"
                || e.PropertyName == "TCStart"
                || e.PropertyName == "Duration"
                || e.PropertyName == "DurationPlay")
                NotifyPropertyChanged("IsTrimmed");
        }

        public override string ToString()
        {
            return Media.ToString();
        }
    }
}
