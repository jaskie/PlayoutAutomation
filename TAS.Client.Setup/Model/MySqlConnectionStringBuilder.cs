using System;
using System.Data.Common;

namespace TAS.Client.Setup.Model
{
    /// <summary> 
    /// Provide convenience properties on top of the standard key/value 
    /// pairs stored in a connection string. 
    /// </summary> 
    public class MySqlConnectionStringBuilder : DbConnectionStringBuilder
    {
        /// <summary> 
        /// The name or IP address of the server to use. 
        /// </summary> 
        public string Host
        {
            get { return GetValue(HostKey, HostAliases); }
            set { SetValue(HostKey, HostAliases, value); }
        }

        /// <summary> 
        /// Port to use when connecting with sockets. 
        /// </summary> 
        public int Port
        {
            get { return GetValueInt(PortKey, null); }
            set
            {
                if (value > 0)
                    SetValue(PortKey, null, value.ToString());
                else
                    RemoveValue(PortKey, null);
            }
        }

        /// <summary> 
        /// Database to use initially. 
        /// </summary> 
        public string Database
        {
            get { return GetValue(DatabaseKey, DatabaseAliases); }
            set { SetValue(DatabaseKey, DatabaseAliases, value); }
        }

        /// <summary> 
        /// The username to connect as. 
        /// </summary> 
        public string UserID
        {
            get { return GetValue(UserIdKey, UserIdAliases); }
            set { SetValue(UserIdKey, UserIdAliases, value); }
        }

        /// <summary> 
        /// The password to use for authentication. 
        /// </summary> 
        public string Password
        {
            get { return GetValue(PasswdKey, PasswdAliases); }
            set { SetValue(PasswdKey, PasswdAliases, value); }
        }

        /// <summary> 
        /// Show user password in connection string. 
        /// </summary> 
        public bool PersistSecurityInfo
        {
            get { return GetValueBool(PersistKey, null); }
            set { SetValue(PersistKey, null, value.ToString()); }
        }

        /// <summary> 
        /// Number of seconds to wait for the connection to succeed. 
        /// </summary> 
        public int ConnectionTimeout
        {
            get { return GetValueInt(TimeoutKey, TimeoutAliases); }
            set
            {
                if (value > 0)
                    SetValue(TimeoutKey, TimeoutAliases, value.ToString());
                else
                    RemoveValue(TimeoutKey, TimeoutAliases);
            }
        }

        /// <summary> 
        /// Should the connection support pooling. 
        /// </summary> 
        public bool Pooling
        {
            get { return GetValueBool(PoolingKey, null); }
            set { SetValue(PoolingKey, null, value.ToString()); }
        }

        /// <summary> 
        /// Minimum number of connections to have in this pool. 
        /// </summary> 
        public int MinPoolSize
        {
            get { return GetValueInt(MinPoolKey, null); }
            set
            {
                if (value > 0)
                    SetValue(MinPoolKey, null, value.ToString());
                else
                    RemoveValue(MinPoolKey, null);
            }
        }

        /// <summary> 
        /// Maximum number of connections to have in this pool. 
        /// </summary> 
        public int MaxPoolSize
        {
            get { return GetValueInt(MaxPoolKey, null); }
            set
            {
                if (value > 0)
                    SetValue(MaxPoolKey, null, value.ToString());
                else
                    RemoveValue(MaxPoolKey, null);
            }
        }

        /// <summary> 
        /// Maximum number of seconds a connection should live. 
        /// This is checked when a connection is returned to the pool. 
        /// </summary> 
        public int ConnectionLifetime
        {
            get { return GetValueInt(LifetimeKey, LifetimeAliases); }
            set
            {
                if (value > 0)
                    SetValue(LifetimeKey, LifetimeAliases, value.ToString());
                else
                    RemoveValue(LifetimeKey, LifetimeAliases);
            }
        }

        private string GetValue(string key, string[] aliases)
        {
            object value;

            // look on the main key first 
            value = this[key];
            if (value != null)
                return value.ToString();

            // spin through the aliases as well 
            if (aliases != null)
            {
                foreach (string alias in aliases)
                {
                    value = this[alias];
                    if (value != null)
                        return value.ToString();
                }
            }

            return (null);
        }

        private int GetValueInt(string key, string[] aliases)
        {
            string value;

            value = GetValue(key, aliases);
            try
            {
                return Int32.Parse(value);
            }
            catch (Exception)
            {
                return (0);
            }
        }

        private bool GetValueBool(string key, string[] aliases)
        {
            string value;

            value = GetValue(key, aliases);
            if (value == null)
                return (false);

            if (String.Compare(value, "true", true) == 0 ||
            String.Compare(value, "yes", true) == 0 ||
            value == "1")
                return (true);
            else
                return (false);
        }

        private void SetValue(string key, string[] aliases, string value)
        {
            // remove any alias entries 
            if (aliases != null)
            {
                foreach (string alias in aliases)
                    Remove(alias);
            }

            // set the main entry if we have a value 
            if (value == null || value == "")
                Remove(key);
            else
                Add(key, value);
        }

        private void RemoveValue(string key, string[] aliases)
        {
            if (aliases != null)
            {
                foreach (string alias in aliases)
                    Remove(alias);
            }

            Remove(key);
        }

        private static string HostKey = "Server";
        private static string[] HostAliases = new string[] { 
            "Host", 
            "Data Source", 
            "Data Source", 
            "Server", 
            "Address", 
            "Addr", 
            "Network Address" 
            };

        private static string PortKey = "Port";

        private static string DatabaseKey = "Database";
        private static string[] DatabaseAliases = new string[] { 
            "Initial Catalog", 
            };

        private static string UserIdKey = "User Id";
        private static string[] UserIdAliases = new string[] { 
            "uid", 
            "User Name", 
            "userid" 
            };

        private static string PasswdKey = "Password";
        private static string[] PasswdAliases = new string[] { 
            "Pwd", 
            };

        private static string PersistKey = "Persist Security Info";

        private static string TimeoutKey = "Connect Timeout";
        private static string[] TimeoutAliases = new string[] { 
            "Connection Timeout", 
            };

        private static string PoolingKey = "Pooling";
        private static string MinPoolKey = "Min Pool Size";
        private static string MaxPoolKey = "Max Pool Size";

        private static string LifetimeKey = "Connect Lifetime";
        private static string[] LifetimeAliases = new string[] { 
            "Connection Lifetime", 
            };
    }
}

