namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update016 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE rundownevent ADD COLUMN `SignalId` INTEGER UNSIGNED NULL DEFAULT NULL
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }
}
