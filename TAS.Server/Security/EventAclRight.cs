using TAS.Database;

namespace TAS.Server.Security
{
    public class EventAclRight: AclRightBase
    {
        public override void Save()
        {
            if (Id == default(ulong))
                EngineController.Database.DbInsertEventAcl(this);
            else
                EngineController.Database.DbUpdateEventAcl(this);
        }

        public override void Delete()
        {
            EngineController.Database.DbDeleteEventAcl(this);
            Dispose();
        }
    }
}
