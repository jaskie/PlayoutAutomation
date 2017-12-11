using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config
{
    public class PlayoutRecorderViewmodel: EditViewmodelBase<CasparRecorder>
    {
        public PlayoutRecorderViewmodel(Model.CasparRecorder r): base(r, null)
        {
            _id = r.Id;
            _recorderName = r.RecorderName;
        }
        private int _id;
        public int Id { get { return _id; } set { SetField(ref _id, value); } }
        private string _recorderName;
        public string RecorderName { get { return _recorderName; } set { SetField(ref _recorderName, value); } }
    }
}
