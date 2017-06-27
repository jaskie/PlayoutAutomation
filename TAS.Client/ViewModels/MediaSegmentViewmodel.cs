using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using TAS.Client.Common;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaSegmentViewmodel: ViewmodelBase
    {
        private readonly IMediaSegment _mediaSegment;
        private TVideoFormat _videoFormat;
        private TimeSpan _tcIn;
        private TimeSpan _tcOut;
        private string _segmentName;

        public MediaSegmentViewmodel(IPersistentMedia media, IMediaSegment mediaSegment)
        {
            _mediaSegment = mediaSegment;
            Media = media;
            _videoFormat = media.VideoFormat;
            mediaSegment.PropertyChanged += OnPropertyChanged;
            Load();
        }

        public string SegmentName
        {
            get { return _segmentName; }
            set { SetField(ref _segmentName, value); }
        }
        
        public TimeSpan TcIn
        {
            get { return _tcIn; }
            set
            {
                if (SetField(ref _tcIn, value))
                    NotifyPropertyChanged(nameof(Duration));
            }
        }

        public TimeSpan TcOut
        {
            get { return _tcOut; }
            set
            {
                if (SetField(ref _tcOut, value))
                {
                    NotifyPropertyChanged(nameof(Duration));
                }
            }
        }

        public TimeSpan Duration
        {
            get { return TcOut - TcIn + Media.FormatDescription().FrameDuration; }
        }

        public TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            set { SetField(ref _videoFormat, value); }
        }

        public IMediaSegment MediaSegment => _mediaSegment;

        public IPersistentMedia Media { get; }

        public void Load()
        {
            var mediaSegment = _mediaSegment;
            if (mediaSegment != null)
            {
                PropertyInfo[] copiedProperties = GetType().GetProperties();
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo sourcePi = mediaSegment.GetType().GetProperty(copyPi.Name);
                    if (sourcePi != null)
                        copyPi.SetValue(this, sourcePi.GetValue(mediaSegment, null), null);
                }
            }
            else // mediaSegment is null
            {
                PropertyInfo[] zeroedProperties = GetType().GetProperties();
                foreach (PropertyInfo zeroPi in zeroedProperties)
                {
                    PropertyInfo sourcePi = typeof(IMediaSegment).GetProperty(zeroPi.Name);
                    if (sourcePi != null)
                        zeroPi.SetValue(this, null, null);
                }
            }
            IsModified = false;
            NotifyPropertyChanged(null);
        }

        public void Save()
        {
            var mediaSegment = _mediaSegment;
            if (IsModified && mediaSegment != null)
            {
                PropertyInfo[] copiedProperties = GetType().GetProperties();
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo destPi = mediaSegment.GetType().GetProperty(copyPi.Name);
                    if (destPi != null)
                    {
                        if (destPi.GetValue(mediaSegment, null) != copyPi.GetValue(this, null))
                            destPi.SetValue(mediaSegment, copyPi.GetValue(this, null), null);
                    }
                }
                IsModified = false;
                mediaSegment.Save();
            }
        }
       
        public string DisplayName { get; protected set; }

        public bool IsSelected { get; set; }

        protected override void OnDispose()
        {
            _mediaSegment.PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate 
            {
                PropertyInfo sourcePi = _mediaSegment.GetType().GetProperty(e.PropertyName);
                PropertyInfo destPi = GetType().GetProperty(e.PropertyName);
                if (sourcePi != null && destPi != null)
                {
                    bool oldModified = IsModified;
                    destPi.SetValue(this, sourcePi.GetValue(_mediaSegment, null), null);
                    IsModified = oldModified;
                    NotifyPropertyChanged(e.PropertyName);
                }
                NotifyPropertyChanged(e.PropertyName);
            });
        }


    }
}
