using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Network.Core
{
    public enum E_ClientSocketState : byte
    {
        eFree       = 0,
        eConnectTry = 1,
        eConnected  = 2,
        eSecure     = 3,
        eLoggedIn   = 4
    };
    
    //客户端-结构体
    public class TcpClientData
    {
        private readonly object _sockLock = new();
        private TcpClient _sock = new();
        public TcpClient sock
        {
            get
            {
                lock (_sockLock)
                {
                    return _sock;
                }
            }
            private set
            {
                lock (_sockLock)
                {
                    _sock = value;
                }
            }
        }

        public IPEndPoint ServerTcpEP;

        private readonly object _stateLock = new();
        private E_ClientSocketState _state;
        public E_ClientSocketState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
            set
            {
                lock (_stateLock)
                {
                    _state = value;
                }
            }
        }
        public byte xor_code;

        public float  last_max_heart_time;
        public float  last_auto_connect_time;
        public string md5;
        

        public void Init(IPEndPoint serverAddr)
        {
            ServerTcpEP = serverAddr;
            last_max_heart_time = 0;
            last_auto_connect_time = 0;
            Reset();
        }

        public void Reset()
        {
            State = E_ClientSocketState.eFree;
            xor_code = ConfigXML.xor_code;

            if (sock == null) 
                return;
            
            SafeClose();
            sock = new TcpClient();
        }

        public bool IsStateFree() => State == E_ClientSocketState.eFree;

        /// @brief 是否已连接
        public bool IsStateConnected() => State != E_ClientSocketState.eFree && State != E_ClientSocketState.eConnectTry;

        /// @brief 是否是安全连接
        public bool IsStateSecure() => State is E_ClientSocketState.eSecure or E_ClientSocketState.eLoggedIn;

        /// @brief 是否已登录
        public bool IsStateLoggedIn() => State == E_ClientSocketState.eLoggedIn;
        
        public void SafeClose()
        {
            try { sock?.GetStream().Close(); }
            catch
            {
                // ignored
            }

            try { sock?.Close(); }
            catch
            {
                // ignored
            }
        }
       
        // 没有sendbuf，直接发送
        public void Send(ReadOnlySpan<byte> data)
        {
            try
            {
                NetworkStream ns = sock.GetStream();
                ns.BeginWrite(data.ToArray(), 0, data.Length, ar =>
                {
                    // int sendlen = (int)ar.AsyncState;
                    try {
                        sock.GetStream().EndWrite(ar);
                    }
                    catch {
                        SafeClose();
                    }
                }, data.Length);
            }
            catch
            {
                SafeClose();
            }
        }
    };
}
