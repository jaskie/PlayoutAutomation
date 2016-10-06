using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TAS.Server.Common
{
    public enum ConnectionStateRedundant
    {
        Closed = ConnectionState.Closed,
        //
        // Summary:
        //     The connection is open.
        Open = ConnectionState.Open,
        //
        // Summary:
        //     The connection object is connecting to the data source. (This value is reserved
        //     for future versions of the product.)
        Connecting = ConnectionState.Connecting,
        //
        // Summary:
        //     The connection object is executing a command. (This value is reserved for future
        //     versions of the product.)
        Executing = ConnectionState.Executing,
        //
        // Summary:
        //     The connection object is retrieving data. (This value is reserved for future
        //     versions of the product.)
        Fetching = ConnectionState.Fetching,
        //
        // Summary:
        //     The connection to the data source is broken. This can occur only after the connection
        //     has been opened. A connection in this state may be closed and then re-opened.
        //     (This value is reserved for future versions of the product.)
        Broken = ConnectionState.Broken,
        /// Summary:
        ///     The secondary connection is valid, but autoincrement values obtained differs from primary. 
        ///     Manual resync is required.
        Desynchronized = 128,
        /// <summary>
        /// When secondary connection is broken
        /// </summary>
        BrokenSecondary = ConnectionState.Broken + 128,
    }

    public class RedundantConnectionStateEventArgs : EventArgs
    {

        public RedundantConnectionStateEventArgs(ConnectionStateRedundant oldState, ConnectionStateRedundant newState)
        {
            OldState = oldState;
            NewState = newState;
        }
        public ConnectionStateRedundant OldState { get; private set; }
        public ConnectionStateRedundant NewState { get; private set; }
    }
}
