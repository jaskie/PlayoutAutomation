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
using TAS.Remoting.Server;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using TAS.Server.Common;
using System.Threading;
using System.Text.RegularExpressions;

namespace TAS.Server
{
    public class CasparServerChannel : DtoBase, IPlayoutServerChannel
    {
        internal CasparServer ownerServer;
        [XmlIgnore]
        public IPlayoutServer OwnerServer { get { return ownerServer; } }
        #region IPlayoutServerChannel
        public int Id { get; set; }
        public int ChannelNumber { get { return Id; } set { Id = value; } } // old field name; to avoid problems after field rename after version 1.1.1
        [JsonProperty]
        public string ChannelName { get; set; }
        [DefaultValue(typeof(Decimal), "1")]
        public decimal MasterVolume { get; set; }
        public string LiveDevice { get; set; }
        [XmlIgnore]
        public TVideoFormat VideoFormat { get; set; }
        #endregion // IPlayoutServerChannel
        protected SimpleDictionary<VideoLayer, bool> outputAspectNarrow = new SimpleDictionary<VideoLayer, bool>();

        private Channel _casparChannel;
        internal Channel CasparChannel
        {
            set
            {
                if (_casparChannel != value)
                {
                    _casparChannel = value;
                    VideoFormat = CasparModeToVideoFormat(_casparChannel.VideoMode);
                }
            }
        }

        private bool _checkConnected(Channel channel)
        {
            var server = OwnerServer;
            if (server != null && channel != null)
                return server.IsConnected;
            return false;
        }

        public void Initialize()
        {
            if (_checkConnected(_casparChannel))
            {
                ClearMixer();
                _casparChannel.MasterVolume((float)MasterVolume);
            }
        }

        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;

        #region Utilites

        private CasparItem _getItem(Event aEvent)
        {
            CasparItem item = new CasparItem(string.Empty);
            IPersistentMedia media = (aEvent.Engine.PlayoutChannelPRI == this) ? aEvent.ServerMediaPRI : aEvent.ServerMediaSEC;
            if (aEvent.EventType == TEventType.Live || media != null)
            {
                if (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.StillImage)
                    item.Clipname = string.Format($"\"{Path.GetFileNameWithoutExtension(media.FileName)}\"");
                if (aEvent.EventType == TEventType.Live)
                    item.Clipname = string.IsNullOrWhiteSpace(LiveDevice) ? "BLACK" : LiveDevice;
                if (aEvent.EventType == TEventType.Live || aEvent.EventType == TEventType.Movie)
                    item.ChannelLayout = ChannelLayout.Stereo;
                if (aEvent.EventType == TEventType.Movie)
                    item.FieldOrderInverted = media.FieldOrderInverted;
                item.VideoLayer = (int)aEvent.Layer;
                item.Loop = false;
                item.Transition.Type = (Svt.Caspar.TransitionType)aEvent.TransitionType;
                item.Transition.Duration = (int)((aEvent.TransitionTime.Ticks - aEvent.TransitionPauseTime.Ticks) / aEvent.Engine.FrameTicks);
                item.Transition.Pause = (int)(aEvent.TransitionPauseTime.Ticks / aEvent.Engine.FrameTicks);
                item.Transition.Easing = (Easing)aEvent.TransitionEasing; 
                item.Seek = (int)aEvent.MediaSeek;
                return item;
            }
            return null;
        }

        private CasparItem _getItem(Media media, VideoLayer videolayer, long seek)
        {
            if (media != null && media.MediaType == TMediaType.Movie)
            {
                CasparItem item = new CasparItem(string.Empty);
                item.Clipname = $"\"{(media is ServerMedia ? Path.GetFileNameWithoutExtension(media.FileName) : media.FullPath)}\"";
                item.ChannelLayout = ChannelLayout.Stereo;                 
                item.VideoLayer = (int)videolayer;
                item.Seek = (int)seek;
                item.FieldOrderInverted = media.FieldOrderInverted;
                return item;
            }
            else
                return null;
        }
                
