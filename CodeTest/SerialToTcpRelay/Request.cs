using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SerialToTcpRelay
{
    public class Request
    {
        public Request(Socket socket, byte[] requestBytes)
        {
            Socket = socket;
            RequestBytes = requestBytes;
        }

        public Socket Socket { get;  }
        public byte[] RequestBytes { get;  }
        public byte[] Response { get; private set; }

        public void SetResponse(byte[] responseBytes)
        {
            Response = responseBytes;
        }
    }

}