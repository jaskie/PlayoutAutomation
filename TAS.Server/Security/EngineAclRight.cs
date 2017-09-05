using TAS.Database;

namespace TAS.Server.Security
{
    public class EngineAclRight : AclRightBase
    {
        public override void Save()
        {
            if (Id == default(ulong))
                this.DbInsertEngineAcl();
            else
                this.DbUpdateEngineAcl();
        }

        public override void Delete()
        {
            this.DbDeleteEngineAcl();
            Dispose();
        }
    }
}
