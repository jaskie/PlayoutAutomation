using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaSegmentViewmodel: ViewmodelBase
    {
        private readonly IMediaSegment _mediaSegment;
        private readonly IPersistentMedia _media;
        private RationalNumber _frameRate;
        public MediaSegmentViewmodel(IPersistentMedia media, IMediaSegment mediaSegment)
        {
            _mediaSegment = mediaSegment;
            _media = media;
            _frameRate = media.FrameRate;
            mediaSegment.PropertyChanged += OnPropertyChanged;
            Load();
        }

        public MediaSegmentViewmodel(IPersistentMedia media) : this(media, media.CreateSegment()) { }

        protected override void OnDispose()
        {
            _mediaSegment.PropertyChanged -= OnPropertyChanged;
        }

        private string _segmentName;
        public string SegmentName
        {
            get { return _segmentName; }
            set { SetField(ref _segmentName, value, nameof(SegmentName)); }
        }
        
        private TimeSpan _tcIn;
        public TimeSpan TcIn
        {
            get { return _tcIn; }
            set
            {
                if (SetField(ref _tcIn, value, nameof(TcIn)))
                {
                    NotifyPropertyChanged(nameof(sTcIn));
                    NotifyPropertyChanged(nameof(Duration));
                    NotifyPropertyChanged(nameof(sDuration));
                }
            }
        }

        private TimeSpan _tcOut;
        public TimeSpan TcOut
        {
            get { return _tcOut; }
            set
            {
                if (SetField(ref _tcOut, value, nameof(TcOut)))
                {
                    NotifyPropertyChanged(nameof(Duration));
                    NotifyPropertyChanged(nameof(sDuration));
                }
            }
        }

        public TimeSpan Duration
        {
            get { return TcOut - TcIn + _media.VideoFormatDescription.FrameDuration; }
        }

        public string sTcIn { get { return _tcIn.ToSMPTETimecodeString(_frameRate); } }
        public string sDuration { get { return Duration.ToSMPTETimecodeString(_frameRate); } }

        public RationalNumber FrameRate
        {
            get { return _frameRate; }
            set
            {
                if (SetField(ref _frameRate, value, nameof(FrameRate)))
                {
                    NotifyPropertyChanged(nameof(sDuration));
                    NotifyPropertyChanged(nameof(sTcIn));
                }
            }
        }

        public IMediaSegment MediaSegment { get { return _mediaSegment; } }
        
        public IPersistentMedia Media { get { return _media; } }

        public void Load()
        {
            var mediaSegment = _mediaSegment;
            if (mediaSegment != null)
            {
                PropertyInfo[] copiedProperties = this.GetType().GetProperties();
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo sourcePi = mediaSegment.GetType().GetProperty(copyPi.Name);
                    if (sourcePi != null)
                        copyPi.SetValue(this, sourcePi.GetValue(mediaSegment, null), null);
                }
            }
            else // mediaSegment is null
            {
                PropertyInfo[] zeroedProperties = this.GetType().GetProperties();
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
                PropertyInfo[] copiedProperties = this.GetType().GetProperties();
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

        public void Delete()
        {
            if (_mediaSegment != null)
                _mediaSegment.Delete();
        }


        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                PropertyInfo sourcePi = _mediaSegment.GetType().GetProperty(e.PropertyName);
                PropertyInfo destPi = this.GetType().GetProperty(e.PropertyName);
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

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (base.SetField(ref field, value, propertyName))
            {
                IsModified = true;
                return true;
            }
            return false;
        }

        private bool _isModified;
        
        [Browsable(false)]
        public bool IsModified
        {
            get { return _isModified; }
            private set
            {
                if (value != _isModified)
                {
                    _isModified = value;
                    NotifyPropertyChanged(nameof(IsModified));
                }
            }
        }

        [Browsable(false)]
        public string DisplayName { get; protected set; }

        public bool IsSelected { get; set; }

    }
}
