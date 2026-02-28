using System;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Yy.Protocol;

namespace Network.Core
{
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct MessageHeader_Cmd
    {
        private const int kCheckCodeSize = 2;
        private const int kTypeCmdSize = sizeof(UInt16);
        private const int kBodyLengthSize = sizeof(UInt32);
        public const int kHeaderLen = kCheckCodeSize + kTypeCmdSize + kBodyLengthSize;
        
        #region 首部字段
        /// <summary>
        /// 协议校验码：2B
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] CheckCode;

        /// <summary>
        /// 类型ID：整数标识，2B
        /// </summary>
        public UInt16 TypeCmd { get; private set; }
        
        /// <summary>
        /// 消息总长度：4B
        /// </summary>
        public UInt32  BodyLength { get; private set; }
        #endregion

        public UInt32 FullLength => kHeaderLen + BodyLength;

        /// <returns> 消息体长度 </returns>
        public int CalcBodyLen()  { return (int)BodyLength; }

        public MessageHeader_Cmd(IMessage message)
        {
            // 设置校验码
            CheckCode = ConfigXML.check_code[..kCheckCodeSize]; // C# 8.0 支持切片语法

            // 获取消息类型名
            string typeName = message?.Descriptor?.FullName;
            if(typeName != null && ProtobufDispatcher_Cmd.NameToID.TryGetValue(typeName, out MessageCommand id)) 
            {
                TypeCmd = (UInt16)id;
            }
            else
            {
                TypeCmd = 0;
            }

            // 计算总长度：最小头长 + 类型名长度 + Protobuf数据长度
            int payloadSize = message?.CalculateSize() ?? 0;
            BodyLength = (uint)(payloadSize);
        }




        public void AppendIntoBuffer(Buffer sendBuf, byte xorCode)
        {
            int checkCodeLen = CheckCode?.Length ?? 0;

            if (sendBuf.RemainedSize < kHeaderLen)
                throw new InvalidOperationException("Buffer space is not enough to append package header.");

            byte[] arr = sendBuf.buf;
            int newTailPos = sendBuf.tail;

            // 异或写入校验码
            Array.Copy(XorBytes(CheckCode, xorCode), 0, arr, newTailPos, 2);
            newTailPos += kCheckCodeSize;

            // 先写入大端字节序，再对每个字节异或
            Span<byte> typeLenBytes = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(typeLenBytes, TypeCmd);
            for (int i = 0; i < typeLenBytes.Length; i++) {
                typeLenBytes[i] ^= xorCode;
            }
            Array.Copy(typeLenBytes.ToArray(), 0, arr, newTailPos, 2);
            newTailPos += kTypeCmdSize;
            
            // 先写入大端字节序，再对每个字节异或
            Span<byte> fullLenBytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(fullLenBytes, BodyLength);
            for (int i = 0; i < fullLenBytes.Length; i++) {
                fullLenBytes[i] ^= xorCode;
            }
            Array.Copy(fullLenBytes.ToArray(), 0, arr, newTailPos, 4);
            newTailPos += kBodyLengthSize;

            sendBuf.tail = newTailPos;
        }

        public MessageParseErrorCode RetrieveFromBuffer(Buffer recvBuf, byte xorCode)
        {
            int newHeadPos = recvBuf.head;
            Span<byte> bufferSpan = recvBuf.buf;

            // 处理校验码
            ReadXorCheckCode(bufferSpan.Slice(newHeadPos, 2), xorCode);
            newHeadPos += kCheckCodeSize;
            if (CheckCode[0] != ConfigXML.check_code[0] ||  CheckCode[1] != ConfigXML.check_code[1])
            {
                return MessageParseErrorCode.eInvalidCheckCode;
            }

            // 读取并解析类型名称长度
            ReadXorTypeCmd(bufferSpan.Slice(newHeadPos, sizeof(ushort)), xorCode);
            newHeadPos += kTypeCmdSize;
            
            // 读取并解析消息长度
            ReadXorBodyLength(bufferSpan.Slice(newHeadPos, sizeof(uint)), xorCode);
            newHeadPos += kBodyLengthSize;

            // 检查是否接收完整
            if (recvBuf.DataSize < (BodyLength+kHeaderLen))
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
            CheckCode = value.Length == 0 ? null : value.ToArray().Select(b => (byte)(b ^ xorCode)).ToArray();
        }
        
        private void ReadXorTypeCmd(ReadOnlySpan<byte> value, byte xorCode)
        {
            Span<byte> tmp = stackalloc byte[kTypeCmdSize];
            for (int i = 0; i < kTypeCmdSize; i++)
                tmp[i] = (byte)(value[i] ^ xorCode);
            TypeCmd = BinaryPrimitives.ReadUInt16BigEndian(tmp);
        }
        
        private void ReadXorBodyLength(ReadOnlySpan<byte> value, byte xorCode)
        {
            Span<byte> tmp = stackalloc byte[kBodyLengthSize];
            // 异或每个字节
            for (int i = 0; i < kBodyLengthSize; i++)
                tmp[i] = (byte)(value[i] ^ xorCode);
            // 从大端字节流读取为主机字节序的整数
            BodyLength = BinaryPrimitives.ReadUInt32BigEndian(tmp);
        }

    }

}



