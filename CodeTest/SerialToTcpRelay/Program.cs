using System;

namespace SerialToTcpRelay
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SerialToTcpRelayServer<DummySerialComm> relayServer = new SerialToTcpRelayServer<DummySerialComm>(new DummySerialComm(), 19998);
            relayServer.Start();

            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
