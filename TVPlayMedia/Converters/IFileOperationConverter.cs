using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Server.Interfaces;
using Newtonsoft.Json;

namespace TAS.Client.Converters
{
    class IFileOperationConverter : DtoCreationConverter<IFileOperation>
    {
        public IFileOperationConverter(IRemoteClient client) : base(client) { }
        public override IFileOperation Create(Type objectType)
        {
            if (objectType.IsInterface)
            {
                if (objectType == typeof(IConvertOperation))
                    return new ConvertOperation();
                if (objectType == typeof(ILoudnessOperation))
                    return new LoudnessOperation();
                return new FileOperation();
            }
            return (IFileOperation)Activator.CreateInstance(objectType);
        }
    }
}
