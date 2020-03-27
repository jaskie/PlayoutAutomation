namespace TAS.Server.Security
{
    public class EngineAclRight : AclRightBase
    {
        public override void Save()
        {
            if (Id == default)
                DatabaseProvider.Database.InsertEngineAcl(this);
            else
                DatabaseProvider.Database.UpdateEngineAcl(this);
            base.Save();
        }

        public override void Delete()
        {
            DatabaseProvider.Database.DeleteEngineAcl(this);
            Dispose();
        }
    }
}
