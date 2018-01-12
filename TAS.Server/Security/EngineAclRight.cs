namespace TAS.Server.Security
{
    public class EngineAclRight : AclRightBase
    {
        public override void Save()
        {
            if (Id == default(ulong))
                EngineController.Database.DbInsertEngineAcl(this);
            else
                EngineController.Database.DbUpdateEngineAcl(this);
        }

        public override void Delete()
        {
            EngineController.Database.DbDeleteEngineAcl(this);
            Dispose();
        }
    }
}
