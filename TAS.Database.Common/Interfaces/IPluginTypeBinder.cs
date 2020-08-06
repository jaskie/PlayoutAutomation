using System;

namespace TAS.Database.Common.Interfaces
{
    public interface IPluginTypeBinder
    {        
        /// <summary>
        /// Used when deserializing JSON
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        Type BindToType(string assemblyName, string typeName);

        /// <summary>
        /// Used to convert types configModel->model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Type GetBindedType(Type configType);        
    }
}
