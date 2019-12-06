namespace TAS.Server.Security
{
    public class EventAclRight: AclRightBase
    {
        public override void Save()
        {
            if (Id == default(ulong))
                EngineController.Current.Database.InsertEventAcl(this);
            else
                EngineController.Current.Database.UpdateEventAcl(this);
            base.Save();
        }

        public override void Delete()
        {
            EngineController.Current.Database.DeleteEventAcl(this);
            Dispose();
        }
    }
}
