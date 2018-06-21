using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface IPersistent
    {
        ulong Id { get; set; }

        IDictionary<string, int> FieldLengths { get; }

        void Save();

        void Delete();
    }
}
