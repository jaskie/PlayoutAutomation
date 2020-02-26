using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TAS.Common;

namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update014 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE rundownevent
ADD COLUMN RecordingInfo JSON;
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }
}