        private CasparCGDataCollection _getContainerData(ITemplated template)
        {
            var data =  new CasparCGDataCollection();
            foreach (var field in template.Fields)
                data.DataPairs.Add(new CGDataPair(field.Key, new CGTextFieldData(field.Value)));
            return data;
        }

        #endregion // Utilities

        private readonly ConcurrentDictionary<VideoLayer, Event> _loadedNext = new ConcurrentDictionary<VideoLayer, Event>();
        private readonly ConcurrentDictionary<VideoLayer, Event> _visible = new ConcurrentDictionary<VideoLayer, Event>();

        #region IPlayoutServerChannel

        public bool LoadNext(Event aEvent)
        {
            var channel = _casparChannel;
            if (aEvent != null && _checkConnected(channel))
            {
                var eventType = aEvent.EventType;
                if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
                {
                    CasparItem item = _getItem(aEvent);
                    if (item != null)
                    {
                        channel.LoadBG(item);
                        _loadedNext[aEvent.Layer] = aEvent;
                        Debug.WriteLine(aEvent, "CasparLoadNext: ");
                        return true;
                    }
                }
            }
            Debug.WriteLine(aEvent, "LoadNext did not load: ");
            return false;
        }

        public bool Load(Event aEvent)
        {
            var channel = _casparChannel;
            if (aEvent != null && _checkConnected(channel))
            {
                var eventType = aEvent.EventType;
                if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
                {
                    CasparItem item = _getItem(aEvent);
                    if (item != null)
                    {
                        channel.Load(item);
                        if (eventType == TEventType.Live)
                            channel.Play(item.VideoLayer);
                        _visible[aEvent.Layer] = aEvent;
                        Event removed;
                        _loadedNext.TryRemove(aEvent.Layer, out removed);
                        Debug.WriteLine(aEvent, "CasparLoad: ");
                        return true;
                    }
                }
            }
            Debug.WriteLine(aEvent, "CasparLoad did not load: ");
            return false;
        }

