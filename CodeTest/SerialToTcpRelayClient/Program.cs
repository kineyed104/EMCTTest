using System;
using System.Collections.Generic;
using System.Text;

namespace SerialToTcpRelayClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpRelayClient client = new TcpRelayClient(System.Net.IPAddress.Loopback, 19998);
            client.DataReceived += Client_DataReceived;
            client.Start();

            while (true)
            {
                var ascii = Console.ReadLine();
                client.Send(new byte[] { 0x05, 0x30, 0x32, 0x52, 0x30, 0x41, 0x44, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x30, 0x31, 0x42, 0x39, 0x04 });
            }
        }

        private static void Client_DataReceived(TcpRelayClient client, byte[] data)
        {
            Console.WriteLine(Encoding.ASCII.GetString(data));
        }
    }
}
