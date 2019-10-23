using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ComponentModelRPC.Server;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public abstract class SearchProvider<T> : DtoBase, ISearchProvider<T>
    {

        private readonly IEnumerable<T> _result;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected SearchProvider(IEnumerable<T> result)
        {
            _result = result;
            TokenSource = new CancellationTokenSource();
        }

        public async void Start()
        {
            try
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
            catch (Exception e)
            {
                Logger.Error(e);
            }
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
