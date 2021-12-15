using System;
using System.Threading;

namespace TAS.Server.VideoSwitch.Helpers
{
    internal class MessageRequest<T>: IDisposable where T: class
    {
        private readonly ManualResetEvent _mutex = new ManualResetEvent(false);
        private T _result;

        public void Dispose()
        {
            _mutex.Dispose();
        }

        public void SetResult(T result)
        {
            _result = result;
            _mutex.Set();
        }

        public T WaitForResult(CancellationToken token)
        {
            if (WaitHandle.WaitAny(new [] { _mutex, token.WaitHandle }) == 1)
                throw new OperationCanceledException();
            _mutex.Reset();
            return _result;
        }

        public T WaitForResult(CancellationToken token, int millisecondsTimeout)
        {
            switch (WaitHandle.WaitAny(new[] { _mutex, token.WaitHandle }, millisecondsTimeout))
            {
                case 1:
                    throw new OperationCanceledException();
                case WaitHandle.WaitTimeout:
                    throw new TimeoutException("Timeout waiting for result");
                case 0:
                    _mutex.Reset();
                    return Interlocked.Exchange(ref _result, null);
                default:
                    throw new InvalidOperationException();
            }
        }

        public object SyncRoot { get; } = new object();

    }
}
