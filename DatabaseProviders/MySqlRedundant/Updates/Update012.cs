namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update012 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `rundownevent`
ADD COLUMN `RouterPort` SMALLINT DEFAULT NULL;
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
