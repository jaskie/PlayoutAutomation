#undef DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using TAS.Server.Common;
using System.Diagnostics;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaViewViewmodel: ViewmodelBase
    {
        public readonly IMedia Media;
        public MediaViewViewmodel(IMedia media)
        {
            Media = media;
            media.PropertyChanged += OnMediaPropertyChanged;
            IPersistentMedia pm = media as IPersistentMedia;
            if (pm != null)
            {
                _mediaSegments = new Lazy<ObservableCollection<MediaSegmentViewmodel>>(() =>
                {
                    var result = new ObservableCollection<MediaSegmentViewmodel>(pm.MediaSegments.Segments.Select(ms => new MediaSegmentViewmodel(pm, ms)));
                    pm.MediaSegments.SegmentAdded += MediaSegments_SegmentAdded;
                    pm.MediaSegments.SegmentRemoved += _mediaSegments_SegmentRemoved;
                    return result;
                });
            }
        }

        protected override void OnDispose()
        {
            Media.PropertyChanged -= OnMediaPropertyChanged;
            var pm = Media as IPersistentMedia;
            if (pm != null && _mediaSegments.IsValueCreated)
            {
                pm.MediaSegments.SegmentAdded -= MediaSegments_SegmentAdded;
                pm.MediaSegments.SegmentRemoved -= _mediaSegments_SegmentRemoved;
            }
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

        private void _mediaSegments_SegmentRemoved(object sender, MediaSegmentEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                var segment = _mediaSegments.Value.FirstOrDefault(ms => ms.MediaSegment == e.Segment);
                if (segment != null)
                    _mediaSegments.Value.Remove(segment);
                NotifyPropertyChanged(nameof(HasSegments));
                if ((Media is IPersistentMedia) && (Media as IPersistentMedia).MediaSegments.Count == 0)
                    IsExpanded = false;
            }));
        }

        private void MediaSegments_SegmentAdded(object sender, MediaSegmentEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                _mediaSegments.Value.Add(new MediaSegmentViewmodel((Media as IPersistentMedia), e.Segment));
                NotifyPropertyChanged(nameof(HasSegments));
            }));
        }
        
        public string MediaName => Media.MediaName;
        public string FileName => Media.FileName;
        public string Folder => Media.Folder;
        public string Location => Media.Directory.DirectoryName;
        public TimeSpan TcStart => Media.TcStart;
        public TimeSpan TcPlay => Media.TcPlay;
        public TimeSpan Duration => Media.Duration;
        public TimeSpan DurationPlay => Media.DurationPlay;
        public string sTcStart => Media.IsVerified ? Media.TcStart.ToSMPTETimecodeString(Media.FrameRate()) : string.Empty;
        public string sTcPlay => Media.IsVerified ? Media.TcPlay.ToSMPTETimecodeString(Media.FrameRate()) :string.Empty;
        public string sDuration => Media.Duration != TimeSpan.Zero ? Media.Duration.ToSMPTETimecodeString(Media.FrameRate()): string.Empty;
        public string sDurationPlay => Media.DurationPlay != TimeSpan.Zero ? Media.DurationPlay.ToSMPTETimecodeString(Media.FrameRate()): string.Empty;
        public DateTime LastUpdated => Media.LastUpdated;
        public TMediaCategory MediaCategory => Media.MediaType == TMediaType.Movie ? Media.MediaCategory : TMediaCategory.Uncategorized;
        public TMediaStatus MediaStatus => Media.MediaStatus;
        public TMediaEmphasis MediaEmphasis => (Media is IPersistentMedia) ? (Media as IPersistentMedia).MediaEmphasis : TMediaEmphasis.None;
        public int SegmentCount => (Media is IPersistentMedia) ? (Media as IPersistentMedia).MediaSegments.Count : 0;
        public bool HasSegments => SegmentCount != 0;
        public bool IsTrimmed => TcPlay != TcStart || Duration != DurationPlay;
        public bool IsArchived => Media is IServerMedia ? ((IServerMedia)Media).IsArchived : false;
        public string ClipNr => (Media as IXdcamMedia)?.ClipNr > 0  ? $"{(Media as IXdcamMedia).ClipNr}/{(Media.Directory as IIngestDirectory)?.XdcamClipCount}" : string.Empty;
        public TIngestStatus IngestStatus => Media is IIngestMedia ? ((IIngestMedia)Media).IngestStatus : Media is IArchiveMedia ? ((IArchiveMedia)Media).IngestStatus : TIngestStatus.NotReady;
        public TVideoFormat VideoFormat => Media.VideoFormat;
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
        public bool IsVerified => Media.IsVerified;

        private readonly Lazy<ObservableCollection<MediaSegmentViewmodel>> _mediaSegments;

        public ObservableCollection<MediaSegmentViewmodel> MediaSegments { get { return _mediaSegments.Value; } }

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
            if (e.PropertyName == nameof(IMedia.VideoFormat))
            {
                NotifyPropertyChanged(nameof(VideoFormat));
                NotifyPropertyChanged(nameof(sTcPlay));
                NotifyPropertyChanged(nameof(sTcStart));
                NotifyPropertyChanged(nameof(sDuration));
                NotifyPropertyChanged(nameof(sDurationPlay));
                if (media is IPersistentMedia && _mediaSegments.IsValueCreated)
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                       {
                           foreach (MediaSegmentViewmodel segment in _mediaSegments.Value)
                               segment.VideoFormat = ((IMedia)media).VideoFormat;
                       }));
            }
            if (e.PropertyName == nameof(IMedia.IsVerified))
            {
                NotifyPropertyChanged(nameof(sTcPlay));
                NotifyPropertyChanged(nameof(sTcStart));
            }
        }

        public override string ToString()
        {
            return Media.ToString();
        }
    }
}
