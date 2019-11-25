//#undef DEBUG
using System;
using System.Linq;
using Svt.Caspar;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using TAS.Remoting.Server;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using TAS.Common;
using System.Text.RegularExpressions;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Server.Media;

namespace TAS.Server
{
    public class CasparServerChannel : DtoBase, IPlayoutServerChannel, IPlayoutServerChannelProperties
    {
        private Channel _casparChannel;
        private readonly ConcurrentDictionary<VideoLayer, bool> _outputAspectNarrow = new ConcurrentDictionary<VideoLayer, bool>();
        private readonly ConcurrentDictionary<VideoLayer, Event> _loadedNext = new ConcurrentDictionary<VideoLayer, Event>();
        private readonly ConcurrentDictionary<VideoLayer, Event> _visible = new ConcurrentDictionary<VideoLayer, Event>();
        private bool _isServerConnected;
        private int _audiolevel;

        public static readonly Regex RegexMixerClip = new Regex(EventExtensions.MixerClipCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexMixerFill = new Regex(EventExtensions.MixerFillCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexMixerClear = new Regex(EventExtensions.MixerClearCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexPlay = new Regex(EventExtensions.PlayCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexCall = new Regex(EventExtensions.CallCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexCg = new Regex(EventExtensions.CgCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexCgWithLayer = new Regex(EventExtensions.CgWithLayerCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexCgAdd = new Regex(EventExtensions.CgAddCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexCgInvoke = new Regex(EventExtensions.CgInvokeCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);
        public static readonly Regex RegexCgUpdate = new Regex(EventExtensions.CgUpdateCommand, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);


        #region IPlayoutServerChannel

        [JsonProperty]
        public int Id { get; set; }

        [JsonProperty]
        public string ChannelName { get; set; }

        public double MasterVolume { get; set; } = 1;

        [JsonProperty]
        public int AudioChannelCount { get; set; } = 2;

        [JsonProperty]
        public string PreviewUrl { get; set; }

        public string LiveDevice { get; set; }

        [XmlIgnore]
        [JsonProperty]
        public TVideoFormat VideoFormat { get; set; }

        [XmlIgnore]
        [JsonProperty]
        public bool IsServerConnected { get => _isServerConnected; internal set => SetField(ref _isServerConnected, value); }

        [XmlIgnore]
        [JsonProperty]
        public int AudioLevel { get => _audiolevel; private set => SetField(ref _audiolevel, value); }

        public bool LoadNext(Event aEvent)
        {
            var channel = _casparChannel;
            if (aEvent != null && CheckConnected(channel))
            {
                var eventType = aEvent.EventType;
                if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
                {
                    CasparItem item = _getItem(aEvent);
                    if (item != null)
                    {
                        channel.LoadBG(item);
                        _loadedNext[aEvent.Layer] = aEvent;
                        Debug.WriteLine(aEvent, "CasparLoadNext");
                        return true;
                    }
                }
            }
            Debug.WriteLine(aEvent, "CasparLoadNext did not load");
            return false;
        }

        public bool Load(Event aEvent)
        {
            var channel = _casparChannel;
            if (aEvent != null && CheckConnected(channel))
            {
                var eventType = aEvent.EventType;
                if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
                {
                    var item = _getItem(aEvent);
                    if (item != null)
                    {
                        channel.Load(item);
                        if (eventType == TEventType.Live)
                            channel.Play(item.VideoLayer);
                        _visible[aEvent.Layer] = aEvent;
                        _loadedNext.TryRemove(aEvent.Layer, out _);
                        Debug.WriteLine(aEvent, "CasparLoad");
                        return true;
                    }
                }
            }
            Debug.WriteLine(aEvent, "CasparLoad did not load");
            return false;
        }

        public bool Load(MediaBase media, VideoLayer videolayer, long seek, long duration)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel)
                && media != null)
            {
                CasparItem item = _getItem(media, videolayer, seek);
                if (item != null)
                {
                    item.Length = (int)duration;
                    channel.Load(item);
                    _visible.TryRemove(videolayer, out _);
                    _loadedNext.TryRemove(videolayer, out _);
                    Debug.WriteLine("CasparLoad media {0} Layer {1} Seek {2}", media, videolayer, seek);
                    return true;
                }
            }
            return false;
        }

        public bool Load(System.Drawing.Color color, VideoLayer videolayer)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
            {
                var scolor = '#' + color.ToArgb().ToString("X8");
                CasparItem item = new CasparItem((int)videolayer, scolor);
                channel.Load(item);
                _visible.TryRemove(videolayer, out _);
                _loadedNext.TryRemove(videolayer, out _);
                Debug.WriteLine("CasparLoad color {0} Layer {1}", scolor, videolayer);
                return true;
            }
            return false;
        }


        public bool Seek(VideoLayer videolayer, long position)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
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
            if (CheckConnected(channel))
            {
                var eventType = aEvent.EventType;
                if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
                {
                    if (!(_visible.TryGetValue(aEvent.Layer, out var visible) && visible == aEvent))
                    {
                        if (!(_loadedNext.TryGetValue(aEvent.Layer, out var loaded) && loaded == aEvent))
                        {
                            var item = _getItem(aEvent);
                            if (item != null)
                                channel.LoadBG(item);
                        }
                    }
                    channel.Play((int)aEvent.Layer);
                    _visible[aEvent.Layer] = aEvent;
                    _loadedNext.TryRemove(aEvent.Layer, out _);
                    Debug.WriteLine(aEvent, $"CasparPlay Layer {aEvent.Layer}");
                    return true;
                }
                if (eventType == TEventType.Animation)
                {
                    if (aEvent is ITemplated eTemplated)
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
                                channel.CG.Update((int)aEvent.Layer, eTemplated.TemplateLayer, uf.ToAMCPEscapedXml());
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
            if (CheckConnected(channel))
            {
                {
                    channel.Play((int)videolayer);
                    _visible.TryRemove(videolayer, out _);
                    _loadedNext.TryRemove(videolayer, out _);
                    Debug.WriteLine("CasparPlay Layer {0}", videolayer);
                    return true;
                }
            }
            return false;
        }

        public bool Stop(Event aEvent)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
            {
                if (!_visible.TryGetValue(aEvent.Layer, out var playing) || playing != aEvent)
                    return true;
                channel.Stop((int)aEvent.Layer);
                _visible.TryRemove(aEvent.Layer, out var _);
                _loadedNext.TryRemove(aEvent.Layer, out _);
                Debug.WriteLine(aEvent, $"CasprarStop {aEvent} layer {aEvent.Layer}");
                return true;
            }
            else
                return false;
        }

        public bool Pause(Event aEvent)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
            {
                if (_visible.TryRemove(aEvent.Layer, out var visible) && visible == aEvent)
                {
                    channel.Pause((int)aEvent.Layer);
                    _loadedNext.TryRemove(aEvent.Layer, out _);
                    Debug.WriteLine(aEvent, $"CasprarPause {aEvent} layer {aEvent.Layer}");
                }
                return true;
            }
            else
                return false;
        }

        public bool Pause(VideoLayer videolayer)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
            {
                {
                    channel.Pause((int)videolayer);
                    Debug.WriteLine("CasparPause Layer {0}", videolayer);
                    return true;
                }
            }
            return false;
        }


        internal void ReStart(Event ev, bool play)
        {
            var channel = _casparChannel;
            if (!CheckConnected(channel) || ev == null)
                return;
            var item = _getItem(ev);
            if (item == null)
                return;
            var media = ev.Media;
            if (ev.EventType == TEventType.Movie && media != null)
                item.Seek = (int)ev.Position + (int)((ev.ScheduledTc.Ticks - media.TcPlay.Ticks) / ev.Engine.FrameTicks);
            item.Transition.Duration = 3;
            item.Transition.Type = TransitionType.MIX;
            if (play)
            {
                channel.LoadBG(item);
                channel.Play(item.VideoLayer);
            }
            else
                channel.Load(item);
            Debug.WriteLine("CasparChanner.ReStart: restarted {0} from frame {1}", item.Clipname, item.Seek);
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
            {
                channel.Clear((int)aVideoLayer);
                _visible.TryRemove(aVideoLayer, out _);
                _loadedNext.TryRemove(aVideoLayer, out _);
                Debug.WriteLine(aVideoLayer, "CasparClear");
            }
        }

        public void Clear()
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
            {
                channel.Clear();
                channel.ClearMixer((int)VideoLayer.Program);
                _outputAspectNarrow[VideoLayer.Program] = false;
                _visible.Clear();
                _loadedNext.Clear();
                VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(VideoLayer.Program, 1));
                Debug.WriteLine(this, "CasparClear");
            }
        }

