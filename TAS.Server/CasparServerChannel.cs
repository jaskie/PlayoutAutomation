#undef DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Svt.Caspar;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using TAS.Common;
using TAS.Server.Interfaces;
using System.Xml.Serialization;
using System.ComponentModel;

namespace TAS.Server
{
    public class CasparServerChannel : IPlayoutServerChannel
    {
        [XmlIgnore]
        public IPlayoutServer OwnerServer { get; set; }
        #region IPlayoutServerChannel
        public string ChannelName { get; set; }
        public int ChannelNumber { get; set; }
        [DefaultValue(1.0d)]
        public decimal MasterVolume { get; set; }
        public string LiveDevice { get; set; }
        #endregion // IPlayoutServerChannel
        protected bool? outputAspectNarrow;
        [XmlIgnore]
        public IEngine Engine { get; set; }

        private Channel _casparChannel;
        internal Channel CasparChannel
        {
            set
            {
                if (_casparChannel != value)
                    _casparChannel = value;
            }
        }

        private bool _checkConnected()
        {
            var server = OwnerServer;
            var channel = _casparChannel;
            if (server != null && channel != null)
                return server.IsConnected;
            return false;
        }

        protected TVideoFormat _getFormat()
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
                switch (channel.VideoMode)
                {
                    case VideoMode.PAL:
                        return TVideoFormat.PAL_FHA;
                    case VideoMode.NTSC:
                        return TVideoFormat.NTSC;
                    case VideoMode.HD720p5000:
                        return TVideoFormat.HD720p5000;
                    case VideoMode.HD1080i5000:
                        return TVideoFormat.HD1080i5000;
                    default:
                        return TVideoFormat.Other;
                }
            return TVideoFormat.Other;
        }


        public void Initialize()
        {
            lock (this)
            {
                var channel = _casparChannel;
                if (channel != null
                    && OwnerServer != null
                    && OwnerServer.IsConnected)
                    channel.CustomCommand(string.Format(CultureInfo.InvariantCulture, "MIXER {0} MASTERVOLUME {1:F3}", ChannelNumber, MasterVolume));
            }
        }

        public event VolumeChangeNotifier OnVolumeChanged;

        private CasparItem _getItem(IEvent aEvent)
        {
            CasparItem item = new CasparItem(string.Empty);
            IServerMedia media = (aEvent.Engine.PlayoutChannelPRI == this) ? aEvent.ServerMediaPRI : aEvent.ServerMediaSEC;
            if (aEvent.EventType == TEventType.Live || media != null)
            {
                if (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.StillImage)
                {
                    item.Clipname = "\"" + Path.GetFileNameWithoutExtension(media.FileName) + "\"" +
                        (media.MediaType == TMediaType.Movie ? " CHANNEL_LAYOUT STEREO" : string.Empty);
                }
                if (aEvent.EventType == TEventType.Live)
                    item.Clipname = LiveDevice ?? "BLACK";
                item.VideoLayer = (int)aEvent.Layer;
                item.Loop = false;
                item.Transition.Duration = (int)(aEvent.TransitionTime.Ticks / Engine.FrameTicks);
                item.Seek = (int)aEvent.MediaSeek;
                item.Transition.Type = (Svt.Caspar.TransitionType)aEvent.TransitionType;
                return item;
            }
            else
                return null;
        }

        private CasparItem _getItem(IServerMedia media, VideoLayer videolayer, long seek)
        {
            if (media != null)
            {
                CasparItem item = new CasparItem(string.Empty);
                if (media.MediaType == TMediaType.Movie || media.MediaType == TMediaType.Movie)
                    item.Clipname = "\"" + Path.GetFileNameWithoutExtension(media.FileName) + "\"" +
                        ((media.MediaType == TMediaType.Movie && media.HasExtraLines) ? " FILTER CROP=720:576:0:32" : string.Empty);
                item.VideoLayer = (int)videolayer;
                item.Seek = (int)seek;
                return item;
            }
            else
                return null;
        }

