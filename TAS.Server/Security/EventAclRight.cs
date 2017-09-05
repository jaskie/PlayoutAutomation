using TAS.Database;

namespace TAS.Server.Security
{
    public class EventAclRight: AclRightBase
    {
        public override void Save()
        {
            if (Id == default(ulong))
                this.DbInsertEventAcl();
            else
                this.DbUpdateEventAcl();
        }

        public override void Delete()
        {
            this.DbDeleteEventAcl();
            Dispose();
        }
    }
}
