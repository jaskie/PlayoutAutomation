namespace TAS.Server.Security
{
    public class EventAclRight: AclRightBase
    {
        public override void Save()
        {
            if (Id == default(ulong))
                EngineController.Database.InsertEventAcl(this);
            else
                EngineController.Database.UpdateEventAcl(this);
            base.Save();
        }

        public override void Delete()
        {
            EngineController.Database.DeleteEventAcl(this);
            Dispose();
        }
    }
}