        private CasparCGDataCollection GetContainerData(Template template)
        {
            var data =  new CasparCGDataCollection();
            foreach (var field in template.TemplateFields)
                data.DataPairs.Add(new CGDataPair(field.Key, new CGTextFieldData(field.Value)));
            return data;
        }

        public bool LoadNext(IEvent aEvent)
        {
            var channel = _casparChannel;
            if (aEvent != null && _checkConnected() && channel != null)
            {
                if (aEvent.EventType == TEventType.Live || aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.StillImage)
                {
                    CasparItem item = _getItem(aEvent);
                    if (item != null)
                    {
                        channel.LoadBG(item);
                        Debug.WriteLine(aEvent, "CasparLoadNext: ");
                        return true;
                    }
                }
                //if (aEvent.EventType == TEventType.AnimationFlash)
                //{
                //    var template = aEvent.Template;
                //    var media = aEvent.Media;
                //    if (template != null && media != null)
                //    {
                //        channel.CG.Add((int)aEvent.Layer, template.Layer, media.FileName, false, GetContainerData(template));
                //    }
                //}
            }
            Debug.WriteLine(aEvent, "LoadNext did not load: ");
            return false;
        }

        public bool Load(IEvent aEvent)
        {
            var channel = _casparChannel;
            if (aEvent != null && channel != null && _checkConnected())
            {
                if (aEvent.EventType == TEventType.Live || aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.StillImage)
                {
                    CasparItem item = _getItem(aEvent);
                    if (item != null)
                    {
                        channel.Load(item);
                        Debug.WriteLine(aEvent, "CasparLoad: ");
                        IMedia m = aEvent.Media;
                        return true;
                    }
                }
                //if (aEvent.EventType == TEventType.AnimationFlash)
                //{
                //    var template = aEvent.Template;
                //    var media = aEvent.Media;
                //    if (template != null && media != null)
                //    {
                //        channel.CG.Add((int)aEvent.Layer, template.Layer, media.FileName, false, GetContainerData(template));
                //        return true;
                //    }
                //}
            }
            Debug.WriteLine(aEvent, "CasparLoad did not load: ");
            return false;
        }

        public bool Load(IServerMedia media, VideoLayer videolayer, long seek, long duration)
        {
            var channel = _casparChannel;
            if (_checkConnected() 
                && media != null 
                && channel != null)
            {
                CasparItem item = _getItem(media, videolayer, seek);
                if (item != null)
                {
                    item.Length = (int)duration;
                    channel.Load(item);
                    Debug.WriteLine("CasparLoad media {0} Layer {1} Seek {2}", media, videolayer, seek);
                    return true;
                }
            }
            return false;
        }

