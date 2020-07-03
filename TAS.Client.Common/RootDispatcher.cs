using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TAS.Client.Common
{
    public class RootDispatcher
    {
        private static Dispatcher _rootDispatcher = null;

        public static Dispatcher Dispatcher
        {
            get
            {
                if (_rootDispatcher == null)
                    _rootDispatcher = Application.Current != null
                        ? Application.Current.Dispatcher
                        : Dispatcher.CurrentDispatcher;                  
                    
                return _rootDispatcher;
            }           
        }
    }
}
