using System;
using System.Threading;

namespace TAS.Server.VideoSwitch.Helpers
{
    internal class MessageRequest: IDisposable
    {
        private readonly ManualResetEventSlim _mutex = new ManualResetEventSlim();
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
            _mutex.Wait(token);
            return _result;
        }

    }
}
