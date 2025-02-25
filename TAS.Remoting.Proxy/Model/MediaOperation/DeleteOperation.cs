using System;
using jNet.RPC;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model.MediaOperation
{
    public class DeleteOperation : FileOperationBase, IDeleteOperation
    {
        #pragma warning disable CS0649, CS0169

        [DtoMember(nameof(ILoudnessOperation.Source))]
        private MediaBase _source;

        #pragma warning restore

        public IMedia Source { get => _source; set => Set(value); }

    }
}
