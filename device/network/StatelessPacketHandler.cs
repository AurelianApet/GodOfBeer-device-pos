using GodOfBeer.util;
using System;
using GodOfBeer.restful;

namespace GodOfBeer.network
{
    #region StatelessHandler
    public class StatelessHandler
    {
        TokenManager tm = TokenManager.Instance;
        public void REQ_PING(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
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

                Array.Copy(NetUtils.GetBytes(NetUtils.ConvertDateTimeToNetDate(now)), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(NetUtils.ConvertDateTimeToNetTime(now)), 0, packet, 28, 4);

                session.Send(packet, 0, packet.Length);
            }
        }
        public void REQ_GET_CHANGED_OPCODES(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
            {
                int opcode = (int)Opcode.RES_GET_CHANGED_OPCODES;
                int length = PacketInfo.HeaderSize + 44;

                var reservedoOcodes = tm.PopReservedOpcodes(requestInfo.token);
                int opcodeCount = reservedoOcodes.Count;
                byte[] opcodesBytes = new byte[4 * opcodeCount];

                //Console.WriteLine("COUNT : " + opcodeCount);
                for (int i = 0; i < opcodeCount; i++)
                {
                    //Console.WriteLine("OPCODE : " + ((Opcode)reservedoOcodes[i]).ToString());
                    Array.Copy(NetUtils.GetBytes((int)reservedoOcodes[i]), 0, opcodesBytes, 4 * i, 4);
                }

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                Array.Copy(NetUtils.GetBytes(opcodeCount), 0, packet, 24, 4);
                Array.Copy(opcodesBytes, 0, packet, 28, opcodesBytes.Length);

                session.Send(packet, 0, packet.Length);
            }
        }
        public void REQ_AUTH_DEVICE(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();

            // char[128] DEVICE_SERIAL 장비 시리얼 문자열
            // char[128] AUTH_KEY 인증 KEY
            byte[] device_serial = new byte[128];
            Array.Copy(requestInfo.Body, 0, device_serial, 0, 128);
            byte[] auth_key = new byte[128];
            Array.Copy(requestInfo.Body, 128, auth_key, 0, 128);

            string str_device_serial = NetUtils.ConvertByteArrayToStringASCII(device_serial);
            string str_auth_key = NetUtils.ConvertByteArrayToStringASCII(auth_key);

            Console.WriteLine("인증요청 : Serial(" + str_device_serial + ")");

            // 인증 처리
            var res = ApiClient.Instance.FindTapApiFunc(ConfigSetting.ShopId, str_device_serial);
            if (res.suc == 1)
            {
                // 성공
                long token = tm.CreateNewToken(str_device_serial);

                Console.WriteLine("인증 : 성공(ToKen => " + token.ToString() + ")");
                Console.WriteLine("인증 : 성공(ToKen => " + token.ToString("X") + ")");
                Int32 length = PacketInfo.HeaderSize + 8;
                Int32 opcode = (int)Opcode.RES_AUTH_DEVICE;


                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(token), 0, packet, 16, 8);

                Array.Copy(NetUtils.GetBytes(token), 0, packet, 24, 8);

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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);
            }
        }
        public void REQ_GET_TAG_INFO(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                Console.WriteLine("[REQ_GET_TAG_INFO] : Token Invalid => (" + requestInfo.token + ")");

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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                Console.WriteLine("[REQ_GET_TAG_INFO] : Shop Closed");

                return;
            }

            // char[256] NFC_VALUE 확인 대상 NFC 값 문자열
            byte[] tag_value = new byte[256];
            Array.Copy(requestInfo.Body, 0, tag_value, 0, 256);
            string str_tag_value = NetUtils.ConvertByteArrayToStringASCII(tag_value).ToUpper();
            string serial = tm.GetSerial(requestInfo.token);

            var res = ApiClient.Instance.GetTagInfoApiFunc(ConfigSetting.ShopId, serial, str_tag_value);
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

                Array.Copy(NetUtils.GetBytes(status), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(remainigAmount), 0, packet, 28, 4);
                Array.Copy(NetUtils.GetBytes(remain_beer), 0, packet, 32, 4);

                session.Send(packet, 0, packet.Length);
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);
                //*/

                Int32 length = PacketInfo.HeaderSize + 12;
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

        public void REQ_GET_PRODUCT_INFO(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
            string serial = tm.GetSerial(requestInfo.token);

            var res = ApiClient.Instance.GetTapProductInfoFunc(ConfigSetting.ShopId, serial);

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

                Array.Copy(NetUtils.GetBytes(id), 0, packet, 24, 4);
                Array.Copy(name, 0, packet, 28, name.Length);//256
                Array.Copy(NetUtils.GetBytes(unitPrice), 0, packet, 284, 4);
                Array.Copy(NetUtils.GetBytes(descriptionSize), 0, packet, 288, 4);
                Array.Copy(NetUtils.GetBytes(imageSize), 0, packet, 292, 4);
                Array.Copy(imageType, 0, packet, 296, imageType.Length);

                session.Send(packet, 0, packet.Length);
            }
            else
            {
                // 상품 연결데이터 없음
                //Int32 length = PacketInfo.HeaderSize + 264;
                //Int32 opcode = (int)Opcode.ERROR_MESSAGE;


                //Int32 errorOpcode = requestInfo.opcode;
                //Int32 errorCode = (int)Errorcode.ERR_UNKNOWN;
                //byte[] message = new byte[256];

                //byte[] packet = new byte[length];
                //Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                //Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                //Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                //Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);
                //Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                //Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                //Array.Copy(message, 0, packet, 32, 256);

                //session.Send(packet, 0, packet.Length);

                Int32 length = PacketInfo.HeaderSize + 276;//상품 설명, 이미지 데이터 없음.
                Int32 opcode = (int)Opcode.RES_GET_PRODUCT_INFO;


                int id = (int)0;
                byte[] name = NetUtils.ConvertStringToByteArrayASCII("");
                int unitPrice = (int)0;
                int descriptionSize = 0;
                int imageSize = 0;
                byte[] imageType = NetUtils.ConvertStringToByteArrayASCII("bmp");

                Console.WriteLine("product server_id : " + 0);
                Console.WriteLine("product name : " + "");
                Console.WriteLine("product unitPrice : " + 0);

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                Array.Copy(NetUtils.GetBytes(id), 0, packet, 24, 4);
                Array.Copy(name, 0, packet, 28, name.Length);//256
                Array.Copy(NetUtils.GetBytes(unitPrice), 0, packet, 284, 4);
                Array.Copy(NetUtils.GetBytes(descriptionSize), 0, packet, 288, 4);
                Array.Copy(NetUtils.GetBytes(imageSize), 0, packet, 292, 4);
                Array.Copy(imageType, 0, packet, 296, imageType.Length);

                session.Send(packet, 0, packet.Length);
            }
        }
        
        public void REQ_GET_SHOP_INFO(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
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

                Console.WriteLine("shop id : " + id);
                Console.WriteLine("shop name : " + ConfigSetting.ShopName);
                Console.WriteLine("shop status : " + status);

                byte[] packet = new byte[length];
                Array.Copy(NetUtils.GetBytes(length), 0, packet, 0, 4);
                Array.Copy(NetUtils.GetBytes(opcode), 0, packet, 4, 4);
                Array.Copy(NetUtils.GetBytes(requestInfo.reqid), 0, packet, 8, 8);
                Array.Copy(NetUtils.GetBytes(requestInfo.token), 0, packet, 16, 8);

                Array.Copy(NetUtils.GetBytes(id), 0, packet, 24, 4);
                Array.Copy(name, 0, packet, 28, name.Length);//256
                Array.Copy(NetUtils.GetBytes(status), 0, packet, 284, 4);

                session.Send(packet, 0, packet.Length);
            }
        }

        public void REQ_SET_DEVICE_STATUS(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            try
            {
                tm.UpdateExpiredToken();
                if (!tm.IsValidateToken(requestInfo.token))
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
                    Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                    Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
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

                string serial = tm.GetSerial(requestInfo.token);

                var res = ApiClient.Instance.SetDeviceInfoFunc(ConfigSetting.ShopId, serial, is_keg_reset, is_valve_error, is_soldout, status, is_valve_on);

                if(res.suc == 1)
                {
                    Console.WriteLine("SET_DEVICE_STATUS Success!");
                }
                else
                {
                    Console.WriteLine("SET_DEVICE_STATUS Fail!");
                }

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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        public void REQ_SET_FLOWMETER_VALUE(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
            // uint32 VALUE 유량센서 값

            UInt32 value = NetUtils.ToUInt32(requestInfo.Body, 0);
            Console.WriteLine("Flowmeter Value : " + value);
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
        public void REQ_SET_PRODUCT_CONSUMPTION(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                Console.WriteLine("[ERROR][REQ_SET_PRODUCT_CONSUMPTION] : Shop Closed");

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

            string serial = tm.GetSerial(requestInfo.token);

            Console.WriteLine("[TCP][RECV][REQ_SET_PRODUCT_CONSUMPTION] : " + last_order_number + " , " + product_id + " , " + str_tag_value + " , " + consumption + " ml , " + is_soldout);

            string lockTag = tm.GetTag(requestInfo.token);
            Console.WriteLine("tag matching : " + lockTag.Equals(str_tag_value));
            if (tm.TryReleaseTag(requestInfo.token, lockTag))
            {
                Console.WriteLine("Tag(" + lockTag + ") 사용해제 성공");
            }
            else
            {
                Console.WriteLine("Tag(" + lockTag + ") 사용해제 실패");
            }

            //유량센서미작동 판단
            if(consumption == 0)
            {
                int flowCnt = tm.GetFlowCnt(requestInfo.token);
                flowCnt++;
                if(flowCnt == 3)
                {
                    Console.WriteLine("유량센서 미작동!!!");
                    ApiClient.Instance.SensorNotWorkFunc(ConfigSetting.ShopId, serial, str_tag_value);
                    tm.SetFlowCnt(requestInfo.token, 0);
                }
                else
                {
                    tm.SetFlowCnt(requestInfo.token, flowCnt);
                }
            }
            else
            {
                tm.SetFlowCnt(requestInfo.token, 0);
            }

            var res = ApiClient.Instance.GetOrderNumberFunc(ConfigSetting.ShopId, serial, str_tag_value);
            if (res.suc == 1)
            {
                int remain_amount = int.Parse(res.dataMap["remaining_amount"].ToString());
                int pre_last_order_number = int.Parse(res.dataMap["last_order_number"].ToString());

                ApiClient.Instance.SetOrderNumberFunc(ConfigSetting.ShopId, serial, last_order_number);
                if (consumption > 0)
                {
                    var selfOrderRes = ApiClient.Instance.CreateOrderFunc(ConfigSetting.ShopId, serial, last_order_number, product_id, str_tag_value, consumption, is_soldout ? 1 : 0);
                    if(selfOrderRes.suc == 1)
                    {
                        int remain = int.Parse(selfOrderRes.dataMap["remaining_amount"].ToString());
                        // 성공 응답
                        Int32 length = PacketInfo.HeaderSize + 4;
                        Int32 opcode = (int)Opcode.RES_SET_PRODUCT_CONSUMPTION;

                        Console.WriteLine("Remaining_amount : " + remain);

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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);
            }
        }
        public void REQ_GET_PRODUCT_QAUNTITY(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }
            string serial = tm.GetSerial(requestInfo.token);
            var res = ApiClient.Instance.GetProductQuantityFunc(ConfigSetting.ShopId, serial);
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                session.Close();

                return;
            }
        }
        public void REQ_SET_MILLILITER_PER_PULSE(StatelessNetworkSession session, PacketInfo requestInfo)
        {
            tm.UpdateExpiredToken();
            if (!tm.IsValidateToken(requestInfo.token))
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
                Array.Copy(NetUtils.GetBytes(errorOpcode), 0, packet, 24, 4);
                Array.Copy(NetUtils.GetBytes(errorCode), 0, packet, 28, 4);
                Array.Copy(message, 0, packet, 32, 256);

                session.Send(packet, 0, packet.Length);

                return;
            }

            int pulse = NetUtils.ToInt32(requestInfo.Body, 0);
            Console.WriteLine("[TCP][RECV][REQ_SET_MILLILITER_PER_PULSE] : " + pulse);

            {
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
        public void ERROR_MESSAGE(StatelessNetworkSession session, PacketInfo requestInfo)
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
        }
    }
    #endregion
}
