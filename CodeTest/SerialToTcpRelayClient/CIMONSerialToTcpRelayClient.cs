using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToTcpRelayClient
{
    public class CIMONSerialToTcpRelayClient
    {
        TcpRelayClient tcpRelayClient;
        private AutoResetEvent receiveWaiter = new AutoResetEvent(false);
        byte[] response;
        public CIMONSerialToTcpRelayClient(IPAddress ipAddress, int port)
        {
            tcpRelayClient = new TcpRelayClient(ipAddress, port);
            tcpRelayClient.DataReceived += TcpRelayClient_DataReceived;
        }

        private void TcpRelayClient_DataReceived(TcpRelayClient client, byte[] data)
        {
            response = data;
            receiveWaiter.Set();
        }

        public void Start()
        {
            tcpRelayClient.Start();
        }

        public void Stop()
        {
            tcpRelayClient.Stop();
        }

        public bool TryRequest(out byte[] result)
        {
            result = null;
            tcpRelayClient.Send(new byte[] { 0x05, 0x30, 0x32, 0x52, 0x30, 0x41, 0x44, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x30, 0x31, 0x42, 0x39, 0x04 });
            if (!receiveWaiter.WaitOne(1000))
                return false;

            if (response[0] == 0x02 && response[response.Length - 1] == 0x03)
            {
                result = response;
                return true;
            }
            else
            {
                result = response;
                return false;
            }
        }
    }
}
