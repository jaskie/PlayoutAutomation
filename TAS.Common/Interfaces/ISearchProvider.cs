using System;

namespace TAS.Common.Interfaces
{
    public interface ISearchProvider<T>
    {
        void Start();
        void Cancel();
        event EventHandler<EventArgs<T>> ItemAdded;
        event EventHandler Finished;
    }
}
