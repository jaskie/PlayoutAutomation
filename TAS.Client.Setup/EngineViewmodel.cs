using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client.Setup
{
    public class EngineViewmodel : EditViewmodelBase<Model.Engine>
    {
        private TAspectRatioControl _aspectRatioControl;
        private string _engineName;
        private int _timeCorrection;
        private TVideoFormat _videoFormat;
        private ulong _instance;
        public EngineViewmodel(Model.Engine engine)
            : base(engine, new EngineView())
        {
            _channels = new List<object>() { TAS.Client.Common.Properties.Resources._none_ };
            Model.Servers.ForEach(s => s.Channels.ForEach(c => _channels.Add(c)));
            _channelPGM = _channels.FirstOrDefault(c => c is Model.CasparServerChannel 
                                                        && ((Model.CasparServerChannel)c).ChannelNumber == Model.ServerChannelPGM 
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerPGM);
            if (_channelPGM == null) _channelPGM = _channels.First();
            _channelPRV = _channels.FirstOrDefault(c => c is Model.CasparServerChannel
                                                        && ((Model.CasparServerChannel)c).ChannelNumber == Model.ServerChannelPRV
                                                        && ((Model.CasparServerChannel)c).Owner is Model.CasparServer
                                                        && ((Model.CasparServer)(((Model.CasparServerChannel)c).Owner)).Id == Model.IdServerPRV);
            if (_channelPRV == null) _channelPRV = _channels.First();
        }

        protected override void OnDispose() { }

        readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }

        readonly Array _aspectRatioControls = Enum.GetValues(typeof(TAspectRatioControl));
        public Array AspectRatioControls { get { return _aspectRatioControls; } }

        public string EngineName { get { return _engineName; } set { SetField(ref _engineName, value, "EngineName"); } }
        public TAspectRatioControl AspectRatioControl { get { return _aspectRatioControl; } set { SetField(ref _aspectRatioControl, value, "AspectRatioControl"); } }
        public int TimeCorrection { get { return _timeCorrection; } set { SetField(ref _timeCorrection, value, "TimeCorrection"); } }
        public TVideoFormat VideoFormat { get { return _videoFormat; } set { SetField(ref _videoFormat, value, "VideoFormat"); } }
        public ulong Instance { get { return _instance; } set { SetField(ref _instance, value, "Instance"); } }
        public Model.Gpi Gpi { get; set; }

        readonly List<object> _channels;
        public List<object> Channels { get { return _channels; } }
        private object _channelPGM;
        public object ChannelPGM { get { return _channelPGM; } set { SetField(ref _channelPGM, value, "ChannelPGM"); } }
        private object _channelPRV;
        public object ChannelPRV { get { return _channelPRV; } set { SetField(ref _channelPRV, value, "ChannelPRV"); } }
        public override void Save(object destObject = null)
        {
            if (Modified)
            {
                var playoutServerChannelPGM = _channelPGM as Model.CasparServerChannel;
                Model.IdServerPGM = playoutServerChannelPGM == null ? 0 : ((Model.CasparServer)playoutServerChannelPGM.Owner).Id;
                Model.ServerChannelPGM = playoutServerChannelPGM == null ? 0 : playoutServerChannelPGM.ChannelNumber;
                var playoutServerChannelPRV = _channelPRV as Model.CasparServerChannel;
                Model.IdServerPRV = playoutServerChannelPRV == null ? 0 : ((Model.CasparServer)playoutServerChannelPRV.Owner).Id;
                Model.ServerChannelPRV = playoutServerChannelPRV == null ? 0 : playoutServerChannelPRV.ChannelNumber;
            }
            base.Save(destObject);
        }

        public int IdArchive { get; set; }

    }
}
