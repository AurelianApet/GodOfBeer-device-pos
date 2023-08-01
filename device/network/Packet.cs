using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SuperSocket.Common;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.Facility.Protocol;

namespace GodOfBeer.network
{
    public enum Errorcode : int
    {
        ERR_VERSION = 0x00000000,

        ERR_UNKNOWN,
        ERR_INVALID_OPCODE,
        ERR_NOT_AUTH,
        ERR_SERVER_INTERNAL,
        ERR_INVALID_SERIAL,
        ERR_ALREADY_AUTH,

        ERR_VERSION_END
    }
    public enum Opcode : int
    {
        OP_VERSION = 0x10000000,

        REQ_SERVER_INFO,
        RES_SERVER_INFO,
        REQ_PING,
        RES_PING,
        REQ_GET_CHANGED_OPCODES,
        RES_GET_CHANGED_OPCODES,
        REQ_AUTH_DEVICE,
        RES_AUTH_DEVICE,
        REQ_GET_TAG_INFO,
        RES_GET_TAG_INFO,
        REQ_GET_PRODUCT_INFO,
        RES_GET_PRODUCT_INFO,
        REQ_CHANGE_PRODUCT_INFO,
        RES_CHANGE_PRODUCT_INFO,
        REQ_GET_SHOP_INFO,
        RES_GET_SHOP_INFO,
        REQ_SET_SHOP_INFO,
        RES_SET_SHOP_INFO,
        REQ_GET_DEVICE_STATUS,
        RES_GET_DEVICE_STATUS,
        REQ_SET_DEVICE_STATUS,
        RES_SET_DEVICE_STATUS,
        REQ_SET_FLOWMETER_VALUE,
        RES_SET_FLOWMETER_VALUE,
        REQ_SET_PRODUCT_CONSUMPTION,
        RES_SET_PRODUCT_CONSUMPTION,
        REQ_GET_PRODUCT_QAUNTITY,
        RES_GET_PRODUCT_QAUNTITY,
        REQ_SET_MILLILITER_PER_PULSE,
        RES_SET_MILLILITER_PER_PULSE,
        ERROR_MESSAGE,

        OP_VERSION_END
    }

    public class PacketInfo : BinaryRequestInfo
    {
        public Int32 length { get; private set; }
        public Int32 opcode { get; private set; }
        public Int64 reqid { get; private set; }
        public Int64 token { get; private set; }

        public PacketInfo(int length, int opcode, long reqid, long token, byte[] body)
            :base(null, body)
        {
            this.length = length;
            this.opcode = opcode;
            this.reqid = reqid;
            this.token = token;
        }

        public static int HeaderSize
        {
            get {
                return 24;
            }
        }
    }

    public class PakcetReceiveFilter : FixedHeaderReceiveFilter<PacketInfo>
    {
        public PakcetReceiveFilter()
            : base(PacketInfo.HeaderSize)
        {

        }

        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            var nBodySize = NetUtils.ToInt32(header, offset) - PacketInfo.HeaderSize;
            //Console.WriteLine("[TCP][RECV] body size : " + nBodySize);
            return nBodySize;
        }

        protected override PacketInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
        {
            return new PacketInfo(
                NetUtils.ToInt32(header.Array, 0),
                NetUtils.ToInt32(header.Array, 4),
                NetUtils.ToInt64(header.Array, 8),
                NetUtils.ToInt64(header.Array, 16),
                bodyBuffer.CloneRange(offset, length)
                );
        }

        //protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
        //{
        //    int size = bufferStream.ReadInt32(true) - HeaderSize;
        //    return size;
        //}

        //public override PacketInfo ResolvePackage(IBufferStream bufferStream)
        //{
        //    BufferStream bs = bufferStream as BufferStream;
        //    bs.Seek(0, System.IO.SeekOrigin.Begin);

        //    int length = bufferStream.ReadInt32(true);
        //    int opcode = bufferStream.ReadInt32(true);
        //    long reqid = bufferStream.ReadInt64(true);
        //    byte[] body = null;
        //    if (length > HeaderSize)
        //    {
        //        body = new byte[length - HeaderSize];
        //        bufferStream.Read(body, 0, length - HeaderSize);
        //    }
        //    return new PacketInfo(length, opcode, reqid, body);
        //}
    }
}
