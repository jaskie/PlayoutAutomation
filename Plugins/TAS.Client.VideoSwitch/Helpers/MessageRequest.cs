using System;
using System.Threading;

namespace TAS.Server.VideoSwitch.Helpers
{
    internal class MessageRequest: IDisposable
    {
        private readonly ManualResetEvent _mutex = new ManualResetEvent(false);
        private byte[] _result;

        public void Dispose()
        {
            _mutex.Dispose();
        }

        public void SetResult(byte[] message)
        {
            _result = message;
            _mutex.Set();
        }

        public byte[] WaitForResult(CancellationToken token)
        {
            WaitHandle.WaitAny(new [] { token.WaitHandle, _mutex });
            _mutex.Reset();
            return Interlocked.Exchange(ref _result, null);
        }

        public object SyncRoot { get; } = new object();

    }
}
