using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Common.Plugin
{
    public interface UiPlugin: IUiMenuItem
    {
        bool AppliesToEngine(IEngine engine);
    }
}
