using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.Common
{
    public interface IWindowManager
    {
        void ShowWindow(object content, string title);
        bool? ShowDialog(object content, string title);
        //void ShowOkCancelWindow(object content, string title = null);
        //bool? ShowOkCancelDialog(object content, string title = null);
        void CloseWindow(object content);
    }
}
