using System;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.InteropServices;
using Google.Protobuf;

namespace Network.Core
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct MessageHeader_Name
    {
        #region 私有属性
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private byte[] m_CheckCode;

        private byte[] m_Typename;
        #endregion

        #region 公有属性
        public const int kMinHeaderLen = 8;

        public string CheckCode    => Utils.Struct.BytesToString(m_CheckCode);
        public uint   FullLength { get; private set; }

        public ushort TypenameLen { get; private set; }

        public string Typename     => Utils.Struct.BytesToString(m_Typename);
        
        public int    HeaderLength => kMinHeaderLen + TypenameLen;
        #endregion

        /// <returns> 消息头长度 </returns>
        public int CalcHeaderLen()  { return kMinHeaderLen + TypenameLen; }

        /// <returns> 消息体长度 </returns>
        public int CalcBodyLen()  { return (int)FullLength - CalcHeaderLen(); }

        public MessageHeader_Name(IMessage message)
        {
            // 设置校验码
            m_CheckCode = ConfigXML.check_code[..2]; // C# 8.0 支持切片语法

            // 获取消息类型名
            string typeName = message?.Descriptor?.FullName;
            TypenameLen = (ushort)(typeName?.Length ?? 0);
            m_Typename = typeName != null ? Utils.Struct.StringToBytes(typeName) : null;

            // 计算总长度：最小头长 + 类型名长度 + Protobuf数据长度
            int payloadSize = message?.CalculateSize() ?? 0;
            FullLength = (uint)(kMinHeaderLen + TypenameLen + payloadSize);
        }




        public void AppendIntoBuffer(Buffer sendBuf, byte xorCode)
        {
            int checkCodeLen = m_CheckCode?.Length ?? 0;
            int headerLen = checkCodeLen + sizeof(uint) + sizeof(ushort) + TypenameLen;

            if (sendBuf.RemainedSize < headerLen)
                throw new InvalidOperationException("Buffer space is not enough to append package header.");

            byte[] arr = sendBuf.buf;
            int newTailPos = sendBuf.tail;

            // 异或写入校验码
            Array.Copy(XorBytes(m_CheckCode, xorCode), 0, arr, newTailPos, m_CheckCode.Length);
            newTailPos += m_CheckCode.Length;

            // 先写入大端字节序，再对每个字节异或
            Span<byte> fullLenBytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(fullLenBytes, FullLength);
            for (int i = 0; i < fullLenBytes.Length; i++) {
                fullLenBytes[i] ^= xorCode;
            }
            Array.Copy(fullLenBytes.ToArray(), 0, arr, newTailPos, 4);
            newTailPos += 4;

            // 先写入大端字节序，再对每个字节异或
            Span<byte> typeLenBytes = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(typeLenBytes, (ushort)TypenameLen);
            for (int i = 0; i < typeLenBytes.Length; i++) {
                typeLenBytes[i] ^= xorCode;
            }
            Array.Copy(typeLenBytes.ToArray(), 0, arr, newTailPos, 2);
            newTailPos += 2;

            // 类型名异或写入
            Array.Copy(XorBytes(m_Typename, xorCode), 0, arr, newTailPos, TypenameLen);
            newTailPos += TypenameLen;

            sendBuf.tail = newTailPos;
        }




        public MessageParseErrorCode RetrieveFromBuffer(Buffer recvBuf, byte xorCode)
        {
            var newHeadPos = recvBuf.head;
            Span<byte> bufferSpan = recvBuf.buf;

            // 处理校验码
            ReadXorCheckCode(bufferSpan.Slice(newHeadPos, 2), xorCode);
            newHeadPos += 2;

            if (m_CheckCode[0] != ConfigXML.check_code[0] || 
                m_CheckCode[1] != ConfigXML.check_code[1])
            {
                return MessageParseErrorCode.eInvalidCheckCode;
            }

            // 读取并解析消息长度
            ReadXorFullLength(bufferSpan.Slice(newHeadPos, sizeof(uint)), xorCode);
            newHeadPos += sizeof(uint);

            // 读取并解析类型名称长度
            ReadXorTypeNameLength(bufferSpan.Slice(newHeadPos, sizeof(ushort)), xorCode);
            newHeadPos += sizeof(ushort);

            // 读取并解析类型名称
            if (TypenameLen > 0)
            {
                ReadXorTypeName(bufferSpan.Slice(newHeadPos, TypenameLen), xorCode);
                newHeadPos += TypenameLen;
            }

            // 检查是否接收完整
            if (recvBuf.DataSize < FullLength)
            {
                return MessageParseErrorCode.eNotReceiveFullLength;
            }

            recvBuf.head = newHeadPos;
            return MessageParseErrorCode.eNoError;
        }




        private static byte[] XorBytes(ReadOnlySpan<byte> data, byte xorCode)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            var result = new byte[data.Length];
            for (var i = 0; i < data.Length; i++)
                result[i] = (byte)(data[i] ^ xorCode);
            return result;
        }


        private void ReadXorCheckCode(ReadOnlySpan<byte> value, byte xorCode)
        {
            m_CheckCode = value.Length == 0 ? null : value.ToArray().Select(b => (byte)(b ^ xorCode)).ToArray();
        }
        private void ReadXorFullLength(ReadOnlySpan<byte> value, byte xorCode)
        {
            Span<byte> tmp = stackalloc byte[4];
            // 异或每个字节
            for (int i = 0; i < 4; i++)
                tmp[i] = (byte)(value[i] ^ xorCode);

            // 从大端字节流读取为主机字节序的整数
            FullLength = BinaryPrimitives.ReadUInt32BigEndian(tmp);
        }

        private void ReadXorTypeNameLength(ReadOnlySpan<byte> value, byte xorCode)
        {
            Span<byte> tmp = stackalloc byte[2];
            for (int i = 0; i < 2; i++)
                tmp[i] = (byte)(value[i] ^ xorCode);

            TypenameLen = BinaryPrimitives.ReadUInt16BigEndian(tmp);
        }

        private void ReadXorTypeName(ReadOnlySpan<byte> value, byte xorCode)
        {
            m_Typename = value.Length == 0 ? null : value.ToArray().Select(b => (byte)(b ^ xorCode)).ToArray();
        }
    }

}



