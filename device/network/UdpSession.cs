using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GodOfBeer.network
{
    class UdpSession
    {
        AsyncCallback receiveCallback;
        UdpClient udpSocket;

        public UdpSession(int port, AsyncCallback OnReceiveCallback)
        {
            udpSocket = new UdpClient(port);

            Console.WriteLine("UdpSession created! Port : " + port);
            //udpSocket set ioctl/////////////
            const int SIO_UDP_CONNRESET = -1744830452;
            udpSocket.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            //////////////////////////////////
            
            udpSocket.EnableBroadcast = true;
            receiveCallback = OnReceiveCallback;
            udpSocket.BeginReceive(receiveCallback, udpSocket);
        }

        public void Close()
        {
            if (udpSocket != null)
            {
                udpSocket.Close();
                udpSocket = null;
                receiveCallback = null;
            }
        }

        public void Broadcast(int port, int opcode, long reqid, long token, byte[] data)
        {
            IPAddress[] broadcastAddresses = NetUtils.GetDirectedBroadcastAddresses();
            foreach (IPAddress broadcastAddress in broadcastAddresses)
            {
                Send(broadcastAddress.ToString(), port, opcode, reqid, token, data);
            }
        }

        public void Send(string ip, int port, int opcode, long reqid, long token, byte[] data)
        {
            IPEndPoint target = new IPEndPoint(IPAddress.Parse(ip), port);
            Send(target, opcode, reqid, token, data);
        }

        public void Send(IPEndPoint target, int opcode, long reqid, long token, byte[] data)
        {
            int length = PacketInfo.HeaderSize + (data == null ? 0 : data.Length);
            byte[] _length = BitConverter.GetBytes(length);
            byte[] _opcode = BitConverter.GetBytes(opcode);
            byte[] _reqid = BitConverter.GetBytes(reqid);
            byte[] _token = BitConverter.GetBytes(token);

            byte[] sendData = new byte[length];
            Array.Copy(_length, 0, sendData, 0, _length.Length);
            Array.Copy(_opcode, 0, sendData, _length.Length, _opcode.Length);
            Array.Copy(_reqid, 0, sendData, _length.Length + _opcode.Length, _reqid.Length);
            Array.Copy(_token, 0, sendData, _length.Length + _opcode.Length + _reqid.Length, _token.Length);
            if (length > 16)
                Array.Copy(data, 0, sendData, _length.Length + _opcode.Length + _reqid.Length + _token.Length, data.Length);

            int result = udpSocket.Send(sendData, sendData.Length, target);

            Console.WriteLine("[UDP][SEND] result : " + result.ToString() + ", remote addr : " + target.Address.ToString() + ":" + target.Port);
        }
    }
}
