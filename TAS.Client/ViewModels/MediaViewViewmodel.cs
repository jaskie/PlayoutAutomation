using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using System.Diagnostics;

namespace TAS.Client.ViewModels
{
    public class MediaViewViewmodel: ViewmodelBase
    {
        public readonly IMedia Media;
        public readonly IMediaManager MediaManager;
        public MediaViewViewmodel(IMedia media, IMediaManager manager)
        {
            Media = media;
            MediaManager = manager;
            Media.PropertyChanged += OnMediaPropertyChanged;
            if (Media is IPersistentMedia)
            {
                (Media as IPersistentMedia).MediaSegments.CollectionOperation += _mediaSegmentsCollectionOperation;
                foreach (IMediaSegment ms in (Media as IPersistentMedia).MediaSegments)
                    _mediaSegments.Add(new MediaSegmentViewmodel((Media as IPersistentMedia), ms));
            }
        }

        protected override void OnDispose()
        {
            Media.PropertyChanged -= OnMediaPropertyChanged;
            if (_mediaSegments != null && Media is IPersistentMedia)
                (Media as IPersistentMedia).MediaSegments.CollectionOperation -= _mediaSegmentsCollectionOperation;
        }

#if DEBUG
        /// <summary>
        /// Useful for ensuring that ViewModel objects are properly garbage collected.
        /// </summary>
        ~MediaViewViewmodel()
        {
            Debug.WriteLine(string.Format("{0} ({1}) ({2}) Finalized", this.GetType().Name, this, this.GetHashCode()));
        }
#endif

        public string MediaName { get { return Media.MediaName; } }
        public string FileName { get { return Media.FileName; } }
        public string Folder { get { return Media.Folder; } }
        public string Location { get { return Media.Directory.DirectoryName; } }
        public TimeSpan TcStart { get { return Media.TcStart; } }
        public TimeSpan TcPlay { get { return Media.TcPlay; } }
        public TimeSpan Duration { get { return Media.Duration; } }
        public TimeSpan DurationPlay { get { return Media.DurationPlay; } }
        public string sTcStart { get { return Media.TcStart.ToSMPTETimecodeString(Media.FrameRate); } }
        public string sTcPlay { get { return Media.TcPlay.ToSMPTETimecodeString(Media.FrameRate); } }
        public string sDuration { get { return Media.Duration.ToSMPTETimecodeString(Media.FrameRate); } }
        public string sDurationPlay { get { return Media.DurationPlay.ToSMPTETimecodeString(Media.FrameRate); } }
        public DateTime LastUpdated { get { return Media.LastUpdated.ToLocalTime(); } }
        public TMediaCategory MediaCategory { get { return Media.MediaType == TMediaType.Movie ? Media.MediaCategory : TMediaCategory.Uncategorized; } }
        public TMediaStatus MediaStatus { get { return Media.MediaStatus; } }
        public TMediaEmphasis MediaEmphasis { get { return (Media is IPersistentMedia) ? (Media as IPersistentMedia).MediaEmphasis : TMediaEmphasis.None; } }
        public int SegmentCount { get { return (Media is IPersistentMedia) ? (Media as IPersistentMedia).MediaSegments.Count : 0; } }
        public bool HasSegments { get { return SegmentCount != 0; } }
        public bool IsTrimmed { get { return TcPlay != TcStart || Duration != DurationPlay; } }
        public bool IsArchived { get { return Media is IServerMedia ? ((IServerMedia)Media).IsArchived : false; } }
        public string ClipNr { get { return (Media as IXdcamMedia)?.ClipNr > 0  ? $"{(Media as IXdcamMedia).ClipNr}/{(Media.Directory as IIngestDirectory)?.XdcamClipCount}" : string.Empty; } }
        public TIngestStatus IngestStatus { get { return Media is IIngestMedia ? ((IIngestMedia)Media).IngestStatus : Media is IArchiveMedia ? ((IArchiveMedia)Media).IngestStatus : TIngestStatus.NotReady; } }
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
                    NotifyPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private ObservableCollection<MediaSegmentViewmodel> _mediaSegments = new ObservableCollection<MediaSegmentViewmodel>();

        public ObservableCollection<MediaSegmentViewmodel> MediaSegments { get { return _mediaSegments; } }

        private void _mediaSegmentsCollectionOperation(object o, CollectionOperationEventArgs<IMediaSegment> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (e.Operation == TCollectionOperation.Insert)
                        _mediaSegments.Add(new MediaSegmentViewmodel((Media as IPersistentMedia), e.Item));
                    if (e.Operation == TCollectionOperation.Remove)
                    {
                        var segment = _mediaSegments.FirstOrDefault(ms => ms.MediaSegment == e.Item);
                        if (segment != null)
                            _mediaSegments.Remove(segment);
                    }
                    NotifyPropertyChanged(nameof(HasSegments));
                    if ((Media is IPersistentMedia) && (Media as IPersistentMedia).MediaSegments.Count == 0)
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
                    NotifyPropertyChanged(nameof(SelectedSegment));
                }
            }
        }

        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName) && GetType().GetProperty(e.PropertyName) != null)
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IMedia.TcPlay)
                || e.PropertyName == nameof(IMedia.TcStart)
                || e.PropertyName == nameof(IMedia.Duration)
                || e.PropertyName == nameof(IMedia.DurationPlay))
                NotifyPropertyChanged(nameof(IsTrimmed));
            if (e.PropertyName == nameof(IMedia.TcStart))
                NotifyPropertyChanged(nameof(sTcStart));
            if (e.PropertyName == nameof(IMedia.TcPlay))
                NotifyPropertyChanged(nameof(sTcPlay));
            if (e.PropertyName == nameof(IMedia.Duration))
                NotifyPropertyChanged(nameof(sDuration));
            if (e.PropertyName == nameof(IMedia.DurationPlay))
                NotifyPropertyChanged(nameof(sDurationPlay));
            if (e.PropertyName == nameof(IIngestMedia.IngestStatus)
                || e.PropertyName == nameof(IArchiveMedia.IngestStatus))
                NotifyPropertyChanged(nameof(IngestStatus));
            if (e.PropertyName == nameof(IMedia.FrameRate))
            {
                NotifyPropertyChanged(nameof(sTcPlay));
                NotifyPropertyChanged(nameof(sTcStart));
                NotifyPropertyChanged(nameof(sDuration));
                NotifyPropertyChanged(nameof(sDurationPlay));
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                   {
                       foreach (MediaSegmentViewmodel segment in _mediaSegments)
                           segment.FrameRate = ((IMedia)media).FrameRate;
                   }));
            }
        }

        public override string ToString()
        {
            return Media.ToString();
        }
    }
}
