using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialToTcpRelay
{
    public delegate void SerialCommDataReceivedEventHandler(ISerialComm serialComm, byte[] data);

    public interface ISerialComm
    {
        event SerialCommDataReceivedEventHandler DataReceived;
        void Close();
        void Send(byte[] sendBytes);
        void Open();
    }
}
