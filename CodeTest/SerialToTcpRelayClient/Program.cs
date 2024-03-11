using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace SerialToTcpRelayClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 10; i++) {
                TestMultiClient();
            }

            //TcpRelayClient client = new TcpRelayClient(System.Net.IPAddress.Loopback, 19998);
            //client.DataReceived += Client_DataReceived;
            //client.Start();

            //while (true)
            //{
            //    client.Send(new byte[] { 0x05, 0x30, 0x32, 0x52, 0x30, 0x41, 0x44, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x30, 0x31, 0x42, 0x39, 0x04 });
            //    Thread.Sleep(50);
            //}
        }

        private static void Client_DataReceived(TcpRelayClient client, byte[] data)
        {

            Console.WriteLine(Encoding.ASCII.GetString(data));
        }


        private static void TestMultiClient()
        {
            int testCount = 10;
            List<Thread> threads = new List<Thread>();
            Dictionary<int, int> results = new Dictionary<int, int>();
            Dictionary<int, CIMONSerialToTcpRelayClient> clients = new Dictionary<int, CIMONSerialToTcpRelayClient>();
            Dictionary<int, List<int>> timeoutList = new Dictionary<int, List<int>>();
            for (int i = 0; i < testCount; i++)
            {
                results[i] = 0;
                timeoutList[i] = new List<int>();
                clients[i] = new CIMONSerialToTcpRelayClient(IPAddress.Loopback, 19998);
                clients[i].Start();
            }

            Thread.Sleep(10);// for client socket listener ready

            for (int i = 0; i < testCount; i++)
            {
                Thread thread = new Thread(new ParameterizedThreadStart((idx) =>
                {
                    int index = (int)idx;

                    int count = 1000;
                    while (count-- > 0)
                    {
                        if (clients[index].TryRequest(out byte[] response))
                            results[index] = results[index] + 1;
                        else if (response == null)
                            timeoutList[index].Add(count);
                    }
                }));

                thread.Start(i);
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            for (int i = 0; i < testCount; i++)
            {
                Console.WriteLine($"{i}/{testCount} thread {results[i]} success  {timeoutList[i].Count()} timeout {(timeoutList[i].Count() > 0 ? timeoutList[i].Max() : 0)} interation");
            }
        }
    }
}
