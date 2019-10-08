using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TAS.Client.NDIVideoPreview
{
    public class NevionCommunicator : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource cancellationToken;

        public bool Connect(string ip, int port)
        {
            try
            {
                _tcpClient = new TcpClient(ip, port);
                if (!_tcpClient.Connected)
                    return false;

                _stream = _tcpClient.GetStream();
                Listen();
                return false;
            }
            catch
            {
                return false;
            }            
        }

        public void Disconnect()
        {
            cancellationToken.Cancel();
        }

        public bool Send(string message)
        {
            return Task.Run(async() =>
            {
                try
                {                    
                    var data = Encoding.ASCII.GetBytes(String.Concat(message, "\n\n"));
                    await _stream.WriteAsync(data, 0, data.Length, cancellationToken.Token);
                    Debug.WriteLine($"Nevion message sent: {message}");
                    return true;
                }
                catch
                {
                    return false;
                }
            }).Result;          
        }

        private async void Listen()
        {
            await Task.Run(() =>
            {
                Byte[] bytesReceived = new Byte[256];
                string response = String.Empty;
                int bytes = 0;
                

                Console.WriteLine("Nevion listener started!");

                while (true)
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if ((bytes = _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length, cancellationToken.Token).Result) != 0)
                        {
                            response = System.Text.Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                            Console.WriteLine($"Received: {response}");

                            bytesReceived = new byte[256];
                            bytes = 0;
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"Failed to read nevion response. {ex.Message}");
                    }
                }


                Console.WriteLine("Nevion listener stopped!");
            });
        }

        public void Dispose()
        {
            Disconnect();
            _stream.Close();
            _tcpClient.Close();
        }        
    }
}
