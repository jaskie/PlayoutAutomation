using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Database;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class AnimatedMedia : PersistentMedia, IAnimatedMedia
    {
        public AnimatedMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid, idPersistentMedia) { }

        public Dictionary<string, string> Fields { get; set; }


        public override bool Save()
        {
            bool result = false;
            var directory = Directory as AnimationDirectory;
            if (directory != null)
            {
                if (IdPersistentMedia == 0)
                    result = this.DbInsert(directory.Server.Id);
                else
                if (Modified)
                    result = this.DbUpdate(directory.Server.Id);
            }
            Modified = false;
            return result;
        }
    }
}
