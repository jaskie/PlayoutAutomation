using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Server
{
    public abstract class SearchProvider<T> : DtoBase, ISearchProvider<T>
    {

        private readonly IEnumerable<T> _result;

        protected SearchProvider(IEnumerable<T> result)
        {
            _result = result;
            TokenSource = new CancellationTokenSource();
        }

        public async void Start()
        {
            await Task.Run(() =>
            {
                using (var enumerator = _result.GetEnumerator())
                    while (enumerator.MoveNext())
                    {
                        if (TokenSource.IsCancellationRequested)
                            break;
                        ItemAdded?.Invoke(this, new EventArgs<T>(enumerator.Current));
                    }
            }, TokenSource.Token);
            Finished?.Invoke(this, EventArgs.Empty);
        }

        public CancellationTokenSource TokenSource { get; }

        public void Cancel()
        {
            TokenSource.Cancel();
        }

        protected override void DoDispose()
        {
            TokenSource.Dispose();
        }

        public event EventHandler<EventArgs<T>> ItemAdded;

        public event EventHandler Finished;

    }
}
