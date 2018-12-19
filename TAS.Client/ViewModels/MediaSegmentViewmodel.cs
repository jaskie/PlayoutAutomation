using System;
using System.ComponentModel;
using System.Windows;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.ViewModels
{
    public class MediaSegmentViewmodel: ModifyableViewModelBase
    {
        private TVideoFormat _videoFormat;
        private TimeSpan _tcIn;
        private TimeSpan _tcOut;
        private string _segmentName;

        public MediaSegmentViewmodel(IPersistentMedia media, IMediaSegment mediaSegment)
        {
            MediaSegment = mediaSegment;
            Media = media;
            _videoFormat = media.VideoFormat;
            mediaSegment.PropertyChanged += OnPropertyChanged;
            Load();
        }

        public string SegmentName
        {
            get => _segmentName;
            set => SetField(ref _segmentName, value);
        }
        
        public TimeSpan TcIn
        {
            get => _tcIn;
            set
            {
                if (SetField(ref _tcIn, value))
                    NotifyPropertyChanged(nameof(Duration));
            }
        }

        public TimeSpan TcOut
        {
            get => _tcOut;
            set
            {
                if (SetField(ref _tcOut, value))
                {
                    NotifyPropertyChanged(nameof(Duration));
                }
            }
        }

        public TimeSpan Duration => TcOut - TcIn + Media.FormatDescription().FrameDuration;

        public TVideoFormat VideoFormat
        {
            get => _videoFormat;
            set => SetField(ref _videoFormat, value);
        }

        public IMediaSegment MediaSegment { get; }

        public IPersistentMedia Media { get; }

        public void Load()
        {
            var mediaSegment = MediaSegment;
            if (mediaSegment != null)
            {
                var copiedProperties = GetType().GetProperties();
                foreach (var copyPi in copiedProperties)
                {
                    var sourcePi = mediaSegment.GetType().GetProperty(copyPi.Name);
                    if (sourcePi != null)
                        copyPi.SetValue(this, sourcePi.GetValue(mediaSegment, null), null);
                }
            }
            else // mediaSegment is null
            {
                var zeroedProperties = GetType().GetProperties();
                foreach (var zeroPi in zeroedProperties)
                {
                    var sourcePi = typeof(IMediaSegment).GetProperty(zeroPi.Name);
                    if (sourcePi != null)
                        zeroPi.SetValue(this, null, null);
                }
            }
            IsModified = false;
            NotifyPropertyChanged(null);
        }

        public void Save()
        {
            var mediaSegment = MediaSegment;
            if (IsModified && mediaSegment != null)
            {
                var copiedProperties = GetType().GetProperties();
                foreach (var copyPi in copiedProperties)
                {
                    var destPi = mediaSegment.GetType().GetProperty(copyPi.Name);
                    if (destPi == null)
                        continue;
                    if (destPi.GetValue(mediaSegment, null) != copyPi.GetValue(this, null))
                        destPi.SetValue(mediaSegment, copyPi.GetValue(this, null), null);
                }
                IsModified = false;
                mediaSegment.Save();
            }
        }
       
        public string DisplayName { get; protected set; }

        public bool IsSelected { get; set; }

        protected override void OnDispose()
        {
            MediaSegment.PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnUiThread(() =>
            {
                var sourcePi = MediaSegment.GetType().GetProperty(e.PropertyName);
                var destPi = GetType().GetProperty(e.PropertyName);
                if (sourcePi != null && destPi != null)
                {
                    var oldModified = IsModified;
                    destPi.SetValue(this, sourcePi.GetValue(MediaSegment, null), null);
                    IsModified = oldModified;
                    NotifyPropertyChanged(e.PropertyName);
                }
                NotifyPropertyChanged(e.PropertyName);
            });
        }


    }
}
