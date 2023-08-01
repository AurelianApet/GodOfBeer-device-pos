using System;
using System.Runtime.InteropServices;
using System.Threading;
using GodOfBeer.network;
using GodOfBeer.restful;
using GodOfBeer.util;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;
using SimpleJSON;

namespace device
{
    class Program
    {
        public const int broadcastServerPort = 15000;

        public static MainServer mainServer = ServerManager.Instance.mainServer;
        public static StatelessServer statelessServer = ServerManager.Instance.statelessServer;
        public static UdpSessionManager udpSessionManager = ServerManager.Instance.udpSessionManager;

        public static bool is_socket_open = false;

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void Main(string[] args)
        {
            ConfigSetting.api_prefix = @"/m-api/device/";
            string title = "ExistingSelfExe";
            Console.Title = title;
            IntPtr hWnd = FindWindow(null, title);
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, 2); // minimize the winodw  
            }

            Console.WriteLine("-----셀프단말기용 Exe-----");
            try
            {
                if (args.Length == 4)
                {
                    ConfigSetting.ShopId = int.Parse(args[0]);
                    ConfigSetting.ShopName = args[1];
                    ConfigSetting.IsOpen = bool.Parse(args[2]);
                    ConfigSetting.server_address = args[3];
                    Console.WriteLine("pub_id : " + ConfigSetting.ShopId);
                    Console.WriteLine("pub_name : " + ConfigSetting.ShopName);
                    Console.WriteLine("shop open : " + ConfigSetting.IsOpen);
                    Console.WriteLine("server_address : " + ConfigSetting.server_address);
                    ConfigSetting.api_server_domain = @"http://" + ConfigSetting.server_address + ":3006";
                    ConfigSetting.socketServerUrl = @"http://" + ConfigSetting.server_address + ":3006";
                    Console.WriteLine("api_url : " + ConfigSetting.api_server_domain);
                }
                mainServer.InitConfig(0);
                mainServer.CreateServer();
                mainServer.Start();
                statelessServer.InitConfig(0);
                statelessServer.CreateServer();
                statelessServer.Start();
                udpSessionManager.Start(broadcastServerPort);

                Socket socket = IO.Socket(ConfigSetting.socketServerUrl);

                socket.On(Socket.EVENT_CONNECT, () =>
                {
                    if (is_socket_open)
                    {
                        return;
                    }
                    is_socket_open = true;
                    Console.WriteLine("Setting Info Sended!");
                    var UserInfo = new JObject();
                    UserInfo.Add("pub_id", ConfigSetting.ShopId);
                    socket.Emit("existExeSetInfo", UserInfo);
                });

                socket.On(Socket.EVENT_CONNECT_ERROR, (data) =>
                {
                    Console.WriteLine("Socket Connect failed.");
                    is_socket_open = false;
                    socket.Close();
                    socket = IO.Socket(ConfigSetting.socketServerUrl);
                    //ApiClient.Instance.PosExeDisconnectFunc(ConfigSetting.ShopId);
                });

                socket.On(Socket.EVENT_DISCONNECT, (data) =>
                {
                    try
                    {
                        Console.WriteLine("Socket Disconnect.");
                        is_socket_open = false;
                        socket.Close();
                        socket = IO.Socket(ConfigSetting.socketServerUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception : " + ex);
                    }
                });

                socket.On("existExeSetSuccess", (data) =>
                {
                    Console.WriteLine("Socket Connected!");
                    ApiClient.Instance.PosExeSuccessFunc(ConfigSetting.ShopId);
                });

                socket.On("changeProductInfo", (data) =>
                {
                    JSONNode jsonNode = SimpleJSON.JSON.Parse(data.ToString());
                    string serial_number = jsonNode["serial_number"];
                    int product_id = jsonNode["product_id"].AsInt;
                    string product_name = jsonNode["product_name"];
                    int unit_price = jsonNode["unit_price"].AsInt;

                    //상품데이터 존재함
                    Int32 length = PacketInfo.HeaderSize + 276;//상품 설명, 이미지 데이터 없음.
                    Int32 opcode = (int)Opcode.REQ_CHANGE_PRODUCT_INFO;
                    Int64 reqid = 0;

                    Console.WriteLine("REQ_CHANGE_PRODUCT_INFO Sended!");
                    Console.WriteLine("Serial_number : " + serial_number);
                    Console.WriteLine("Product_id : " + product_id + ", name : " + product_name + ", unit_price : " + unit_price);

                    byte[] name = NetUtils.ConvertStringToByteArrayUTF8(product_name);
                    int descriptionSize = 0;
                    int imageSize = 0;
                    byte[] imageType = NetUtils.ConvertStringToByteArrayUTF8("bmp");

                    byte[] packet = new byte[length];
                    Array.Copy(BitConverter.GetBytes(length), 0, packet, 0, 4);
                    Array.Copy(BitConverter.GetBytes(opcode), 0, packet, 4, 4);
                    Array.Copy(BitConverter.GetBytes(reqid), 0, packet, 8, 8);

                    Array.Copy(BitConverter.GetBytes(product_id), 0, packet, 16, 4);
                    Array.Copy(name, 0, packet, 20, name.Length);//256
                    Array.Copy(BitConverter.GetBytes(unit_price), 0, packet, 276, 4);
                    Array.Copy(BitConverter.GetBytes(descriptionSize), 0, packet, 280, 4);
                    Array.Copy(BitConverter.GetBytes(imageSize), 0, packet, 284, 4);
                    Array.Copy(imageType, 0, packet, 288, imageType.Length);

                    mainServer.WriteToClient(serial_number, packet);

                    statelessServer.WriteToLoginedClient(serial_number, Opcode.REQ_GET_PRODUCT_INFO);
                });

                socket.On("changeShopStatus", (data) =>
                {
                    JSONNode jsonNode = SimpleJSON.JSON.Parse(data.ToString());
                    int status = jsonNode["status"].AsInt;
                    ConfigSetting.IsOpen = status == 0 ? false : true;

                    //상품데이터 존재함
                    Int32 length = PacketInfo.HeaderSize + 264;
                    Int32 opcode = (int)Opcode.REQ_SET_SHOP_INFO;
                    Int64 reqid = 0;

                    Console.WriteLine("REQ_SET_SHOP_INFO Sended!");
                    Console.WriteLine("Shop Id : " + ConfigSetting.ShopId);
                    Console.WriteLine("Shop Name : " + ConfigSetting.ShopName);
                    Console.WriteLine("Shop Status : " + status);

                    byte[] name = NetUtils.ConvertStringToByteArrayUTF8(ConfigSetting.ShopName);

                    byte[] packet = new byte[length];
                    Array.Copy(BitConverter.GetBytes(length), 0, packet, 0, 4);
                    Array.Copy(BitConverter.GetBytes(opcode), 0, packet, 4, 4);
                    Array.Copy(BitConverter.GetBytes(reqid), 0, packet, 8, 8);

                    Array.Copy(BitConverter.GetBytes(ConfigSetting.ShopId), 0, packet, 16, 4);
                    Array.Copy(name, 0, packet, 20, name.Length);//256
                    Array.Copy(BitConverter.GetBytes(status), 0, packet, 276, 4);

                    ServerManager.Instance.mainServer.WriteToAllLoginedClient(packet);

                    ServerManager.Instance.statelessServer.WriteToAllLoginedClient(Opcode.REQ_GET_SHOP_INFO);
                });

                Console.ReadLine();
            } catch(Exception ex)
            {
                Console.WriteLine("Exception : " + ex);
            }
        }
    }
}
