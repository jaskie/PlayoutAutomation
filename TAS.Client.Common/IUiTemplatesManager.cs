using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.Common
{
    public interface IUiTemplatesManager
    {
        /// <summary>
        /// I wanted to execute this method by main application, but module loading is not so universal... It would be redundant code. 
        /// For now DatabaseMySqlRedundant constructor will fire it.
        /// </summary>
        void LoadDataTemplates();
    }
}
