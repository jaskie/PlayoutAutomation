using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using TAS.Common;

namespace TAS.Client.Setup
{
    public class EngineViewmodel : OkCancelViewmodelBase<Server.Engine>
    {
        readonly Server.EngineController _controller;
        public EngineViewmodel(Server.Engine engine, Server.EngineController controller) : base(engine, new EngineView(), "Channel config", 550, 320) 
        {
            _controller = controller;
            _channels = new List<Server.PlayoutServerChannel>() { null };
            controller.Servers.ForEach(s => _channels.AddRange(s.Channels));

            View.ShowDialog();
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

        readonly List<Server.PlayoutServerChannel> _channels;
        public List<Server.PlayoutServerChannel> Channels { get { return _channels; } }

        
        public Server.PlayoutServerChannel ChannelPGM { get; set; }

        public UserControl GPIView { get; set; }
        


        protected override void Close(object parameter)
        {
            View.Close();
        }

        protected override void OnDispose()
        {
            
        }

        protected override void Apply(object parameter)
        {
            base.Apply(parameter);
            if (!Server.DatabaseConnector.EngineSaveEngine(Model))
                System.Windows.MessageBox.Show("Unsuccessfull save");
        }
    }
}
