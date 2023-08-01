using GodOfBeer.util;
using GodOfBeer.restful;
using System;
using System.Linq;

namespace GodOfBeer.network
{
    #region CommonHandler
    public class CommonHandler
    {
        TokenManager tm = TokenManager.Instance;
        public void REQ_PING(NetworkSession session, PacketInfo requestInfo)
        {
            // uint64 TIMESTAMP 본 패킷의 전송 시간
            int date = NetUtils.ToInt32(requestInfo.Body, 0);
            int time = NetUtils.ToInt32(requestInfo.Body, 4);

            DateTime now = DateTime.Now;

            Int32 length = PacketInfo.HeaderSize + 8;
            Int32 opcode = (int)Opcode.RES_PING;


            byte[] packet = new byte[length];
            Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
            Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
            Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
            Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
            int pos = 0;
            Array.Copy(NetUtils.GetBytes(NetUtils.ConvertDateTimeToNetDate(now)), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
            Array.Copy(NetUtils.GetBytes(NetUtils.ConvertDateTimeToNetTime(now)), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;

            session.Send(packet, 0, packet.Length);

            session.Timestamp = now;
        }
        public void RES_PING(NetworkSession session, PacketInfo requestInfo)
        {
            // uint64 TIMESTAMP REQ_PING 패킷의 TIMESTAMP 값
            int date = NetUtils.ToInt32(requestInfo.Body, 0);
            int time = NetUtils.ToInt32(requestInfo.Body, 4);

            session.Timestamp = NetUtils.ConvertNetDatetimeToDateTime(date, time);
        }
        public void REQ_AUTH_DEVICE(NetworkSession session, PacketInfo requestInfo)
        {
            // char[128] DEVICE_SERIAL 장비 시리얼 문자열
            // char[128] AUTH_KEY 인증 KEY
            byte[] device_serial = new byte[128];
            Array.Copy(requestInfo.Body, 0, device_serial, 0, 128);
            byte[] auth_key = new byte[128];
            Array.Copy(requestInfo.Body, 128, auth_key, 0, 128);

            string str_device_serial = NetUtils.ConvertByteArrayToStringASCII(device_serial);
            string str_auth_key = NetUtils.ConvertByteArrayToStringASCII(auth_key);

            var sessions = session.AppServer.GetAllSessions();
            if (sessions.Where(v => v != session && v.DeviceSerial.Equals(str_device_serial)).Any())
            {
                Console.WriteLine("인증 실패 : 동일 장비 있음");
                // 동일 장비 있음
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_ALREADY_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);
            }
            else
            {
                // 인증 처리
                var res = ApiClient.Instance.FindTapApiFunc(ConfigSetting.ShopId, str_device_serial);
                if (res.suc == 1)
                {
                    Console.WriteLine("인증 : 성공");
                    // 성공
                    session.IsLogin = true;
                    session.DeviceSerial = str_device_serial;
                    session.DeviceId = int.Parse(res.dataMap["tap_id"].ToString());
                    session.Timestamp = DateTime.Now;
                    session.Token = tm.CreateNewToken(session.DeviceSerial, false);

                    Int32 length = PacketInfo.HeaderSize;
                    Int32 opcode = (int)Opcode.RES_AUTH_DEVICE;

                    byte[] packet = new byte[length];
                    Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                    Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                    Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                    Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                    session.Send(packet, 0, packet.Length);
                }
                else
                {
                    Console.WriteLine("인증 실패 : 미등록 장비");
                    // 실패
                    Int32 length = PacketInfo.HeaderSize + 264;
                    Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                    Int32 errorOpcode = requestInfo.opcode;
                    Int32 errorCode = (int)Errorcode.ERR_INVALID_SERIAL;
                    byte[] message = new byte[256];

                    byte[] packet = new byte[length];
                    Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                    Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                    Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                    Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                    int pos = 0;
                    Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                    Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                    Array.Copy(message, 0, packet, 32, 256);

                    session.Send(packet, 0, packet.Length);
                }
            }
        }

        public void REQ_GET_TAG_INFO(NetworkSession session, PacketInfo requestInfo)
        {
            if (!session.IsLogin)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;

                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_NOT_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }

            if (!ConfigSetting.IsOpen)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;

                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_SERVER_INTERNAL;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }

