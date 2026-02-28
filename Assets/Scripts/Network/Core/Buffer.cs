using System;
using System.Net.Sockets;
using Google.Protobuf;

namespace Network.Core
{
    public class Buffer
    {
        public byte[] buf; // 缓冲区
        // public byte[] tempbuf; // 临时缓冲区
        public int head; // 偏移头
        public int tail; // 偏移尾

        public Buffer(uint maxBufSize)
        {
            buf = new byte[maxBufSize];
        }

        public int DataSize => tail - head;
        public int RemainedSize => Maxsize - tail;
        public int Maxsize => buf.Length;
        
        public bool AppendDataFromArray(ReadOnlySpan<byte> data)
        {
            bool isEnsured = TryMakeEnoughSpace(data.Length);
            System.Buffer.BlockCopy(data.ToArray(), 0, buf, tail, data.Length);  // 拷贝到缓冲区
            tail += data.Length;
            return isEnsured;
        }

        public bool SendToSocket(TcpClient sock)
        {
            if(DataSize <= 0) return true;

            try
            {
                byte[] tempBuf = new byte[DataSize];
                NetworkStream ns = sock.GetStream();
                ns.BeginWrite(tempBuf, 0, DataSize, ar =>
                {
                    int size = (int)ar.AsyncState;
                    head += size;
                    if(head == tail) 
                        head = tail = 0;
                }, DataSize);
                
                return true;
            }
            catch
            {
                return false;
            }

        }
        
        
        public bool PopDataToProtobuf(ref IMessage outMsg, int bodyLen)
        {
            if (bodyLen > DataSize) {
                return false;
            }

            if (outMsg != null)
                outMsg = outMsg.Descriptor.Parser.ParseFrom(buf, head, bodyLen);
            else
                return false;
            
            head += bodyLen;
            if(DataSize == 0)
                head = tail = 0;

            return true;
        }

        public bool PopDataToBytes(out byte[] msg, int msg_len)
        {
            if (msg_len > DataSize) {
                msg = null;
                return false;
            }            
            
            msg = new byte[msg_len];
            Array.Copy(buf, head, msg, 0, msg_len);
            
            head += msg_len;
            if(DataSize == 0)
                head = tail = 0;
            
            return true;
        }
        
        public bool TryMakeEnoughSpace(int needLen)
        {
            Compact();
            if (RemainedSize < needLen)
            {
                // 扩容：分配新缓冲区（当前数据大小 + 需要的大小）
                int newSize = DataSize + needLen;
                byte[] newBuf = new byte[newSize];
                System.Buffer.BlockCopy(buf, head, newBuf, 0, DataSize);
                buf = newBuf;
                tail = DataSize;
                head = 0;
            }

            return true; // 无上限扩容策略
        }
        
        public void Compact() {
            // 将数据区移到缓冲区最前，回收DataBegin()前的空间（如果数据区已在最前，则不移动）
            if(DataSize > 0 && head != 0) {
                System.Buffer.BlockCopy(buf, head, buf, 0, DataSize);
            }
            head = 0;
            tail = head + DataSize;
        }
    };
}