using System.Collections;
using System.Collections.Generic;

namespace TAS.Database.MySqlRedundant
{
    public class DbParameterCollectionRedundant: IEnumerable<DbParameterRedundant>
    {
        private readonly List<DbParameterRedundant> _parameters = new List<DbParameterRedundant>();

        internal DbParameterCollectionRedundant()
        {

        }

        public DbParameterRedundant AddWithValue(string key, object value)
        {
            var newParameter = new DbParameterRedundant(key, value);
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
