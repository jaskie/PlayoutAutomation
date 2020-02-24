namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update007 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `asrunlog` 
ADD COLUMN `Flags` BIGINT UNSIGNED NULL AFTER `typAudio`;
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
