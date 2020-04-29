using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.Common
{
    public interface IUiStateManager
    {
        void SetBusyState(bool state = true);
    }
}
