using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using TAS.Common;
using TAS.Client.Common;
using TAS.Data;

namespace TAS.Client.Setup
{
    public class EngineViewmodel : OkCancelViewmodelBase<Server.Engine>
    {
        readonly Server.EngineController _controller;
        public EngineViewmodel(Server.Engine engine, Server.EngineController controller) : base(engine, new EngineView(),  "Channel config") 
        {
            _controller = controller;
            _channels = new List<object>() { Properties.Resources._none_ };
            controller.Servers.ForEach(s => _channels.AddRange(s.Channels));
            _channelPGM = engine.PlayoutChannelPGM;
            _channelPRV = engine.PlayoutChannelPRV;
            var gpi = engine.GPI;
            _gPIEnabled = gpi != null;
            if (_gPIEnabled)
            {
                _gPIAddress = gpi.Address;
                _gPIGraphicsAhead = -gpi.GraphicsStartDelay;
            }
            Show();
        }
        private string _engineName;
        public string EngineName { get { return _engineName; } set { SetField(ref _engineName, value, "EngineName"); } }

        readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }

        private TVideoFormat _videoFormat;
        public TVideoFormat VideoFormat { get { return _videoFormat; } set { SetField(ref _videoFormat, value, "VideoFormat"); } }

        private int _timeCorrection;
        public int TimeCorrection { get { return _timeCorrection; } set { SetField(ref _timeCorrection, value, "TimeCorrection"); } }

        private TAspectRatioControl _aspectRatioControl;
        public TAspectRatioControl AspectRatioControl { get { return _aspectRatioControl; } set { SetField(ref _aspectRatioControl, value, "AspectRatioControl"); } }

        readonly Array _aspectRatioControls = Enum.GetValues(typeof(TAspectRatioControl)); 
        public Array AspectRatioControls { get { return _aspectRatioControls; } }

        readonly List<object> _channels;
        public List<object> Channels { get { return _channels; } }


        private object _channelPGM;
        public object ChannelPGM { get { return _channelPGM; } set { SetField(ref _channelPGM, value, "ChannelPGM"); } }
        private object _channelPRV;
        public object ChannelPRV { get { return _channelPRV; } set { SetField(ref _channelPRV, value, "ChannelPRV"); } }

        private bool _gPIEnabled;
        public bool GPIEnabled { get { return _gPIEnabled; } set { SetField(ref _gPIEnabled, value, "GPIEnabled"); } }

        private string _gPIAddress;
        public string GPIAddress { get { return _gPIAddress; } set { SetField(ref _gPIAddress, value, "GPIAddress"); } }

        private int _gPIGraphicsAhead;
        public int GPIGraphicsAhead { get { return _gPIGraphicsAhead; } set { SetField(ref _gPIGraphicsAhead, value, "GPIGraphicsAhead"); } }


        protected override void OnDispose()
        {
            
        }

        protected override void Apply(object parameter)
        {
            base.Apply(parameter);
            Model.PlayoutChannelPGM = _channelPGM as Server.PlayoutServerChannel;
            Model.PlayoutChannelPRV = _channelPRV as Server.PlayoutServerChannel;
            var gpi = Model.GPI;
            Model.UnInitialize();
            if (gpi == null)
            {
                if (_gPIEnabled)
                {
                    gpi = new Server.GPINotifier();
                    gpi.Address = _gPIAddress;
                    gpi.GraphicsStartDelay = -_gPIGraphicsAhead;
                    Model.GPI = gpi;
                }
            }
            else
            {
                if (_gPIEnabled)
                {
                    if (gpi.Address != _gPIAddress)
                        gpi.Address = _gPIAddress;
                    if (gpi.GraphicsStartDelay != -_gPIGraphicsAhead)
                        gpi.GraphicsStartDelay = -_gPIGraphicsAhead;
                }
                else 
                    Model.GPI = null;
            }
            Model.Initialize(Model.LocalGpi);
            {
                if (!Model.DbUpdate())
                    System.Windows.MessageBox.Show("Unsuccessfull save");
            }
        }
    }
}
