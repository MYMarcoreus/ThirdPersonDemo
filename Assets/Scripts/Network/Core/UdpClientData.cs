using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Network.Core
{
    public class UdpClientData
    {
        public UdpClient sock = new();

        public IPEndPoint server_udp_ep;

        //动态变化字段
        public byte xor_code;

        public string md5;
        
        public void Init(IPEndPoint serverAddr)
        {
            server_udp_ep = serverAddr;
            Reset();
        }

        public void Reset()
        {
            xor_code = ConfigXML.xor_code;
            
            if (sock != null)
            {
                sock.Close();
                sock = new UdpClient();
            }
        }

        public void Send(ReadOnlySpan<byte> data)
        {
            try
            {
                // Debug.Log($"准备发送 {data.Length} 字节到 [{server_udp_ep.Address}:{server_udp_ep.Port}]");
                sock.BeginSend(data.ToArray(), data.Length, ar =>
                {
                    int sendlen = (int)ar.AsyncState;
                    try {
                        int actualSent = sock.EndSend(ar);
                        // Debug.Log($"成功发送 {actualSent} 字节至 [{ServerUdpEP.Address}:{ServerUdpEP.Port}] (期望发送: {sendlen})");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EndSend 异常: {ex.Message}");
                        sock?.Close();
                    }
                }, data.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"BeginSend 异常: {ex.Message}\n{ex.StackTrace}");
                sock?.Close();
            }
        }
    }
}