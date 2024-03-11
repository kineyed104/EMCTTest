using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

using NUnit.Framework;

using SerialToTcpRelay;

using SerialToTcpRelayClient;

namespace SerialToTcpRelayTest
{
    public class Tests
    {
        //DummySerialModule 을 킨 상태로 해야한다.
        SerialToTcpRelayServer<DummySerialComm> relayServer;

        [SetUp]
        public void Setup()
        {
            List<string> acceptableIPList = new List<string>();
            acceptableIPList.Add("127.0.0.1");
            relayServer = new SerialToTcpRelayServer<DummySerialComm>(new DummySerialComm(), 19998, acceptableIPList);
            relayServer.Start();
            Thread.Sleep(5000);
        }

        [TearDown]
        public void TearDown()
        {
            if (relayServer != null)
                relayServer.Stop();

            Thread.Sleep(10);
        }

        [Test]
        public void ClientTest()
        {
            int result = 0;
            List<int> timeoutInteration = new List<int>();
            var client = new CIMONSerialToTcpRelayClient(IPAddress.Loopback, 19998);
            client.Start();
            Thread.Sleep(10);// for client socket listener ready
            Thread thread = new Thread(() =>
            {
                int count = 1000;
                while (count-- > 0)
                {
                    if (client.TryRequest(out byte[] response))
                        result++;
                    else if (response == null)
                        timeoutInteration.Add(1000-count);
                }
            });

            thread.Start();
            thread.Join();
            Assert.AreEqual(1000, result, $" {result} success  {timeoutInteration.Count()} timeout {(timeoutInteration.Count() > 0 ? timeoutInteration.Max() : 0)} interation");
        }

        [Test]
        public void MultiClientTest()
        {
            int testCount = 10;
            List<Thread> threads = new List<Thread>();
            Dictionary<int, int> results = new Dictionary<int, int>();
            Dictionary<int, CIMONSerialToTcpRelayClient> clients = new Dictionary<int, CIMONSerialToTcpRelayClient>();
            Dictionary<int, List<int>> timeoutInterations = new Dictionary<int, List<int>>();
            for (int i = 0; i < testCount; i++)
            {
                results[i] = 0;
                timeoutInterations[i] = new List<int>();
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
                            timeoutInterations[index].Add(10000 - count);
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
                Assert.AreEqual(1000, results[i], $"{i}/{testCount} thread {results[i]} success  {timeoutInterations[i].Count()} timeout {(timeoutInterations[i].Count() > 0 ? timeoutInterations[i].Max() : 0)} interation");
            }
        }
    }
}