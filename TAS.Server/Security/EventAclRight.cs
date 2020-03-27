namespace TAS.Server.Security
{
    public class EventAclRight: AclRightBase
    {
        public override void Save()
        {
            if (Id == default)
                DatabaseProvider.Database.InsertEventAcl(this);
            else
                DatabaseProvider.Database.UpdateEventAcl(this);
            base.Save();
        }

        public override void Delete()
        {
            DatabaseProvider.Database.DeleteEventAcl(this);
            Dispose();
        }
    }
}
