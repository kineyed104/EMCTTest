using System;
using System.Collections.Generic;

namespace SerialToTcpRelay
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<string> acceptableIPList = new List<string>();
            acceptableIPList.Add("127.0.0.1");
            SerialToTcpRelayServer<DummySerialComm> relayServer = new SerialToTcpRelayServer<DummySerialComm>(new DummySerialComm(), 19998, acceptableIPList);
            relayServer.Start();

            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
