using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.Setup
{
    public class EngineViewmodel : OkCancelViewmodelBase<Server.Engine, EngineView>
    {
        public EngineViewmodel(Server.Engine engine) : base(engine) 
        {
            View.ShowDialog();
        }
        private string _engineName;
        public string EngineName { get { return _engineName; } set { SetField(ref _engineName, value, "EngineName"); } }

        readonly Array _videoFormats = Enum.GetValues(typeof(Server.TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }

        private Server.TVideoFormat _videoFormat;
        public Server.TVideoFormat VideoFormat { get { return _videoFormat; } set { SetField(ref _videoFormat, value, "VideoFormat"); } }

        private int _timeCorrection;
        public int TimeCorrection { get { return _timeCorrection; } set { SetField(ref _timeCorrection, value, "TimeCorrection"); } }

        private Server.TAspectRatioControl _aspectRatioControl;
        public Server.TAspectRatioControl AspectRatioControl { get { return _aspectRatioControl; } set { SetField(ref _aspectRatioControl, value, "AspectRatioControl"); } }

        readonly Array _aspectRatioControls = Enum.GetValues(typeof(Server.TAspectRatioControl)); 
        public Array AspectRatioControls { get { return _aspectRatioControls; } }

        readonly Array _gPITypes = Enum.GetValues(typeof(Server.GPIType));

        


        protected override void Close(object parameter)
        {
            View.Close();
        }

        protected override void OnDispose()
        {
            
        }
    }
}
