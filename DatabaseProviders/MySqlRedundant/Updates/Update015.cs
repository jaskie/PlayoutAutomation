using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TAS.Common;

namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update015 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE servermedia
ADD COLUMN `LastPlayed` DATETIME(3) NULL DEFAULT NULL AFTER `LastUpdated`;
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }
}
