using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Threading;

namespace SerialToTcpRelayClient
{
    public delegate void TcpRelayClientDataReceivedEventHandler(TcpRelayClient client, byte[] data);
    public class TcpRelayClient
    {
        private Socket clientSocket;
        private IPEndPoint serverEndPoint;
        private Thread receiveThread;
        private bool running;

        public event TcpRelayClientDataReceivedEventHandler DataReceived;

        public TcpRelayClient(IPAddress serverIp, int serverPort)
        {
            serverEndPoint = new IPEndPoint(serverIp, serverPort);
        }

        public void Start()
        {
            running = true;
            receiveThread = new Thread(ReceiveHandler);
            receiveThread.Start();
        }

        public void InitailizeSocket()
        {
            ClearSocket();
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.ConnectAsync(serverEndPoint).Wait();
        }

        public void ReceiveHandler()
        {
            while (running)
            {
                InitailizeSocket();

                try
                {
                    var buffer = new byte[1024];
                    while (clientSocket.Connected && running)
                    {
                        var bytesRead = clientSocket.ReceiveAsync(buffer, SocketFlags.None).Result;
                        if (bytesRead > 0)
                        {
                            var bytes = new ArraySegment<byte>(buffer, 0, bytesRead).ToArray();
                            DataReceived?.Invoke(this, bytes);
                        }
                    }
                }
                catch (SocketException)
                {
                }
            }
        }

        public void Send(byte[] bytes)
        {
            try
            {
                if (clientSocket != null && clientSocket.Connected)
                    clientSocket.Send(bytes);
            }
            catch (SocketException)
            {
                ClearSocket();
            }
        }

        private void ClearSocket()
        {
            if (clientSocket != null)
            {
                clientSocket.Disconnect(false);
                clientSocket = null;
            }
        }

        public void Stop()
        {
            running = false;
            if (clientSocket != null)
            {
                clientSocket.Disconnect(false);
                clientSocket = null;
            }
            receiveThread.Join();
        }
    }
}
