using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.Common
{
    /// <summary>
    /// Interface for OkCancel Window. Implement and define what will buttons do.
    /// </summary>
    public interface IOkCancelViewModel
    {
        /// <summary>
        /// What will happen on Ok button click
        /// </summary>        
        /// <returns>True if window can be closed, false if otherwise</returns>
        bool Ok(object obj);
        void Cancel(object obj);
        bool CanOk(object obj);
        bool CanCancel(object obj);
    }
}
