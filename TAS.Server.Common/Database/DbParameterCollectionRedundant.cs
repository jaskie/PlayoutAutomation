using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Database
{
    public class DbParameterCollectionRedundant: IEnumerable<DbParameterRedundant>
    {
        private readonly List<DbParameterRedundant> _parameters = new List<DbParameterRedundant>();
        internal DbParameterCollectionRedundant()
        {

        }
        public DbParameterRedundant AddWithValue (string key, object value)
        {
            //TODO: Temporary (as of version 6.9.9) MySql fix to properly serialize TimeSpan with fractional seconds
            if (value is TimeSpan)
                value = ((TimeSpan)value).ToString("c");
            DbParameterRedundant newParameter = new DbParameterRedundant(key, value);
            _parameters.Add(newParameter);
            return newParameter;
        }

        public IEnumerator<DbParameterRedundant> GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }
    }
}
