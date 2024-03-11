using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToTcpRelay
{
    public class SerialToTcpRelayServer<T> where T : ISerialComm
    {
        private ISerialComm SerialPort;
        private Socket serverSocket;
        private bool running;
        private Thread acceptThread;
        private Thread sendQueueThread;
        private AutoResetEvent receiveWaiter = new AutoResetEvent(false);
        private byte[] serialReceiveBuffer = new byte[1024];
        private int serialReceiveBufferIndex;
        private Request currentRequest = null;
        private BlockingCollection<Request> requests = new BlockingCollection<Request>();
        private ConcurrentDictionary<Socket, Thread> clientSockets = new ConcurrentDictionary<Socket, Thread>();

        public List<string> AcceptableIPList { get; }
        public int Port { get; private set; }

        public SerialToTcpRelayServer(T serialPort, int port, List<string> acceptableIPList)
        {
            AcceptableIPList = acceptableIPList;
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
            serverSocket.Listen(100);
            running = true;
            acceptThread = new Thread(AcceptClient);
            acceptThread.Start();
            sendQueueThread = new Thread(SendQueueHandler);
            sendQueueThread.Start();
        }

        private void AcceptClient()
        {
            while (running)
            {
                try
                {
                    var clientSocket = serverSocket.Accept();
                    var ipEndpoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                    if (AcceptableIPList.Contains(ipEndpoint.Address.ToString()))
                        AttachClient(clientSocket);
                    else
                        clientSocket.Disconnect(false);
                }
                catch (SocketException ex)
                {
                    LogWrite($"SerialToTcpRelayServer Failed to accept. {ex.ToString()}");
                }
            }
        }

        private void AttachClient(Socket clientSocket)
        {
            Thread clientThread = new Thread(new ParameterizedThreadStart(ClientHandler));
            clientThread.Start(clientSocket);
            clientSockets.TryAdd(clientSocket, clientThread);
        }

        private void ClientHandler(object socket)
        {
            Socket clientSocket = (Socket)socket;
            byte[] buffer = new byte[1024];
            while (running && clientSocket != null && clientSocket.Connected)
            {
                try
                {
                    int bytesRead = clientSocket.Receive(buffer);
                    if (bytesRead > 0)
                    {
                        var bytes = new ArraySegment<byte>(buffer, 0, bytesRead).ToArray();
                        requests.Add(new Request(clientSocket, bytes));
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

        private void SendQueueHandler(object obj)
        {
            while (running)
            {
                if (requests.TryTake(out Request request, 10))
                {
                    currentRequest = request;
                    Array.Clear(serialReceiveBuffer, 0, serialReceiveBuffer.Length);
                    serialReceiveBufferIndex = 0;

                    SerialPort.Send(request.RequestBytes);
                    receiveWaiter.Reset();
                    if (!receiveWaiter.WaitOne(20))
                    {
                        int receivedIndex = serialReceiveBufferIndex;
                        var response = new byte[receivedIndex];
                        Array.Copy(serialReceiveBuffer, 0, response, 0, receivedIndex);
                        currentRequest.SetResponse(response);
                    }

                    ReplyToSocket(request);
                }
            }
        }

        private void SerialPort_DataReceived(ISerialComm serialComm, byte[] data)
        {
            data.CopyTo(serialReceiveBuffer, serialReceiveBufferIndex);
            serialReceiveBufferIndex += data.Length;

            if (TryInterpret(serialReceiveBuffer, out byte[] response))
            {
                currentRequest.SetResponse(response);
                receiveWaiter.Set();
            }
        }

        private bool TryInterpret(byte[] buffer, out byte[] response)
        {
            response = null;
            int header = -1, footer = -1;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0x02 && header == -1)
                    header = i;

                if (buffer[i] == 0x03 && header >= 0)
                    footer = i;
            }

            if (header < 0 || footer < 0)
                return false;

            var count = footer - header + 1;
            response = new byte[count];
            Array.Copy(buffer, header, response, 0, count);
            return true;
        }

        private void ReplyToSocket(Request request)
        {
            try
            {
                if (request.Socket.Connected && request.Response != null)
                    request.Socket.Send(request.Response);
            }
            catch (SocketException ex)
            {
                LogWrite($"ClientSocket Failed to send. {ex.ToString()}");
                RemoveClientSocket(request.Socket);
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
            sendQueueThread.Join();
        }

        object logLock = new object();
        private void LogWrite(string v)
        {
            lock (logLock)
                System.IO.File.AppendAllText($"{nameof(SerialToTcpRelay)}{DateTime.Now.ToShortDateString()}", $"[{DateTime.Now}] {v}");
        }
    }
}
