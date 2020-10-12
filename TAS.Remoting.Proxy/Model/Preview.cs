﻿using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model
{
    public class Preview: ProxyObjectBase, IPreview
    {
#pragma warning disable CS0649
        [DtoMember(nameof(IPreview.Channel))]
        private PlayoutServerChannel _channel;

        [DtoMember(nameof(IPreview.AudioVolume))]
        private double _audioVolume;

        [DtoMember(nameof(IPreview.IsConnected))]
        private bool _isConnected;

        [DtoMember(nameof(IPreview.IsPlaying))]
        private bool _isPlaying;

        [DtoMember(nameof(IPreview.HaveLiveDevice))]
        private bool _haveLiveDevice;

        [DtoMember(nameof(IPreview.IsLivePlaying))]
        private bool _isLivePlaying;

        [DtoMember(nameof(IPreview.IsMovieLoaded))]
        private bool _isMovieLoaded;

        [DtoMember(nameof(IPreview.LoadedMovie))]
        private MediaBase _loadedMovie;

        [DtoMember(nameof(IPreview.MoviePosition))]
        private long _position;

        [DtoMember(nameof(IPreview.MovieSeekOnLoad))]
        private long _movieSeekOnLoad;

        [DtoMember(nameof(IPreview.VideoFormat))]
        private TVideoFormat _videoFormat;

        [DtoMember(nameof(IPreview.LoadedOverlays))]
        private Dictionary<VideoLayer, IMedia> _loadedStillImages;

#pragma warning restore

        public IPlayoutServerChannel Channel => _channel;

        public double AudioVolume
        {
            get => _audioVolume;
            set => Set(value);
        }

        public bool IsConnected => _isConnected;

        public bool IsPlaying => _isPlaying;

        public bool IsLivePlaying => _isLivePlaying;

        public bool HaveLiveDevice => _haveLiveDevice;

        public bool IsMovieLoaded => _isMovieLoaded;

        public void LoadOverlay(IMedia media, VideoLayer layer)
        {
            Invoke(parameters: new object[] {media, layer});
        }

        public bool UnLoadOverlay(VideoLayer layer)
        {
            return Query<bool>(parameters: new object[] {layer});
        }

        public Dictionary<VideoLayer, IMedia> LoadedOverlays => _loadedStillImages;

        public IMedia LoadedMovie => _loadedMovie;

        public long MoviePosition { get => _position; set => Set(value); }

        public long MovieSeekOnLoad => _movieSeekOnLoad;

        public TVideoFormat VideoFormat => _videoFormat;

        public void LoadMovie(IMedia media, long seek, long duration, long position, double audioLevel)
        {
            Invoke(parameters: new object[] { media, seek, duration, position, audioLevel });
        }


        public void Pause() { Invoke(); }

        public void Play() { Invoke(); }

        public void PlayLiveDevice() { Invoke(); }

        public void UnloadMovie() { Invoke(); }

        private event EventHandler<MediaOnLayerEventArgs> _stillImageLoaded;

        public event EventHandler<MediaOnLayerEventArgs> OverlayLoaded
        {
            add
            {
                EventAdd(_stillImageLoaded);
                _stillImageLoaded += value;
            }
            remove
            {
                _stillImageLoaded -= value;
                EventRemove(_stillImageLoaded);
            }
        }

        private event EventHandler<MediaOnLayerEventArgs> _stillImageUnLoaded;
        public event EventHandler<MediaOnLayerEventArgs> OverlayUnLoaded
        {
            add
            {
                EventAdd(_stillImageUnLoaded);
                _stillImageUnLoaded += value;
            }
            remove
            {
                _stillImageUnLoaded -= value;
                EventRemove(_stillImageUnLoaded);
            }
        }


        protected override void OnEventNotification(SocketMessage message)
        {
            switch (message.MemberName)
            {
                case nameof(IPreview.OverlayLoaded):
                    _stillImageLoaded?.Invoke(this, Deserialize<MediaOnLayerEventArgs>(message));
                    break;
                case nameof(IPreview.OverlayUnLoaded):
                    _stillImageUnLoaded?.Invoke(this, Deserialize<MediaOnLayerEventArgs>(message));
                    break;
            }
        }
    }
}
