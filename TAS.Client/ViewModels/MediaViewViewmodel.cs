//#undef DEBUG
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.ViewModels
{
    public class MediaViewViewmodel: ViewModelBase
    {
        public readonly IMedia Media;
        private readonly Lazy<ObservableCollection<MediaSegmentViewmodel>> _mediaSegments;
        private IMediaSegments _segments;
        private bool _isExpanded;
        private MediaSegmentViewmodel _selectedSegment;

        public MediaViewViewmodel(IMedia media)
        {
            Media = media;
            media.PropertyChanged += OnMediaPropertyChanged;
            if (media is IPersistentMedia pm)
            {
                _mediaSegments = new Lazy<ObservableCollection<MediaSegmentViewmodel>>(() =>
                {
                    _segments = pm.GetMediaSegments();
                    var result = new ObservableCollection<MediaSegmentViewmodel>(_segments.Segments.Select(ms => new MediaSegmentViewmodel(pm, ms)));
                    _segments.SegmentAdded += MediaSegments_SegmentAdded;
                    _segments.SegmentRemoved += _mediaSegments_SegmentRemoved;
                    return result;
                });
            }
        }

        protected override void OnDispose()
        {
            Media.PropertyChanged -= OnMediaPropertyChanged;
            if (_segments != null)
            {
                _segments.SegmentAdded -= MediaSegments_SegmentAdded;
                _segments.SegmentRemoved -= _mediaSegments_SegmentRemoved;
            }
        }

        public string MediaName => Media.MediaName;
        public string FileName => Media.FileName;
        public string Folder => Media.Folder;
        public string Location => Media.Directory.GetDisplayName();
        public TimeSpan TcStart => Media.TcStart;
        public TimeSpan TcPlay => Media.TcPlay;
        public TimeSpan Duration => Media.Duration;
        public TimeSpan DurationPlay => Media.DurationPlay;
        public DateTime LastUpdated => Media.LastUpdated;
        public TMediaCategory MediaCategory => Media.MediaType == TMediaType.Movie ? Media.MediaCategory : TMediaCategory.Uncategorized;
        public TMediaStatus MediaStatus => Media.MediaStatus;
        public TMediaEmphasis MediaEmphasis => (Media as IPersistentMedia)?.MediaEmphasis ?? TMediaEmphasis.None;
        public int SegmentCount => _mediaSegments?.Value.Count ?? 0;
        public bool HasSegments => SegmentCount != 0;
        public bool IsTrimmed => TcPlay != TcStart || Duration != DurationPlay;
        public bool IsArchived => (Media as IServerMedia)?.IsArchived ?? false;
        public bool IsExpired
        {
            get
            {
                var killDate = (Media as IPersistentMedia)?.KillDate;
                if (killDate.HasValue && killDate.Value < DateTime.Today)
                    return true;
                return false;
            }
        }

        public string ClipNr => (Media as IXdcamMedia)?.ClipNr > 0  ? $"{((IXdcamMedia) Media).ClipNr}/{(Media.Directory as IIngestDirectory)?.XdcamClipCount}" : string.Empty;
        public TIngestStatus IngestStatus => (Media as IIngestMedia)?.IngestStatus ?? ((Media as IArchiveMedia)?.IngestStatus ?? TIngestStatus.NotReady);
        public TVideoFormat VideoFormat => Media.VideoFormat;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetField(ref _isExpanded, value))
                {
                    if (!value)
                        SelectedSegment = null;
                }
            }
        }
        public bool IsVerified => Media.IsVerified;
        public MediaSegmentViewmodel SelectedSegment
        {
            get => _selectedSegment;
            set => SetField(ref _selectedSegment, value);
        }
        public ObservableCollection<MediaSegmentViewmodel> MediaSegments => _mediaSegments.Value;

        public override string ToString()
        {
            return Media.ToString();
        }

        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName) && GetType().GetProperty(e.PropertyName) != null)
                NotifyPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(IMedia.TcPlay):
                case nameof(IMedia.TcStart):
                case nameof(IMedia.Duration):
                case nameof(IMedia.DurationPlay):
                    NotifyPropertyChanged(nameof(IsTrimmed));
                    break;
                case nameof(IMedia.VideoFormat):
                    NotifyPropertyChanged(nameof(VideoFormat));
                    if (media is IPersistentMedia && _mediaSegments.IsValueCreated)
                        OnUiThread(() =>
                        {
                            foreach (MediaSegmentViewmodel segment in _mediaSegments.Value)
                                segment.VideoFormat = ((IMedia) media).VideoFormat;
                        });
                    break;
                case nameof(IPersistentMedia.KillDate):
                    NotifyPropertyChanged(nameof(IsExpired));
                    break;
            }
        }

        private void _mediaSegments_SegmentRemoved(object sender, MediaSegmentEventArgs e)
        {
            if (_mediaSegments == null)
                return;
            OnUiThread(() =>
            {
                var segment = _mediaSegments.Value.FirstOrDefault(ms => ms.MediaSegment == e.Segment);
                if (segment != null)
                    _mediaSegments.Value.Remove(segment);
                NotifyPropertyChanged(nameof(HasSegments));
                if (Media is IPersistentMedia && _segments?.Count == 0)
                    IsExpanded = false;
            });
        }

        private void MediaSegments_SegmentAdded(object sender, MediaSegmentEventArgs e)
        {
            if (_mediaSegments == null)
                return;
            OnUiThread(() =>
            {
                _mediaSegments.Value.Add(new MediaSegmentViewmodel(Media as IPersistentMedia, e.Segment));
                NotifyPropertyChanged(nameof(HasSegments));
            });
        }

    }
}
