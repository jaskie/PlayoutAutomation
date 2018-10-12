namespace TAS.Server.Security
{
    public class EngineAclRight : AclRightBase
    {
        public override void Save()
        {
            if (Id == default(ulong))
                EngineController.Database.InsertEngineAcl(this);
            else
                EngineController.Database.UpdateEngineAcl(this);
        }

        public override void Delete()
        {
            EngineController.Database.DeleteEngineAcl(this);
            Dispose();
        }
    }
}