            // char[256] NFC_VALUE 확인 대상 NFC 값 문자열
            byte[] tag_value = new byte[256];
            Array.Copy(requestInfo.Body, 0, tag_value, 0, 256);
            string str_tag_value = NetUtils.ConvertByteArrayToStringASCII(tag_value).ToUpper();

            var res = ApiClient.Instance.GetTagInfoApiFunc(ConfigSetting.ShopId, session.DeviceSerial, str_tag_value);
            if (res.suc == 1)
            {
                // 존재
                Int32 length = PacketInfo.HeaderSize + 12;
                Int32 opcode = (int)Opcode.RES_GET_TAG_INFO;

                int status = int.Parse(res.dataMap["status"].ToString());

                if (status == 2)
                {
                    string lockedTag = tm.GetTag(requestInfo.token);
                    tm.TryReleaseTag(requestInfo.token, lockedTag);
                    if (!tm.TryUsingTag(requestInfo.token, str_tag_value))
                    {
                        status = 4;
                    }
                }

                int remainigAmount = int.Parse(res.dataMap["remaining_amount"].ToString());
                int remain_beer = int.Parse(res.dataMap["remain_beer"].ToString());

                Console.WriteLine("tag : " + str_tag_value);
                Console.WriteLine("status : " + status);
                Console.WriteLine("remaining_amount : " + remainigAmount);
                Console.WriteLine("remain_beer : " + remain_beer);

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(status), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(remainigAmount), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(remain_beer), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;

                session.Send(packet, 0, packet.Length);

                Console.WriteLine("[REQ_GET_TAG_INFO] : " + str_tag_value + " , can use!");
            }
            else
            {
                // 없음
                /*
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_UNKNOWN;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);
                //*/

                Int32 length = PacketInfo.HeaderSize + 8;
                Int32 opcode = (int)Opcode.RES_GET_TAG_INFO;

                int status = 0;//not registed
                int remainigAmount = 0;
                int remain_beer = 0;

                Console.WriteLine("tag : " + str_tag_value);
                Console.WriteLine("status : " + status);
                Console.WriteLine("remaining_amount : " + remainigAmount);
                Console.WriteLine("remain_beer : " + remain_beer);

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                Array.Copy(NetUtils.GetBytes(status), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(remainigAmount), 0, packet, 28, 4);
                Array.Copy(NetUtils.GetBytes(remain_beer), 0, packet, 32, 4);

                session.Send(packet, 0, packet.Length);

                Console.WriteLine("[REQ_GET_TAG_INFO] : " + str_tag_value + " , can not use!");
            }
        }

        public void REQ_GET_PRODUCT_INFO(NetworkSession session, PacketInfo requestInfo)
        {
            if (!session.IsLogin)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_NOT_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }

            var res = ApiClient.Instance.GetTapProductInfoFunc(ConfigSetting.ShopId, session.DeviceSerial);

            //장비 데이터 있음
            if (res.suc == 1)
            {
                //상품데이터 존재함
                Int32 length = PacketInfo.HeaderSize + 276;//상품 설명, 이미지 데이터 없음.
                Int32 opcode = (int)Opcode.RES_GET_PRODUCT_INFO;

                int id = int.Parse(res.dataMap["id"].ToString());
                byte[] name = NetUtils.ConvertStringToByteArrayASCII(res.dataMap["name"].ToString());
                int unitPrice = int.Parse(res.dataMap["unit_price"].ToString());
                int descriptionSize = 0;
                int imageSize = 0;
                byte[] imageType = NetUtils.ConvertStringToByteArrayASCII("bmp");

                Console.WriteLine("product server_id : " + id);
                Console.WriteLine("product name : " + res.dataMap["name"].ToString());
                Console.WriteLine("product unitPrice : " + unitPrice);

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(id), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(name, 0, packet, PacketInfo.HeaderSize + pos, name.Length); pos += 256;
                Array.Copy(NetUtils.GetBytes(unitPrice), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(descriptionSize), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(imageSize), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(imageType, 0, packet, PacketInfo.HeaderSize + pos, imageType.Length); pos += 4;

                session.Send(packet, 0, packet.Length);
            }
            else
            {
                // 장비 데이터 없음
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;

                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_UNKNOWN;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

            }
        }
        public void RES_GET_CHANGE_PRODUCT_INFO(NetworkSession session, PacketInfo requestInfo)
        {
            Console.WriteLine("RES_GET_CHANGE_PRODUCT_INFO Received!");
        }
        public void REQ_GET_SHOP_INFO(NetworkSession session, PacketInfo requestInfo)
        {
            if (!session.IsLogin)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_NOT_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }

            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.RES_GET_SHOP_INFO;


