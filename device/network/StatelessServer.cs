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
    public class StatelessServer
        : AppServer<StatelessNetworkSession, PacketInfo>
    {
        Dictionary<int, Action<StatelessNetworkSession, PacketInfo>> HandlerMap = new Dictionary<int, Action<StatelessNetworkSession, PacketInfo>>();
        StatelessHandler handler = new StatelessHandler();

        IServerConfig m_Config;

        public StatelessServer()
            : base(new DefaultReceiveFilterFactory<PakcetReceiveFilter, PacketInfo>())
        {
            NewSessionConnected += new SessionHandler<StatelessNetworkSession>(OnConnected);
            SessionClosed += new SessionHandler<StatelessNetworkSession, CloseReason>(OnClosed);
            NewRequestReceived += new RequestHandler<StatelessNetworkSession, PacketInfo>(RequestReceived);

            RegistHandler();
        }


        void RegistHandler()
        {
            HandlerMap.Add((int)Opcode.REQ_PING, handler.REQ_PING);
            HandlerMap.Add((int)Opcode.REQ_GET_CHANGED_OPCODES, handler.REQ_GET_CHANGED_OPCODES);
            HandlerMap.Add((int)Opcode.REQ_AUTH_DEVICE, handler.REQ_AUTH_DEVICE);
            HandlerMap.Add((int)Opcode.REQ_GET_TAG_INFO, handler.REQ_GET_TAG_INFO);
            HandlerMap.Add((int)Opcode.REQ_GET_PRODUCT_INFO, handler.REQ_GET_PRODUCT_INFO);
            HandlerMap.Add((int)Opcode.REQ_GET_SHOP_INFO, handler.REQ_GET_SHOP_INFO);
            HandlerMap.Add((int)Opcode.REQ_SET_DEVICE_STATUS, handler.REQ_SET_DEVICE_STATUS);
            HandlerMap.Add((int)Opcode.REQ_SET_FLOWMETER_VALUE, handler.REQ_SET_FLOWMETER_VALUE);
            HandlerMap.Add((int)Opcode.REQ_SET_PRODUCT_CONSUMPTION, handler.REQ_SET_PRODUCT_CONSUMPTION);
            HandlerMap.Add((int)Opcode.REQ_GET_PRODUCT_QAUNTITY, handler.REQ_GET_PRODUCT_QAUNTITY);
            HandlerMap.Add((int)Opcode.REQ_SET_MILLILITER_PER_PULSE, handler.REQ_SET_MILLILITER_PER_PULSE);
            HandlerMap.Add((int)Opcode.ERROR_MESSAGE, handler.ERROR_MESSAGE);

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
                Name = "StatelessServer"
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
            Console.WriteLine(string.Format("Status 서버 생성 성공"));
        }

        public bool IsRunning(ServerState eCurState)
        {
            if (eCurState == ServerState.Running)
            {
                return true;
            }

            return false;
        }

        void OnConnected(StatelessNetworkSession session)
        {
            //DevLog.Write(string.Format("세션 번호 {0} 접속", session.SessionID), LOG_LEVEL.INFO);
            //Console.WriteLine(string.Format("세션 번호 {0} 접속", session.SessionID));
            //Console.WriteLine("remote ip : " + session.RemoteEndPoint.Address.ToString() + " / " + session.RemoteEndPoint.Port);
            //NetUtils.GetMACAddressFromARP(session.RemoteEndPoint.Address);
        }

        void OnClosed(StatelessNetworkSession session, CloseReason reason)
        {
            //DevLog.Write(string.Format("세션 번호 {0} 접속해제: {1}", session.SessionID, reason.ToString()), LOG_LEVEL.INFO);
            //Console.WriteLine(string.Format("세션 번호 {0} 접속해제: {1}", session.SessionID, reason.ToString()));
        }

        void RequestReceived(StatelessNetworkSession session, PacketInfo reqInfo)
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

        public void WriteToLoginedClient(string deviceSerial, Opcode opcode)
        {
            if (string.IsNullOrEmpty(deviceSerial))
                return;

            TokenManager.Instance.PushReservedOpcodeToSession(deviceSerial, opcode);
        }

        public void WriteToAllLoginedClient(Opcode opcode)
        {
            TokenManager.Instance.PushReservedOpcodeToAllSessions(opcode);
        }
    }

    public class StatelessNetworkSession : AppSession<StatelessNetworkSession, PacketInfo>
    {
        public StatelessNetworkSession()
        {
            
        }
    }
}