        public bool Load(Media media, VideoLayer videolayer, long seek, long duration)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel) 
                && media != null)
            {
                CasparItem item = _getItem(media, videolayer, seek);
                if (item != null)
                {
                    item.Length = (int)duration;
                    channel.Load(item);
                    Event removed;
                    _visible.TryRemove(videolayer, out removed);
                    _loadedNext.TryRemove(videolayer, out removed);
                    Debug.WriteLine("CasparLoad media {0} Layer {1} Seek {2}", media, videolayer, seek);
                    return true;
                }
            }
            return false;
        }

        public bool Load(System.Drawing.Color color, VideoLayer videolayer)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                var scolor = '#' + color.ToArgb().ToString("X8");
                CasparItem item = new CasparItem((int)videolayer, scolor);
                channel.Load(item);
                Event removed;
                _visible.TryRemove(videolayer, out removed);
                _loadedNext.TryRemove(videolayer, out removed);
                Debug.WriteLine("CasparLoad color {0} Layer {1}", scolor, videolayer);
                return true;
            }
            return false;
        }
        

        public bool Seek(VideoLayer videolayer, long position)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                channel.Seek((int)videolayer, (uint)position);
                Debug.WriteLine("CasparSeek Channel {0} Layer {1} Position {2}", Id, (int)videolayer, position);
                return true;
            }
            return false;
        }

        public bool Play(Event aEvent)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                var eventType = aEvent.EventType;
                if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
                {
                    Event visible;
                    if (!(_visible.TryGetValue(aEvent.Layer, out visible) && visible == aEvent))
                    {
                        Event loaded;
                        if (!(_loadedNext.TryGetValue(aEvent.Layer, out loaded) && loaded == aEvent))
                        {
                            var item = _getItem(aEvent);
                            if (item != null)
                                channel.LoadBG(item);
                        }
                    }
                    channel.Play((int)aEvent.Layer);
                    _visible[aEvent.Layer] = aEvent;
                    Event removed;
                    _loadedNext.TryRemove(aEvent.Layer, out removed);
                    Debug.WriteLine(aEvent, $"CasparPlay Layer {aEvent.Layer}");
                    return true;
                }
                if (eventType == TEventType.Animation)
                {
                    var eTemplated = aEvent as ITemplated;
                    if (eTemplated != null)
                    {
                        switch (eTemplated.Method)
                        {
                            case TemplateMethod.Add:
                                var media = (aEvent.Engine.PlayoutChannelPRI == this) ? aEvent.ServerMediaPRI : aEvent.ServerMediaSEC;
                                if (media != null && media.FileExists())
                                {
                                    CasparCGDataCollection f = new CasparCGDataCollection();
                                    foreach (var field in eTemplated.Fields)
                                        f.SetData(field.Key, field.Value);
                                    channel.CG.Add((int)aEvent.Layer, eTemplated.TemplateLayer, Path.GetFileNameWithoutExtension(media.FileName).ToUpperInvariant(), true, f);
                                }
                                break;
                            case TemplateMethod.Clear:
                                channel.CG.Clear((int)aEvent.Layer);
                                break;
                            case TemplateMethod.Next:
                                channel.CG.Next((int)aEvent.Layer, eTemplated.TemplateLayer);
                                break;
                            case TemplateMethod.Play:
                                channel.CG.Play((int)aEvent.Layer, eTemplated.TemplateLayer);
                                break;
                            case TemplateMethod.Remove:
                                channel.CG.Remove((int)aEvent.Layer, eTemplated.TemplateLayer);
                                break;
                            case TemplateMethod.Stop:
                                channel.CG.Stop((int)aEvent.Layer, eTemplated.TemplateLayer);
                                break;
                            case TemplateMethod.Update:
                                CasparCGDataCollection uf = new CasparCGDataCollection();
                                foreach (var field in eTemplated.Fields)
                                    uf.SetData(field.Key, field.Value);
                                channel.CG.Update((int)aEvent.Layer, eTemplated.TemplateLayer, uf);
                                break;
                            default:
                                Debug.WriteLine("Method CG {0} not implemented", eTemplated.Method, null);
                                break;
                        }
                    }
                }
                if (eventType == TEventType.CommandScript)
                {
                    CommandScriptEvent csi = aEvent as CommandScriptEvent;
                    string command = csi?.Command;
                    return Execute(command);
                }
            }
            return false;
        }

        public bool Play(VideoLayer videolayer)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                {
                    channel.Play((int)videolayer);
                    Event removed;
                    _visible.TryRemove(videolayer, out removed);
                    _loadedNext.TryRemove(videolayer, out removed);
                    Debug.WriteLine("CasparPlay Layer {0}", videolayer);
                    return true;
                }
            }
            return false;
        }

        public bool Stop(Event aEvent)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                Event playing;
                if (_visible.TryGetValue(aEvent.Layer, out playing) && playing == aEvent)
                {
                    channel.Stop((int)aEvent.Layer);
                    Event removed;
                    _visible.TryRemove(aEvent.Layer, out removed);
                    _loadedNext.TryRemove(aEvent.Layer, out removed);
                    Debug.WriteLine(aEvent, string.Format("CasprarStop {0} layer {1}", aEvent, aEvent.Layer));
                }
                return true;
            }
            else
                return false;
        }

        public bool Pause(Event aEvent)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                Event visible;
                if (_visible.TryRemove(aEvent.Layer, out visible) && visible == aEvent)
                {
                    channel.Pause((int)aEvent.Layer);
                    Event removed;
                    _loadedNext.TryRemove(aEvent.Layer, out removed);
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
            if (_checkConnected(channel))
            {
                {
                    channel.Pause((int)videolayer);
                    Debug.WriteLine("CasparPause Layer {0}", videolayer);
                    return true;
                }
            }
            return false;
        }


        public void ReStart(Event ev)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel)
                && ev != null)
            {
                CasparItem item = _getItem(ev);
                if (item != null)
                {
                    if (ev.EventType == TEventType.Movie && ev.Media != null)
                        item.Seek = (int)ev.Position + (int)((ev.ScheduledTc.Ticks - ev.Media.TcPlay.Ticks) / ev.Engine.FrameTicks);
                    item.Transition.Duration = 3;
                    item.Transition.Type = TransitionType.MIX;
                    channel.LoadBG(item);
                    channel.Play(item.VideoLayer);
                    Debug.WriteLine("CasparChanner.ReStart: restarted {0} from frame {1}", item.Clipname, item.Seek);
                }
            }
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                channel.Clear((int)aVideoLayer);
                Event removed;
                _visible.TryRemove(aVideoLayer, out removed);
                _loadedNext.TryRemove(aVideoLayer, out removed);
                Debug.WriteLine(aVideoLayer, "CasparClear");
            }
        }

        public void Clear()
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                channel.Clear();
                channel.ClearMixer((int)VideoLayer.Program);
                outputAspectNarrow[VideoLayer.Program] = false;
                _visible.Clear();
                _loadedNext.Clear();
                VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(VideoLayer.Program, 1.0m));
                Debug.WriteLine(this, "CasparClear");
            }
        }

        public void ClearMixer()
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                channel.ClearMixer();
                channel.CG.Clear();
            }
        }

        public void SetVolume(VideoLayer videolayer, decimal volume, int transitionDuration)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
            {
                channel.Volume((int)videolayer, (float)volume, transitionDuration, Easing.Linear);
                VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(videolayer, volume));
            }
        }

        public void SetFieldOrderInverted(VideoLayer videolayer, bool invert)
        {
            var channel = _casparChannel;
            if (_checkConnected(channel))
                channel.SetInvertedFieldOrder((int)videolayer, invert);
        }

        public void SetAspect(VideoLayer layer, bool narrow)
        {
            var channel = _casparChannel;
            var oldAspectNarrow = outputAspectNarrow[layer];
            if (oldAspectNarrow != narrow
                && _checkConnected(channel))
            {
                outputAspectNarrow[layer] = narrow;
                if (narrow)
                    channel.Fill((int)layer, 0.125f, 0f, 0.75f, 1f, 10, Easing.Linear);
                else
                    channel.Fill((int)layer, 0f, 0f, 1f, 1f, 10, Easing.Linear);
                Debug.WriteLine("SetAspect narrow: {0}", narrow);
            }
        }
        #endregion //IPlayoutServerChannel

        public bool Execute(string command)
        {
            var channel = _casparChannel;
            if (string.IsNullOrWhiteSpace(command) || !_checkConnected(channel))
                return false;
            Match match = EventExtensions.RegexMixerFill.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                float x = float.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sx = float.Parse(match.Groups["sx"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sy = float.Parse(match.Groups["sy"].Value, System.Globalization.CultureInfo.InvariantCulture);
                int duration = string.IsNullOrWhiteSpace(match.Groups["duration"].Value) ? 0 : int.Parse(match.Groups["duration"].Value);
                TEasing easing = match.Groups["easing"].Success ? (TEasing)Enum.Parse(typeof(TEasing), match.Groups["easing"].Value, true) : TEasing.Linear;
                channel.Fill((int)layer, x, y, sx, sy, duration, (Svt.Caspar.Easing)easing);
                return true;
            }
            match = EventExtensions.RegexMixerClip.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                float x = float.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sx = float.Parse(match.Groups["sx"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sy = float.Parse(match.Groups["sy"].Value, System.Globalization.CultureInfo.InvariantCulture);
                int duration = string.IsNullOrWhiteSpace(match.Groups["duration"].Value) ? 0 : int.Parse(match.Groups["duration"].Value);
                TEasing easing = match.Groups["easing"].Success ? (TEasing)Enum.Parse(typeof(TEasing), match.Groups["easing"].Value, true) : TEasing.Linear;
                channel.Clip((int)layer, x, y, sx, sy, duration, (Svt.Caspar.Easing)easing);
                return true;
            }
            match = EventExtensions.RegexClearMixer.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                channel.ClearMixer((int)layer);
                return true;
            }
            match = EventExtensions.RegexPlay.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                string file = match.Groups["file"].Value;
                CasparItem item = new CasparItem((int)layer, file);
                Capture transitionTypeCapture = match.Groups["transition_type"];
                Capture transitionDurationCapture = match.Groups["transition_duration"];
                Capture transitionEasingCapture = match.Groups["easing"];
                int transitionDuration;
                TransitionType transitionType;
                if (int.TryParse(transitionDurationCapture.Value, out transitionDuration) && Enum.TryParse(transitionTypeCapture.Value, true, out transitionType))
                {
                    item.Transition.Type = transitionType;
                    item.Transition.Duration = transitionDuration;
                    Easing easing;
                    if (Enum.TryParse(transitionEasingCapture.Value, true, out easing))
                        item.Transition.Easing = easing;
                }
                channel.LoadBG(item);
                channel.Play(item.VideoLayer);
            }
            return false;
        }

        static TVideoFormat CasparModeToVideoFormat(VideoMode mode)
        {
            switch (mode)
            {
                case VideoMode.mPAL:
                    return TVideoFormat.PAL_FHA;
                case VideoMode.mNTSC:
                    return TVideoFormat.NTSC_FHA;
                case VideoMode.m576p2500:
                    return TVideoFormat.PAL_FHA_P;
                case VideoMode.m720p2500:
                    return TVideoFormat.HD720p2500;
                case VideoMode.m720p5000:
                    return TVideoFormat.HD720p5000;
                case VideoMode.m720p5994:
                    return TVideoFormat.HD720p5994;
                case VideoMode.m720p6000:
                    return TVideoFormat.HD720p6000;
                case VideoMode.m1080p2398:
                    return TVideoFormat.HD1080p2398;
                case VideoMode.m1080p2400:
                    return TVideoFormat.HD1080p2400;
                case VideoMode.m1080i5000:
                    return TVideoFormat.HD1080i5000;
                case VideoMode.m1080i5994:
                    return TVideoFormat.HD1080i5994;
                case VideoMode.m1080i6000:
                    return TVideoFormat.HD1080i6000;
                case VideoMode.m1080p2500:
                    return TVideoFormat.HD1080p2500;
                case VideoMode.m1080p2997:
                    return TVideoFormat.HD1080p2997;
                case VideoMode.m1080p3000:
                    return TVideoFormat.HD1080p3000;
                case VideoMode.m1080p5000:
                    return TVideoFormat.HD1080p5000;
                case VideoMode.m1080p5994:
                    return TVideoFormat.HD1080p5994;
                case VideoMode.m1080p6000:
                    return TVideoFormat.HD1080p6000;
                case VideoMode.m2160p2398:
                    return TVideoFormat.HD2160p2398;
                case VideoMode.m2160p2400:
                    return TVideoFormat.HD2160p2400;
                case VideoMode.m2160p2500:
                    return TVideoFormat.HD2160p2500;
                case VideoMode.m2160p2997:
                    return TVideoFormat.HD2160p2997;
                case VideoMode.m2160p3000:
                    return TVideoFormat.HD2160p3000;
                case VideoMode.m2160p5000:
                    return TVideoFormat.HD2160p5000;
                default:
                    return TVideoFormat.Other;
            }
        }
    }

    
    public class VolumeChangedEventArgs : EventArgs
    {
        public VolumeChangedEventArgs(VideoLayer layer, decimal volume)
        {
            Layer = layer;
            Volume = volume;
        }
        public decimal Volume { get; private set; }
        public VideoLayer Layer { get; private set; }
    }
}
