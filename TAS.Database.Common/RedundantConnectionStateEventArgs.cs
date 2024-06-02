using System;
using TAS.Common;

namespace TAS.Database.Common
{
    public class RedundantConnectionStateEventArgs : EventArgs
    {
        public RedundantConnectionStateEventArgs(ConnectionStateRedundant oldState, ConnectionStateRedundant newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public ConnectionStateRedundant OldState { get; }

        public ConnectionStateRedundant NewState { get; }
    }
}