        public void ClearMixer()
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
            {
                channel.ClearMixer();
                channel.CG.Clear();
                _outputAspectNarrow.Clear();
            }
        }

        public void SetVolume(VideoLayer videolayer, double volume, int transitionDuration)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
            {
                channel.Volume((int)videolayer, (float)volume, transitionDuration, Easing.Linear);
                VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(videolayer, volume));
            }
        }

        public void SetFieldOrderInverted(VideoLayer videolayer, bool invert)
        {
            var channel = _casparChannel;
            if (CheckConnected(channel))
                channel.SetInvertedFieldOrder((int)videolayer, invert);
        }

        public void SetAspect(VideoLayer layer, bool narrow)
        {
            var channel = _casparChannel;
            _outputAspectNarrow.TryGetValue(layer, out var oldAspectNarrow);
            if (oldAspectNarrow != narrow
                && CheckConnected(channel))
            {
                _outputAspectNarrow[layer] = narrow;
                if (narrow)
                    channel.Fill((int)layer, 0.125f, 0f, 0.75f, 1f, 10, Easing.Linear);
                else
                    channel.Fill((int)layer, 0f, 0f, 1f, 1f, 10, Easing.Linear);
                Debug.WriteLine("SetAspect narrow: {0}", narrow);
            }
        }

        public bool Execute(string command)
        {
            var channel = _casparChannel;
            if (string.IsNullOrWhiteSpace(command) || !CheckConnected(channel))
                return false;
            Match match = RegexMixerFill.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer) Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                float x = float.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
                float sx = float.Parse(match.Groups["sx"].Value, CultureInfo.InvariantCulture);
                float sy = float.Parse(match.Groups["sy"].Value, CultureInfo.InvariantCulture);
                int duration = string.IsNullOrWhiteSpace(match.Groups["duration"].Value)
                    ? 0
                    : int.Parse(match.Groups["duration"].Value);
                TEasing easing = match.Groups["easing"].Success
                    ? (TEasing) Enum.Parse(typeof(TEasing), match.Groups["easing"].Value, true)
                    : TEasing.Linear;
                channel.Fill((int) layer, x, y, sx, sy, duration, (Easing) easing);
                return true;
            }
            match = RegexMixerClip.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer) Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                float x = float.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
                float sx = float.Parse(match.Groups["sx"].Value, CultureInfo.InvariantCulture);
                float sy = float.Parse(match.Groups["sy"].Value, CultureInfo.InvariantCulture);
                int duration = string.IsNullOrWhiteSpace(match.Groups["duration"].Value)
                    ? 0
                    : int.Parse(match.Groups["duration"].Value);
                TEasing easing = match.Groups["easing"].Success
                    ? (TEasing) Enum.Parse(typeof(TEasing), match.Groups["easing"].Value, true)
                    : TEasing.Linear;
                channel.Clip((int) layer, x, y, sx, sy, duration, (Easing) easing);
                return true;
            }
            match = RegexMixerClear.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer) Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                channel.ClearMixer((int) layer);
                return true;
            }
            match = RegexPlay.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer) Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                string file = match.Groups["file"].Value;
                CasparItem item = new CasparItem((int) layer, file);
                Capture transitionTypeCapture = match.Groups["transition_type"];
                Capture transitionDurationCapture = match.Groups["transition_duration"];
                Capture transitionEasingCapture = match.Groups["easing"];
                if (int.TryParse(transitionDurationCapture.Value, out var transitionDuration) &&
                    Enum.TryParse(transitionTypeCapture.Value, true, out TransitionType transitionType))
                {
                    item.Transition.Type = transitionType;
                    item.Transition.Duration = transitionDuration;
                    if (Enum.TryParse(transitionEasingCapture.Value, true, out Easing easing))
                        item.Transition.Easing = easing;
                }
                channel.LoadBG(item);
                channel.Play(item.VideoLayer);
                return true;
            }
            match = RegexCall.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                string function = match.Groups["function"].Value;
                channel.Call((int)layer, function);
                return true;
            }
            match = RegexCg.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer) Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                if (Enum.TryParse(match.Groups["method"].Value, true, out TemplateMethod methodEnum))
                {
                    if (methodEnum == TemplateMethod.Clear)
                    {
                        channel.CG.Clear((int) layer);
                        return true;
                    }
                    var matchWithCgLayer = RegexCgWithLayer.Match(command);
                    if (!matchWithCgLayer.Success || !int.TryParse(matchWithCgLayer.Groups["cg_layer"].Value, out var cgLayer))
                        return false;
                    switch (methodEnum)
                    {
                        case TemplateMethod.Play:
                            channel.CG.Play((int) layer, cgLayer);
                            return true;
                        case TemplateMethod.Next:
                            channel.CG.Next((int) layer, cgLayer);
                            return true;
                        case TemplateMethod.Stop:
                            channel.CG.Stop((int) layer, cgLayer);
                            return true;
                        case TemplateMethod.Remove:
                            channel.CG.Remove((int) layer, cgLayer);
                            return true;
                        case TemplateMethod.Add:
                            var matchAdd = RegexCgAdd.Match(command);
                            if (!matchAdd.Success)
                                return false;
                            var file = matchAdd.Groups["file"].Value;
                            if (string.IsNullOrWhiteSpace(file))
                                return false;
                            int.TryParse(matchAdd.Groups["play_on_load"].Value, out var playOnLoadAsInt);
                            channel.CG.Add((int) layer, cgLayer, file, playOnLoadAsInt == 1,
                                matchAdd.Groups["data"].Value);
                            return true;
                        case TemplateMethod.Invoke:
                            var matchInvoke = RegexCgInvoke.Match(command);
                            if (!matchInvoke.Success)
                                return false;
                            var cgMethod = matchInvoke.Groups["cg_method"].Value;
                            if (string.IsNullOrWhiteSpace(cgMethod))
                                return false;
                            channel.CG.Invoke((int) layer, cgLayer, cgMethod);
                            return true;
                        case TemplateMethod.Update:
                            var matchUpdate = RegexCgUpdate.Match(command);
                            if (!matchUpdate.Success)
                                return false;
                            var data = matchUpdate.Groups["data"].Value;
                            if (string.IsNullOrWhiteSpace(data))
                                return false;
                            channel.CG.Update((int) layer, cgLayer, data);
                            return true;
                    }
                }
            }
            return false;
        }

        #endregion //IPlayoutServerChannel

        internal CasparServer Owner { get; set; }
        internal void AssignCasparChannel(Channel casparChannel)
        {
            if (casparChannel == null)
                return;
            var oldChannel = _casparChannel;
            if (oldChannel == casparChannel)
                return;
            _casparChannel = casparChannel;
            if (oldChannel != null)
                oldChannel.AudioDataReceived -= Channel_AudioDataReceived;
            casparChannel.AudioDataReceived += Channel_AudioDataReceived;
            VideoFormat = CasparModeToVideoFormat(casparChannel.VideoMode);
            Debug.WriteLine(this, "Caspar channel assigned");
            if (Owner?.IsConnected != true)
                return;
            ClearMixer();
            casparChannel.MasterVolume((float) MasterVolume);
        }

        #region Utilites

        private void Channel_AudioDataReceived(object sender, AudioDataEventArgs e)
        {
            if (sender == _casparChannel)
            {
                var values = e.AudioData.dBFS.Where(f => f.HasValue).Select(f=> f.Value).ToArray();
                if (values.Any())
                    AudioLevel = (int)values.Average();
            }
        }

        private bool CheckConnected(Channel channel)
        {
            var server = Owner;
            if (server != null && channel != null)
                return server.IsConnected;
            return false;
        }

        public event EventHandler<VolumeChangedEventArgs> VolumeChanged;
        
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
                item.Transition.Type = (TransitionType)aEvent.TransitionType;
                item.Transition.Duration = (int)((aEvent.TransitionTime.Ticks - aEvent.TransitionPauseTime.Ticks) / aEvent.Engine.FrameTicks);
                item.Transition.Pause = (int)(aEvent.TransitionPauseTime.Ticks / aEvent.Engine.FrameTicks);
                item.Transition.Easing = (Easing)aEvent.TransitionEasing; 
                item.Seek = (int)aEvent.MediaSeek;
                return item;
            }
            return null;
        }

        private CasparItem _getItem(MediaBase media, VideoLayer videolayer, long seek)
        {
            if (media != null && media.MediaType == TMediaType.Movie)
            {
                return new CasparItem(string.Empty)
                {
                    Clipname = $"\"{(media is ServerMedia ? Path.GetFileNameWithoutExtension(media.FileName) : media.FullPath)}\"",
                    ChannelLayout = ChannelLayout.Stereo,
                    VideoLayer = (int) videolayer,
                    Seek = (int) seek,
                    FieldOrderInverted = media.FieldOrderInverted
                };
            }
            return null;
        }

        private TVideoFormat CasparModeToVideoFormat(VideoMode mode)
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

        #endregion // Utilities
    }

    public class VolumeChangedEventArgs : EventArgs
    {
        public VolumeChangedEventArgs(VideoLayer layer, double volume)
        {
            Layer = layer;
            Volume = volume;
        }
        [JsonProperty]
        public double Volume { get; private set; }
        [JsonProperty]
        public VideoLayer Layer { get; private set; }
    }


}