                int id = ConfigSetting.ShopId;
                byte[] name = NetUtils.ConvertStringToByteArrayUTF8(ConfigSetting.ShopName);
                int status = ConfigSetting.IsOpen ? 1 : 0;

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(id), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(name, 0, packet, PacketInfo.HeaderSize + pos, name.Length); pos += 256;
                Array.Copy(NetUtils.GetBytes(status), 0, packet, 276, 4); pos += 4;

                session.Send(packet, 0, packet.Length);
            }
        }

        public void RES_SET_SHOP_INFO(NetworkSession session, PacketInfo requestInfo)
        {
            //   

        }

        public void RES_GET_DEVICE_STATUS(NetworkSession session, PacketInfo requestInfo)
        {
            // uint32 IS_KEG_RESET KEG 리셋 여부(0: FALSE, 1: TRUE)
            // uint32 IS_VALVE_ERROR VALVE 에러 여부(0: FALSE, 1: TRUE)
            // uint32 IS_FLOWMETER_ERROR 유량센서 에러 여부(0: FALSE, 1: TRUE)
            // uint32 STATUS 0:정상, 1:점검, 2:세척, 3:안정화

            int is_keg_reset = NetUtils.ToInt32(requestInfo.Body, 0);
            int is_valve_error = NetUtils.ToInt32(requestInfo.Body, 4);
            int is_flowmeter_error = NetUtils.ToInt32(requestInfo.Body, 8);
            int status = NetUtils.ToInt32(requestInfo.Body, 12);
            int is_valve_on = NetUtils.ToInt32(requestInfo.Body, 16);

        }
        public void REQ_SET_DEVICE_STATUS(NetworkSession session, PacketInfo requestInfo)
        {
            if (!session.IsLogin)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_NOT_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
            // uint32 IS_KEG_RESET KEG 리셋 여부(0: FALSE, 1: TRUE)
            // uint32 IS_VALVE_ERROR VALVE 에러 여부(0: FALSE, 1: TRUE)
            // uint32 IS_FLOWMETER_ERROR 유량센서 에러 여부(0: FALSE, 1: TRUE)
            // uint32 STATUS 0:정상, 1:점검, 2:세척, 3:안정화

            int is_keg_reset = NetUtils.ToInt32(requestInfo.Body, 0);
            int is_valve_error = NetUtils.ToInt32(requestInfo.Body, 4);
            int is_soldout = NetUtils.ToInt32(requestInfo.Body, 8);
            int status = NetUtils.ToInt32(requestInfo.Body, 12);
            int is_valve_on = NetUtils.ToInt32(requestInfo.Body, 16);

            Console.WriteLine("is_keg_reset : " + is_keg_reset);
            Console.WriteLine("is_valve_error : " + is_valve_error);
            Console.WriteLine("is_soldout : " + is_soldout);
            Console.WriteLine("status : " + status);
            Console.WriteLine("is_valve_on : " + is_valve_on);


            var res = ApiClient.Instance.SetDeviceInfoFunc(ConfigSetting.ShopId, session.DeviceSerial, is_keg_reset, is_valve_error, is_soldout, status, is_valve_on);
            if(res.suc == 1)
            {
                Int32 length = PacketInfo.HeaderSize;
                Int32 opcode = (int)Opcode.RES_SET_DEVICE_STATUS;


                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                session.Send(packet, 0, packet.Length);
            }
            else
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_UNKNOWN;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);
            }
        }

        public void REQ_SET_FLOWMETER_VALUE(NetworkSession session, PacketInfo requestInfo)
        {
            if (!session.IsLogin)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_NOT_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
            // uint32 VALUE 유량센서 값

            UInt32 value = NetUtils.ToUInt32(requestInfo.Body, 0);

            {
                Int32 length = PacketInfo.HeaderSize;
                Int32 opcode = (int)Opcode.RES_SET_FLOWMETER_VALUE;


                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                session.Send(packet, 0, packet.Length);
            }
        }
        public void REQ_SET_PRODUCT_CONSUMPTION(NetworkSession session, PacketInfo requestInfo)
        {
            if (!session.IsLogin)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;

                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_NOT_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }

            if (!ConfigSetting.IsOpen)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;

                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_SERVER_INTERNAL;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
            // uint32 PRODUCT_ID 상품 KEY값
            // char[256] NFC_VALUE NFC 값
            // uint32 CONSUMPTION 소모량

            int last_order_number = NetUtils.ToInt32(requestInfo.Body, 0);
            int product_id = NetUtils.ToInt32(requestInfo.Body, 4);

            byte[] tag_value = new byte[256];
            Array.Copy(requestInfo.Body, 8, tag_value, 0, 256);
            string str_tag_value = NetUtils.ConvertByteArrayToStringASCII(tag_value).ToUpper();

            int consumption = NetUtils.ToInt32(requestInfo.Body, 264);
            bool is_soldout = requestInfo.Body[268] == 0x01;

            Console.WriteLine("[TCP][RECV][REQ_SET_PRODUCT_CONSUMPTION] : " + product_id + " , " + str_tag_value + " , " + consumption + " ml , " + is_soldout);

            if (tm.TryReleaseTag(session.Token, str_tag_value))
            {
                Console.WriteLine("Tag(" + str_tag_value + ") 사용해제 성공");
            }
            else
            {
                Console.WriteLine("Tag(" + str_tag_value + ") 사용해제 실패");
            }

            //유량센서미작동 판단
            if (consumption == 0)
            {
                int flowCnt = tm.GetFlowCnt(session.Token);
                flowCnt++;
                if (flowCnt == 3)
                {
                    Console.WriteLine("유량센서 미작동!!!");
                    ApiClient.Instance.SensorNotWorkFunc(ConfigSetting.ShopId, session.DeviceSerial, str_tag_value);
                    tm.SetFlowCnt(session.Token, 0);
                }
                else
                {
                    tm.SetFlowCnt(session.Token, flowCnt);
                }
            }
            else
            {
                tm.SetFlowCnt(session.Token, 0);
            }

            var res = ApiClient.Instance.GetOrderNumberFunc(ConfigSetting.ShopId, session.DeviceSerial, str_tag_value);
            if (res.suc == 1)
            {
                int remain_amount = int.Parse(res.dataMap["remaining_amount"].ToString());
                int pre_last_order_number = int.Parse(res.dataMap["last_order_number"].ToString());

                ApiClient.Instance.SetOrderNumberFunc(ConfigSetting.ShopId, session.DeviceSerial, last_order_number);
                if (consumption > 0)
                {
                    var selfOrderRes = ApiClient.Instance.CreateOrderFunc(ConfigSetting.ShopId, session.DeviceSerial, last_order_number, product_id, str_tag_value, consumption, is_soldout ? 1 : 0);
                    if (selfOrderRes.suc == 1)
                    {
                        int remain = int.Parse(selfOrderRes.dataMap["remaining_amount"].ToString());

                        Console.WriteLine("Remaining_amount : " + remain);
                        // 성공 응답
                        Int32 length = PacketInfo.HeaderSize + 4;
                        Int32 opcode = (int)Opcode.RES_SET_PRODUCT_CONSUMPTION;

                        byte[] packet = new byte[length];
                        Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                        Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                        Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                        Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                        Array.Copy(NetUtils.GetBytes(remain), 0, packet, 24, 4);

                        Console.WriteLine("[TCP][SEND] RES_SET_PRODUCT_CONSUMPTION");
                        session.Send(packet, 0, packet.Length);
                    }
                    else
                    {
                        Int32 length = PacketInfo.HeaderSize + 264;
                        Int32 opcode = (int)Opcode.ERROR_MESSAGE;

                        Int32 errorOpcode = requestInfo.opcode;
                        Int32 errorCode = (int)Errorcode.ERR_UNKNOWN;
                        byte[] message = new byte[256];

                        byte[] packet = new byte[length];
                        Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                        Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                        Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                        Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                        Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                        Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                        Array.Copy(message, 0, packet, 32, 256);

                        session.Send(packet, 0, packet.Length);
                    }
                }
                else
                {
                    // 성공 응답
                    Int32 length = PacketInfo.HeaderSize + 4;
                    Int32 opcode = (int)Opcode.RES_SET_PRODUCT_CONSUMPTION;

                    Console.WriteLine("Remaining_amount : " + remain_amount);
                    Console.WriteLine("[TCP][SEND] RES_SET_PRODUCT_CONSUMPTION");

                    byte[] packet = new byte[length];
                    Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                    Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                    Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                    Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                    Array.Copy(NetUtils.GetBytes(remain_amount), 0, packet, 24, 4);

                    session.Send(packet, 0, packet.Length);

                    Console.WriteLine("[TCP][SEND] RES_SET_PRODUCT_CONSUMPTION last order number duplicated!");
                }
            }
            else
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_UNKNOWN;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);
            }
        }
        public void REQ_GET_PRODUCT_QAUNTITY(NetworkSession session, PacketInfo requestInfo)
        {
            if (!session.IsLogin)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_NOT_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }

            var res = ApiClient.Instance.GetProductQuantityFunc(ConfigSetting.ShopId, session.DeviceSerial);
            if (res.suc == 1)
            {
                Int32 length = PacketInfo.HeaderSize + 8;
                Int32 opcode = (int)Opcode.RES_GET_PRODUCT_QAUNTITY;

                int total = int.Parse(res.dataMap["keg_size"].ToString());
                int remain = int.Parse(res.dataMap["remain_amount"].ToString());

                Console.WriteLine("total : " + total);
                Console.WriteLine("remain : " + remain);

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                Array.Copy(NetUtils.GetBytes(total), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(remain), 0, packet, 28, 4);

                session.Send(packet, 0, packet.Length);
            }
            else
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_INVALID_SERIAL;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                session.Close();

                return;
            }
        }

        public void REQ_SET_MILLILITER_PER_PULSE(NetworkSession session, PacketInfo requestInfo)
        {
            if (!session.IsLogin)
            {
                Int32 length = PacketInfo.HeaderSize + 264;
                Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                Int32 errorOpcode = requestInfo.opcode;
                Int32 errorCode = (int)Errorcode.ERR_NOT_AUTH;
                byte[] message = new byte[256];

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                int pos = 0;
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, PacketInfo.HeaderSize + pos, 4); pos += 4;
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
            int pulse = NetUtils.ToInt32(requestInfo.Body, 0);
            Console.WriteLine("[TCP][RECV][REQ_SET_MILLILITER_PER_PULSE] : " + pulse);

            {
                //float milliliter = (float)NetUtils.ToInt32(requestInfo.Body, 0) / 1000.0f;

                Int32 length = PacketInfo.HeaderSize;
                Int32 opcode = (int)Opcode.RES_SET_MILLILITER_PER_PULSE;

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                session.Send(packet, 0, packet.Length);
            }
        }
        public void ERROR_MESSAGE(NetworkSession session, PacketInfo requestInfo)
        {
            // uint32 OPCODE 에러가 발생한 요청의 OPCODE
            // uint32 ERROR_CODE 에러 코드
            // char[256] ERROR_MESSAGE 에러 메시지

            UInt32 opcode = NetUtils.ToUInt32(requestInfo.Body, 0);
            UInt32 error_code = NetUtils.ToUInt32(requestInfo.Body, 4);

            byte[] error_message = new byte[256];
            Array.Copy(requestInfo.Body, 8, error_message, 0, 256);
            string str_error_message = NetUtils.ConvertByteArrayToStringASCII(error_message);

            Console.WriteLine("[ERROR_MESSAGE] " + opcode.ToString() + " : " + error_code.ToString());

            //session.Close();//disconnect
        }
    }
    #endregion
}