        public bool Load(System.Drawing.Color color, VideoLayer videolayer)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                var scolor = '#' + color.ToArgb().ToString("X8");
                CasparItem item = new CasparItem((int)videolayer, scolor);
                channel.Load(item);
                Debug.WriteLine("CasparLoad color {0} Layer {1}", scolor, videolayer);
                return true;
            }
            return false;
        }
        

        public bool Seek(VideoLayer videolayer, long position)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                channel.CustomCommand(string.Format("CALL {0}-{1} SEEK {2}", ChannelNumber, (int)videolayer, position));
                Debug.WriteLine("CasparSeek Channel {0} Layer {1} Position {2}", ChannelNumber, (int)videolayer, position);
                return true;
            }
            return false;
        }

        public bool Play(IEvent aEvent)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                if (aEvent.EventType != TEventType.AnimationFlash)
                {
                    IMedia m = aEvent.Media;
                    if (aEvent.EventType == TEventType.Live || m != null)
                        channel.Play((int)aEvent.Layer);
                }
                else
                    channel.CG.Play((int)aEvent.Layer);
                Debug.WriteLine(aEvent, string.Format("CasparPlay Layer {0}", aEvent.Layer));
                return true;
            }
            return false;
        }

        public bool Play(VideoLayer videolayer)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                {
                    channel.Play((int)videolayer);
                    Debug.WriteLine("CasparPlay Layer {0}", videolayer);
                    return true;
                }
            }
            return false;
        }

        public bool Stop(IEvent aEvent)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                {
                    channel.Stop((int)aEvent.Layer);
                    Debug.WriteLine(aEvent, string.Format("CasprarStop {0} layer {1}", aEvent, aEvent.Layer));
                }
                return true;
            }
            else
                return false;
        }

        public bool Stop(VideoLayer videolayer)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                {
                    channel.Stop((int)videolayer);
                    Debug.WriteLine("CasparStop Layer {0}", videolayer);
                    return true;
                }
            }
            return false;
        }

        public bool Pause(IEvent aEvent)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                {
                    channel.CustomCommand(string.Format("PAUSE {0}-{1}", ChannelNumber, (int)aEvent.Layer));
                    Debug.WriteLine(aEvent, string.Format("CasprarPause {0} layer {1}", aEvent, aEvent.Layer));
                }
                return true;
            }
            else
                return false;
        }

        public bool Pause(VideoLayer videolayer)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                {
                    channel.CustomCommand(string.Format("PAUSE {0}-{1}", ChannelNumber, (int)videolayer));
                    Debug.WriteLine("CasparPause Layer {0}", videolayer);
                    return true;
                }
            }
            return false;
        }


        public void ReStart(VideoLayer aVideoLayer)
        {
            Engine engine = Engine as Server.Engine;
            if (engine == null)
                return;
            IEvent ev = engine._visibleEvents[aVideoLayer];
            var channel = _casparChannel;
            if (_checkConnected()
                && ev != null
                && channel != null)
            {
                CasparItem item = _getItem(ev);
                if (item != null)
                {
                    if (ev.EventType == TEventType.Movie && ev.Media != null)
                        item.Seek = (int)ev.Position + (int)((ev.ScheduledTc.Ticks - ev.Media.TcPlay.Ticks) / Engine.FrameTicks);
                    item.Transition.Duration = 3;
                    item.Transition.Type = TransitionType.MIX;
                    channel.LoadBG(item);
                    channel.Play(item.VideoLayer);
                    Debug.WriteLine("CasparChanner.ReStart: restarted {0} from frame {1}", item.Clipname, item.Seek);
                }
                IEvent le;
                engine._loadedNextEvents.TryRemove(aVideoLayer, out le);
                if (le != null)
                {
                    LoadNext(le); // workaround to reload event removed with CasarChanenel.Stop()
                    Debug.WriteLine("CasparChanner.ReStart: reloaded {0}", le.ToString());
                }
            }
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                channel.Clear((int)aVideoLayer);
                Debug.WriteLine(aVideoLayer, "CasparClear");
            }
        }

        public void Clear()
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                channel.Clear();
                channel.ClearMixer((int)VideoLayer.Program);
                outputAspectNarrow = false;
                if (OnVolumeChanged != null)
                    OnVolumeChanged(this, VideoLayer.Program, 1.0m);
                Debug.WriteLine(this, "CasparClear");
            }
        }

        public void SetVolume(VideoLayer videolayer, decimal volume)
        {
            var channel = _casparChannel;
            if (_checkConnected() && channel != null)
            {
                channel.CustomCommand(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} VOLUME {2:F3}", ChannelNumber, (int)videolayer, volume));
                if (OnVolumeChanged != null)
                    OnVolumeChanged(this, videolayer, volume);
            }
        }

        public void SetAspect(VideoLayer layer, bool narrow)
        {
            var channel = _casparChannel;
            var oldAspectNarrow = outputAspectNarrow;
            if (oldAspectNarrow != narrow
                && channel != null
                && _checkConnected())
            {
                outputAspectNarrow = narrow;
                if (narrow)
                    channel.CustomCommand(string.Format("MIXER {0}-{1} FILL 0.125 0 0.75 1 10", ChannelNumber, (int)layer));
                else
                    channel.CustomCommand(string.Format("MIXER {0}-{1} FILL 0 0 1 1 10", ChannelNumber, (int)layer));
                Debug.WriteLine("SetAspect narrow: {0}", narrow);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
