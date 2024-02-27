using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToTcpRelay
{
    internal class SerialToTcpRelayServer<T> where T : ISerialComm
    {
        private ISerialComm SerialPort;
        private Socket serverSocket;
        private ConcurrentDictionary<Socket, Thread> clientSockets;
        private bool running;
        private Thread acceptThread;

        public int Port { get; private set; }

        public SerialToTcpRelayServer(T serialPort, int port)
        {
            SerialPort = serialPort;
            SerialPort.DataReceived += SerialPort_DataReceived;
            Port = port;
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(ep);
        }

        public void Start()
        {
            SerialPort.Open();
            serverSocket.Listen(1);
            running = true;
            acceptThread = new Thread(AcceptClient);
            acceptThread.Start();
        }


        private void AcceptClient()
        {
            while (running)
            {
                try
                {
                    var clientSocket = serverSocket.Accept();
                    var ipEndpoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                        Thread clientThread = new Thread(ClientHandler);
                        clientThread.Start();
                    clientSockets.TryAdd(clientSocket, clientThread);
                    }

                    await Task.Delay(10);
                }
                catch (SocketException ex)
                {
                    LogWrite($"SerialToTcpRelayServer Failed to accept. {ex.ToString()}");
                }
            }
        }

        private void ClientHandler(object obj)
        {
            byte[] buffer = new byte[1024];
            while (running && clientSocket != null && clientSocket.Connected)
            {
                try
                {
                    int bytesRead = clientSocket.Receive(buffer);
                    if (bytesRead > 0)
                    {
                        var bytes = new ArraySegment<byte>(buffer,0, bytesRead).ToArray();
                        SerialPort.Send(bytes);
                    }
                }
                catch (SocketException ex)
                {
                    LogWrite($"ClientSocket Failed to received. {ex.ToString()}");
                    break;
                }
            }

            RemoveClientSocket(clientSocket);
            }

            clientSocket = null; 
        }

        private void SerialPort_DataReceived(ISerialComm serialComm, byte[] data)
        {
            if (clientSocket != null && clientSocket.Connected)
            if (clientSockets != null && clientSockets.Count > 0)
            {
                foreach (var clientSocketPair in clientSockets)
                {
                    if (clientSocketPair.Key.Connected)
                    {
                        SendToSocket(clientSocketPair.Key, data);
                    }
                }
            }
        }

        private void SendToSocket(Socket clientSocket, byte[] data)
            {
                try
                {
                    clientSocket.Send(data);
                }
                catch (SocketException ex)
                {
                    LogWrite($"ClientSocket Failed to send. {ex.ToString()}");
                RemoveClientSocket(clientSocket);
                }
            }

        private void RemoveClientSocket(Socket clientSocket)
        {
            clientSocket.Disconnect(false);
            clientSockets.TryRemove(clientSocket, out Thread clientThread);
            clientSocket.Dispose();
        }

        public void Stop()
        {
            running = false;
            SerialPort.Close();
            serverSocket.Close();
            if (clientSockets != null)
            {
                foreach (var clientSocketPair in clientSockets)
            {
                    clientSocketPair.Key.Close();
                }

                clientSockets.Clear();
            }

            acceptThread.Join();
        }


        private void LogWrite(string v)
        {
            System.IO.File.AppendAllText($"{nameof(SerialToTcpRelay)}{DateTime.Now.ToShortDateString()}", $"[{DateTime.Now}] {v}");
        }
    }
}
