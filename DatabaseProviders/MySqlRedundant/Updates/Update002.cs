namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update002 : UpdateBase
    {
        private const string Script = @"
CREATE TABLE `media_templated` (
  `MediaGuid` binary(16) NOT NULL,
  `Fields` text,
  PRIMARY KEY (`MediaGuid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE `rundownevent_templated` (
  `idrundownevent_templated` int(11) NOT NULL,
  `Fields` text,
  PRIMARY KEY (`idrundownevent_templated`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
