using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using jNet.RPC.Server;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public abstract class SearchProvider<T> : ServerObjectBase, ISearchProvider<T>
    {

        private readonly IEnumerable<T> _source;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _isCancellationRequested;

        protected SearchProvider(IEnumerable<T> source)
        {
            _source = source;
        }

        public async void Start()
        {
            _isCancellationRequested = false;
            try
            {
                await Task.Run(() =>
                {
                    using (var enumerator = _source.GetEnumerator())
                        while (enumerator.MoveNext())
                        {
                            if (_isCancellationRequested)
                                break;
                            ItemAdded?.Invoke(this, new EventArgs<T>(enumerator.Current));
                        }
                });
                Finished?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }


        public void Cancel()
        {
            _isCancellationRequested = true;
        }

        public event EventHandler<EventArgs<T>> ItemAdded;

        public event EventHandler Finished;

    }
}
