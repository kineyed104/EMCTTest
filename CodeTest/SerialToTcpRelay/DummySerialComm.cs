using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace SerialToTcpRelay
{
    public class DummySerialComm : ISerialComm
    {
        public event SerialCommDataReceivedEventHandler DataReceived;
        Random rand0;
        TcpClient client;
        private bool running = false;
        Thread receivethread;
        Thread sendThread;
        ConcurrentQueue<byte> queue = new ConcurrentQueue<byte>();

        public DummySerialComm()
        {
            int baseSeed = (int)DateTime.Now.Ticks;
            rand0 = new Random(baseSeed);
        }

        public void Open()
        {
            client = new TcpClient();
            client.ConnectAsync(System.Net.IPAddress.Loopback, 19999).Wait();
            running = true;
            receivethread = new Thread(ReceiveHandler);
            sendThread = new Thread(SendQueueHandler);
            receivethread.Start();
            sendThread.Start();
        }

        public void Close()
        {
            running = false;
            if (client != null)
            {
                client.Close();
                client = null;
            }
            receivethread.Join();
            sendThread.Join();
        }

        private void ReceiveHandler(object obj)
        {
            var buffer = new byte[1024];
            var stream = client.GetStream();

            while (running)
            {
                int readCount = stream.ReadAsync(buffer, 0, buffer.Length).Result;
                if (readCount > 0)
                {
                    var data = new ArraySegment<byte>(buffer, 0, readCount).ToArray();
                    DataReceived?.Invoke(this, data);
                }
            }
        }

        private void SendQueueHandler(object obj)
        {
            while (true)
            {
                List<byte> datas = new List<byte>();
                var count = queue.Count;
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (queue.TryDequeue(out byte byteData))
                            datas.Add(byteData);
                        else
                            break;
                    }

                    if (datas.Count > 0)
                    {
                        var stream = client.GetStream();
                        stream.Write(datas.ToArray(), 0, datas.Count);
                    }
                }
            }
        }

        public void Send(byte[] sendBytes)
        {
            foreach (var item in sendBytes)
            {
                queue.Enqueue(item);
            }
        }
    }
}