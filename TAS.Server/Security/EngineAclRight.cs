namespace TAS.Server.Security
{
    public class EngineAclRight : AclRightBase
    {
        public override void Save()
        {
            if (Id == default(ulong))
                EngineController.Current.Database.InsertEngineAcl(this);
            else
                EngineController.Current.Database.UpdateEngineAcl(this);
            base.Save();
        }

        public override void Delete()
        {
            EngineController.Current.Database.DeleteEngineAcl(this);
            Dispose();
        }
    }
}
