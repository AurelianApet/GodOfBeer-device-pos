using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;
using System.Diagnostics;

using SuperSocket.SocketBase.Logging;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketBase.Config;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace GodOfBeer.network
{
    public class MainServer : AppServer<NetworkSession, PacketInfo>
    {
        Dictionary<int, Action<NetworkSession, PacketInfo>> HandlerMap = new Dictionary<int, Action<NetworkSession, PacketInfo>>();
        CommonHandler CommonHan = new CommonHandler();

        IServerConfig m_Config;

        public MainServer()
            : base(new DefaultReceiveFilterFactory<PakcetReceiveFilter, PacketInfo>())
        {
            NewSessionConnected += new SessionHandler<NetworkSession>(OnConnected);
            SessionClosed += new SessionHandler<NetworkSession, CloseReason>(OnClosed);
            NewRequestReceived += new RequestHandler<NetworkSession, PacketInfo>(RequestReceived);

            RegistHandler();
        }


        void RegistHandler()
        {
            HandlerMap.Add((int)Opcode.REQ_PING, CommonHan.REQ_PING);
            HandlerMap.Add((int)Opcode.RES_PING, CommonHan.RES_PING);
            HandlerMap.Add((int)Opcode.REQ_AUTH_DEVICE, CommonHan.REQ_AUTH_DEVICE);
            HandlerMap.Add((int)Opcode.REQ_GET_TAG_INFO, CommonHan.REQ_GET_TAG_INFO);
            HandlerMap.Add((int)Opcode.REQ_GET_PRODUCT_INFO, CommonHan.REQ_GET_PRODUCT_INFO);
            HandlerMap.Add((int)Opcode.RES_CHANGE_PRODUCT_INFO, CommonHan.RES_GET_CHANGE_PRODUCT_INFO);
            HandlerMap.Add((int)Opcode.REQ_GET_SHOP_INFO, CommonHan.REQ_GET_SHOP_INFO);
            HandlerMap.Add((int)Opcode.RES_SET_SHOP_INFO, CommonHan.RES_SET_SHOP_INFO);
            HandlerMap.Add((int)Opcode.RES_GET_DEVICE_STATUS, CommonHan.RES_GET_DEVICE_STATUS);
            HandlerMap.Add((int)Opcode.REQ_SET_DEVICE_STATUS, CommonHan.REQ_SET_DEVICE_STATUS);
            HandlerMap.Add((int)Opcode.REQ_SET_FLOWMETER_VALUE, CommonHan.REQ_SET_FLOWMETER_VALUE);
            HandlerMap.Add((int)Opcode.REQ_SET_PRODUCT_CONSUMPTION, CommonHan.REQ_SET_PRODUCT_CONSUMPTION);
            HandlerMap.Add((int)Opcode.REQ_GET_PRODUCT_QAUNTITY, CommonHan.REQ_GET_PRODUCT_QAUNTITY);
            HandlerMap.Add((int)Opcode.REQ_SET_MILLILITER_PER_PULSE, CommonHan.REQ_SET_MILLILITER_PER_PULSE);
            HandlerMap.Add((int)Opcode.ERROR_MESSAGE, CommonHan.ERROR_MESSAGE);

            DevLog.Write(string.Format("핸들러 등록 완료"), LOG_LEVEL.INFO);
        }

        public void InitConfig(int port)
        {
            if (port == 0)
            {
                TcpListener tl = new TcpListener(IPAddress.Any, 0);
                tl.Start();
                port = ((IPEndPoint)tl.Server.LocalEndPoint).Port;
                tl.Stop();
            }

            m_Config = new ServerConfig
            {
                Port = port,
                Ip = "Any",
                MaxConnectionNumber = 100,
                Mode = SocketMode.Tcp,
                Name = "MainServer"
            };
        }

        public void CreateServer()
        {
            bool bResult = Setup(new RootConfig(), m_Config, logFactory: new Log4NetLogFactory());

            if (bResult == false)
            {
                //DevLog.Write(string.Format("[ERROR] 서버 네트워크 설정 실패"), LOG_LEVEL.ERROR);
                Console.WriteLine(string.Format("[ERROR] 서버 네트워크 설정 실패"));
                return;
            }

            //DevLog.Write(string.Format("서버 생성 성공"), LOG_LEVEL.INFO);
            Console.WriteLine(string.Format("Main 서버 생성 성공"));
        }

        public bool IsRunning(ServerState eCurState)
        {
            if (eCurState == ServerState.Running)
            {
                return true;
            }

            return false;
        }

        void OnConnected(NetworkSession session)
        {
            //DevLog.Write(string.Format("세션 번호 {0} 접속", session.SessionID), LOG_LEVEL.INFO);
            Console.WriteLine(string.Format("세션 번호 {0} 접속", session.SessionID));
        }

        void OnClosed(NetworkSession session, CloseReason reason)
        {
            //DevLog.Write(string.Format("세션 번호 {0} 접속해제: {1}", session.SessionID, reason.ToString()), LOG_LEVEL.INFO);
            Console.WriteLine(string.Format("세션 번호 {0} 접속해제: {1}", session.SessionID, reason.ToString()));
            if (session.Token != 0)
            {
                TokenManager.Instance.ReleaseToken(session.Token);
            }
        }

        void RequestReceived(NetworkSession session, PacketInfo reqInfo)
        {
            //DevLog.Write(string.Format("세션 번호 {0} 받은 데이터 크기: {1}, ThreadId: {2}", session.SessionID, reqInfo.Body.Length, System.Threading.Thread.CurrentThread.ManagedThreadId), LOG_LEVEL.INFO);
            //Console.WriteLine(string.Format("세션 번호 {0} 받은 데이터 크기: {1}, ThreadId: {2}", session.SessionID, reqInfo.Body.Length, System.Threading.Thread.CurrentThread.ManagedThreadId));

            var length = reqInfo.length;
            var opcode = reqInfo.opcode;
            var reqid = reqInfo.reqid;
            //Console.WriteLine($"[TCP][RECV] opcode(hex) : {opcode:X} , {HandlerMap.ContainsKey(opcode)}, {HandlerMap.Keys.Count}");
            if (HandlerMap.ContainsKey(opcode))
            {
                Opcode op = (Opcode)opcode;
                Console.WriteLine("[TCP][RECV] :" + op.ToString());
                HandlerMap[opcode](session, reqInfo);
            }
            else
            {
                Console.WriteLine("[TCP][RECV] : opcode error. session will be closing");
                session.Close();
            }
        }

        public void WriteToClient(string deviceSerial, byte[] data)
        {
            if (string.IsNullOrEmpty(deviceSerial))
                return;

            var sessions = this.GetAllSessions().Where(v => v.DeviceSerial.Equals(deviceSerial));
            if (!sessions.Any())
                return;

            var session = sessions.First();

            session.Send(data, 0, data.Length);
        }

        public void WriteToAllClient(byte[] data)
        {
            var sessions = this.GetAllSessions();
            foreach (var session in sessions)
            {
                session.Send(data, 0, data.Length);
            }
        }

        public void WriteToAllLoginedClient(byte[] data)
        {
            //Console.WriteLine("try send broadcast");
            var sessions = this.GetAllSessions().Where(v=>v.IsLogin);
            foreach(var session in sessions)
            {
                Console.WriteLine("send broadcast to session : " + session.DeviceSerial);
                session.Send(data, 0, data.Length);
            }
        }

        public void ConnectionChecker()
        {
            DateTime now = DateTime.Now;

            var sessions = this.GetAllSessions().Where(v => v.IsLogin);
            foreach (var session in sessions)
            {
                if (now.Subtract(session.Timestamp).TotalSeconds > 3600)
                {
                    Console.WriteLine("disconnect by server!!!");
                    session.Close();
                }
            }

            Int32 length = PacketInfo.HeaderSize + 8;
            Int32 opcode = (int)Opcode.RES_PING;
            Int64 reqid = 9800;

            byte[] packet = new byte[length];
            Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
            Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
            Array.Copy(NetUtils.GetBytes(reqid), 0, packet, 8, 8);
            Array.Copy(NetUtils.GetBytes(NetUtils.ConvertDateTimeToNetDate(now)), 0, packet, 16, 4);
            Array.Copy(NetUtils.GetBytes(NetUtils.ConvertDateTimeToNetTime(now)), 0, packet, 20, 4);

            WriteToAllLoginedClient(packet);
        }
    }


    public class NetworkSession : AppSession<NetworkSession, PacketInfo>
    {
        public bool IsLogin { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceSerial { get; set; }
        public int DeviceId { get; set; }
        public int Status { get; set; }
        public bool IsValveOn { get; set; }
        public long Token { get; set; }

        public NetworkSession()
        {
            IsLogin = false;
            Timestamp = DateTime.MinValue;
            DeviceSerial = "";
            DeviceId = 0;
            Status = 0;
            IsValveOn = false;
            Token = 0;
        }
    }

    public class UdpSessionManager
    {
        bool flag = false;
        UdpSession udpSession = null;
        AsyncCallback receiveCallback;

        public bool Start(int port)
        {
            flag = true;
            receiveCallback = new AsyncCallback(OnUdpReceive);
            udpSession = new UdpSession(port, receiveCallback);

            var a = NetUtils.GetDirectedBroadcastAddresses();
            foreach(var aa in a)
            {
                Console.WriteLine(aa.ToString());
            }

            return true;
        }
        public void Stop()
        {
            flag = false;
            if (udpSession != null)
            {
                udpSession.Close();
                udpSession = null;
            }
            if (receiveCallback != null)
            {
                receiveCallback = null;
            }
        }

        public void OnUdpReceive(IAsyncResult result)
        {
            try
            {
                Console.WriteLine("[UDP][RECV]");
                if (!flag) return;
                UdpClient socket = result.AsyncState as UdpClient;

                IPEndPoint source = new IPEndPoint(0, 0);

                byte[] packet = socket.EndReceive(result, ref source);

                Console.WriteLine("[UDP][RECV] ip : " + source.Address.ToString());

                int length = NetUtils.ToInt32(packet, 0);
                int opcode = NetUtils.ToInt32(packet, 4);
                long reqid = NetUtils.ToInt64(packet, 8);
                long token = NetUtils.ToInt64(packet, 16);

                if (packet.Length == length)
                {
                    Console.WriteLine("[UDP][RECV] length : " + length);

                    byte[] body = null;
                    if (length > PacketInfo.HeaderSize)
                    {
                        body = new byte[length - PacketInfo.HeaderSize];
                        Array.Copy(packet, PacketInfo.HeaderSize, body, 0, length - PacketInfo.HeaderSize);
                    }

                    switch ((Opcode)opcode)
                    {
                        case Opcode.REQ_SERVER_INFO:
                            REQ_SERVER_INFO(source, reqid, body);
                            break;
                        default:
                            Debug.WriteLine("Receive(udp) Wrong Opcode : 0x" + opcode.ToString("X8"));
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Receive(udp) Wrong Length : Header Length("+length+")" +", Real Length("+packet.Length+")");
                }
                socket.BeginReceive(receiveCallback, socket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void REQ_SERVER_INFO(IPEndPoint source, long reqid, byte[] body)
        {
            int type = NetUtils.ToInt32(body, 0);
            if (type == 0)
            {
                if (ServerManager.Instance.mainServer.Listeners.Any())
                {
                    int port = ServerManager.Instance.mainServer.Listeners[0].EndPoint.Port;
                    udpSession.Send(source, NetUtils.Reverse((int)Opcode.RES_SERVER_INFO), NetUtils.Reverse(reqid), 0, NetUtils.GetBytes(port));
                }
            }
            else if (type == 1)
            {
                if (ServerManager.Instance.statelessServer.Listeners.Any())
                {
                    int port = ServerManager.Instance.statelessServer.Listeners[0].EndPoint.Port;
                    udpSession.Send(source, NetUtils.Reverse((int)Opcode.RES_SERVER_INFO), NetUtils.Reverse(reqid), 0, NetUtils.GetBytes(port));
                }
            }
            else
            {
                Int32 errorOpcode = (int)Opcode.REQ_SERVER_INFO;
                Int32 errorCode = (int)Errorcode.ERR_SERVER_INTERNAL;
                byte[] message = new byte[256];

                byte[] packet = new byte[264];
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 4, 4);
                Array.Copy(message, 0, packet, 8, 256);

                udpSession.Send(source, NetUtils.Reverse((int)Opcode.ERROR_MESSAGE), NetUtils.Reverse(reqid), 0, packet);
            }            
        }
    }
}
