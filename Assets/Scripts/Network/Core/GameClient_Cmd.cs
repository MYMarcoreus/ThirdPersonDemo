using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;
using Yy.Protocol;
using Yy.Protocol.Core;

namespace Network.Core
{
    public delegate void TNOTIFY_COMMAND(IMessage msg);
    public delegate void TNOTIFY_EVENT(string info);


    public class GameClient_Cmd
    {
        public TNOTIFY_EVENT   F_Notify_Connect   {get; set;}
        public TNOTIFY_EVENT   F_Notify_Secure    {get; set;}
        public TNOTIFY_EVENT   F_Notify_Disconnect{get; set;}
        public TNOTIFY_COMMAND F_Notify_Command   {get; set;}

        public readonly TcpClientData tcp_session = new();
        
        public readonly UdpClientData udp_session = new();
        
        private readonly object enable_auto_connect_lock_ = new();
        private bool enable_auto_connect_;
        private bool enable_auto_connect        
        {
            get
            {
                lock (enable_auto_connect_lock_)
                {
                    return enable_auto_connect_;
                }
            }
            set
            {
                lock (enable_auto_connect_lock_)
                {
                    enable_auto_connect_ = value;
                }
            }
        }
        
        public UInt64 SessionID { get; private set; }
        
        public E_ClientSocketState state
        {
            get => tcp_session.State;
            set => tcp_session.State = value;
        }

        public bool IsConnected() => tcp_session.IsStateConnected();

        public bool IsSecure() => tcp_session.IsStateSecure();

        private readonly ProtobufDispatcher_Cmd m_dispatcher = new();
        // private readonly ProtobufIDDispatcher m_dispatcher = new();

        #region 消息队列相关
        ConcurrentQueue<byte[]> m_pendingBytes = new();
        private readonly ConcurrentQueue<IMessage> m_messageQueue = new();
        
        private void EnqueueMessage(IMessage msg)
        {
            if (msg != null)
                m_messageQueue.Enqueue(msg);
        }
        
        private void HandleMessages()
        {
            int count = 100;
            while (count-- > 0 && m_messageQueue.TryDequeue(out IMessage msg))
            {
                // Debug.Log($"处理消息：{msg.Descriptor.FullName}");
                ParseNotify_Command(msg);
            }
        }
        #endregion
        
        private CancellationTokenSource m_ctsTcp = new();
        private CancellationTokenSource m_ctsUDP = new();
        
        public GameClient_Cmd()
        {
            Debug.Log("GameClient初始化");
            m_dispatcher.RegisterMessageCallback<XorBodyRsp>(OnXorCode);
            m_dispatcher.RegisterMessageCallback<HeartBody>(OnHeart);
            m_dispatcher.RegisterMessageCallback<SecurityCheckRsp>(OnSecurityResult);
            m_dispatcher.RegisterMessageCallback<UdpPortRegisterRsp>(OnUdpPortRegisterResponse);
        }

        public void StartTcp(string serverIP,int serverTcpPort)
        {
            if (state == E_ClientSocketState.eFree)
            {
                tcp_session.Init(new IPEndPoint(IPAddress.Parse(serverIP), serverTcpPort));
            }
            enable_auto_connect = true;
            ConnectTcpServer();
        }
        
        public void StartUdp(string serverIP, int serverUdpPort) 
        {
            udp_session.Init(new IPEndPoint(IPAddress.Parse(serverIP), serverUdpPort));
            udp_session.sock.Connect(udp_session.server_udp_ep.Address, udp_session.server_udp_ep.Port);
            
            m_ctsUDP = new CancellationTokenSource();
            Task.Run(() => UdpRecvThread(m_ctsUDP.Token))
                .ContinueWith(t => Debug.LogError(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        public void OnDestroy()
        {
            DisconnectServer("OnDestroy");
        }
         
        //! 循环
        public void Update()
        {
            if (tcp_session.IsStateFree() && enable_auto_connect) {
                AutoConnect();
                return;
            }

            HandleMessages();
            SendHeart();
        }       


        public void RegisterMessageCallback<T>(Action<T> callback)
            where T : class, IMessage, new()
        {
            m_dispatcher.RegisterMessageCallback(callback);
        }

        public bool OnProtobufMessage(IMessage msg)
        {
            return m_dispatcher.OnProtobufMessage(msg);
        }

        //**********************************封包、解包***************************************
        //封包
        public void CreateTcpPackage<T>(T body) where T : IMessage
        {   
            var pkgHead = new MessageHeader_Cmd(body);
            var data = new Buffer(pkgHead.FullLength + 8);
            
            // 写入消息头
            pkgHead.AppendIntoBuffer(data, tcp_session.xor_code);
            // 写入消息体
            data.AppendDataFromArray(body.ToByteArray().AsSpan());
            tcp_session.Send(data.buf.AsSpan(0, data.DataSize));
        }
        
        public void CreateUdpPackage<T>(T body) where T : IMessage
        {
            var pkgHead = new MessageHeader_Cmd(body);
            var data = new Buffer(pkgHead.FullLength + 8);
            
            // 写入消息头
            pkgHead.AppendIntoBuffer(data, udp_session.xor_code);
            data.AppendDataFromArray(body.ToByteArray().AsSpan());

            udp_session.Send(data.buf.AsSpan(0, data.DataSize));
        }


        //********************************************服务器连接*************************************************
        private void AutoConnect()
        {
            // 未启动，服务器地址未知，不能连接服务器
            if (!enable_auto_connect)  
                return;
            float ftime = Time.realtimeSinceStartup - tcp_session.last_auto_connect_time;
            if (ftime < ConfigXML.auto_connect_time) return;

            Debug.Log($"auto connect: {tcp_session.ServerTcpEP}");
            tcp_session.last_auto_connect_time = Time.realtimeSinceStartup;
            ConnectTcpServer();
        }
        
        private void ConnectTcpServer()
        {
            tcp_session.Reset();
            state = E_ClientSocketState.eConnectTry;
            tcp_session.sock.BeginConnect(tcp_session.ServerTcpEP.Address, tcp_session.ServerTcpEP.Port, AsyncConnectServer, tcp_session);
        }
        
        private void AsyncConnectServer(IAsyncResult ar_state)
        {
            TcpClientData obj = (TcpClientData)ar_state.AsyncState;
            try
            {
                obj.sock.EndConnect(ar_state);
                obj.State = E_ClientSocketState.eConnected;
                m_ctsTcp = new CancellationTokenSource();
                Task.Run(() => TcpRecvThread(m_ctsTcp.Token))
                    .ContinueWith(t => Debug.LogError(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                F_Notify_Connect?.Invoke("连接成功！");
            }   
            catch (Exception ex)
            {
                DisconnectServer("DisconnectServer");
                lock (this)
                {
                    obj.State = E_ClientSocketState.eFree;
                }
               
                Debug.LogWarning($"连接失败: {ex.Message}");
            }
        }
        
        private async Task TcpRecvThread(CancellationToken token)
        {
            var recvBuf = new Buffer(ConfigXML.recv_bytes_max);

            while (!token.IsCancellationRequested && tcp_session.IsStateConnected())
            {
                // 如果缓冲区为空，重置指针
                if (recvBuf.DataSize == 0)
                {
                    recvBuf.tail = recvBuf.head = 0;
                }
                try
                {
                    recvBuf.Compact();
                    
                    //! （recvBuf生产者）接收数据到缓冲区中
                    NetworkStream ns = tcp_session.sock.GetStream();
                    int readBytes = await ns.ReadAsync(recvBuf.buf, recvBuf.head, recvBuf.RemainedSize, token);
                    if (readBytes <= 0) {
                        DisconnectServer("EndRead returned 0 or less");
                        return;
                    }
                    if (readBytes > recvBuf.RemainedSize) {
                        DisconnectServer("Buffer overflow risk");
                        return;
                    }
                    recvBuf.tail += readBytes;

                    //! （recvBuf消费者）不断解析数据（可以一直解析，因为在接收线程中）
                    while (recvBuf.DataSize >= MessageHeader_Cmd.kHeaderLen)
                    {
                        (MessageHeader_Cmd header, IMessage msg) = ParseTcp(recvBuf, out MessageParseErrorCode errCode);

                        switch (errCode)
                        {
                            case MessageParseErrorCode.eNoError:
                                if (msg != null) {
                                    //! 将消息放入队列中
                                    EnqueueMessage(msg);
                                }

                                break;
                            case MessageParseErrorCode.eNotReceiveFullLength:
                            case MessageParseErrorCode.eNotReceiveFullHeader:
                                continue;
                            case MessageParseErrorCode.eUnkonwnMessage:
                                bool isOk = recvBuf.PopDataToBytes(out byte[] msg_bytes, header.CalcBodyLen());
                                if (!isOk)
                                {
                                    DisconnectServer("DisconnectServer In eUnkonwnMessage");
                                }
                                else
                                {
                                    // todo 未注册的未知消息该如何处理？或者说，网络消息早于系统初始化如何处理？
                                    // m_pendingBytes.Enqueue(msg_bytes);
                                }
                                break;
                            case MessageParseErrorCode.eInvalidCheckCode:
                            case MessageParseErrorCode.eInvalidFullLength:
                            case MessageParseErrorCode.eParseError:
                            default:
                                DisconnectServer("DisconnectServer");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ReadAsync失败: {ex.Message}");
                    DisconnectServer($"DisconnectServer: {ex.Message}");
                }
            }
            
            Debug.LogWarning($"退出写线程");
        }
        
        private async Task UdpRecvThread(CancellationToken token)
        {
            Buffer recvBuf = new Buffer(ConfigXML.recv_bytes_max);

            while (!token.IsCancellationRequested)
            {
                // 如果缓冲区为空，重置指针
                if (recvBuf.DataSize == 0)
                {
                    recvBuf.tail = recvBuf.head = 0;
                }
                try
                {
                    recvBuf.Compact();
                    
                    //! （recvBuf生产者）接收数据到缓冲区中
                    UdpReceiveResult result;
                    try
                    {
                        result = await udp_session.sock.ReceiveAsync();
                        // 处理 result
                    }
                    catch (ObjectDisposedException)
                    {
                        Debug.Log("Socket has been disposed. Stop receiving.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Receive error: {ex.Message}");
                        break;
                    }
                    
                    byte[] data = result.Buffer;
                    // IPEndPoint remoteEp = result.RemoteEndPoint;
                    recvBuf.AppendDataFromArray(data);
                    
                    int readBytes = data.Length;
                    if (readBytes <= 0) {
                        DisconnectServer("EndRead returned 0 or less");
                        return;
                    }
                    if (readBytes > recvBuf.RemainedSize) {
                        DisconnectServer("Buffer overflow risk");
                        return;
                    }
                    // recvBuf.tail += readBytes;

                    //! （recvBuf消费者）不断解析数据（可以一直解析，因为在接收线程中）
                    while (recvBuf.DataSize >= MessageHeader_Cmd.kHeaderLen)
                    {
                        (MessageHeader_Cmd header, IMessage msg) = ParseUdp(recvBuf, out MessageParseErrorCode errCode);
                        switch (errCode)
                        {
                            case MessageParseErrorCode.eNoError:
                                if (msg != null) {
                                    //! 将消息放入队列中
                                    EnqueueMessage(msg);
                                }

                                break;
                            case MessageParseErrorCode.eNotReceiveFullLength:
                            case MessageParseErrorCode.eNotReceiveFullHeader:
                                continue;
                            case MessageParseErrorCode.eUnkonwnMessage:
                                bool isOk = recvBuf.PopDataToBytes(out byte[] msg_bytes, header.CalcBodyLen());
                                if (!isOk)
                                {
                                    DisconnectServer("DisconnectServer In eUnkonwnMessage");
                                }
                                else
                                {
                                    // todo 未注册的未知消息该如何处理？或者说，网络消息早于系统初始化如何处理？
                                    // m_pendingBytes.Enqueue(msg_bytes);
                                }
                                break;
                            case MessageParseErrorCode.eInvalidCheckCode:
                            case MessageParseErrorCode.eInvalidFullLength:
                            case MessageParseErrorCode.eParseError:
                            default:
                                DisconnectServer($"DisconnectServer: {errCode.ToString()}");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ReadAsync失败: {ex.Message}");
                    DisconnectServer("DisconnectServer");
                }
            }
            
            Debug.LogWarning($"退出写线程");
        }
        
        private (MessageHeader_Cmd, IMessage) ParseTcp(Buffer recvBuf, out MessageParseErrorCode outErrCode)
        {
            //! 解析消息头，获得消息体长和RPC名称
            MessageHeader_Cmd header = default;
            outErrCode = header.RetrieveFromBuffer(recvBuf, tcp_session.xor_code);
            if (outErrCode != MessageParseErrorCode.eNoError) 
                return (header, null);
            
            //! 解析消息体
            IMessage message = m_dispatcher.TryCreateMessage((MessageCommand)header.TypeCmd);
            if (message != null) {
                //! 消费缓冲区
                bool isOk = recvBuf.PopDataToProtobuf(ref message, header.CalcBodyLen());
                if (!isOk) {
                    outErrCode = MessageParseErrorCode.eParseError;
                }
            } else {
                outErrCode = MessageParseErrorCode.eUnkonwnMessage;
            }

            return (header, message);
        }

        private (MessageHeader_Cmd, IMessage) ParseUdp(Buffer recvBuf, out MessageParseErrorCode outErrCode)
        {
            //! 解析消息头，获得消息体长和RPC名称
            MessageHeader_Cmd header = default;
            outErrCode = header.RetrieveFromBuffer(recvBuf, udp_session.xor_code);
            if (outErrCode != MessageParseErrorCode.eNoError) 
                return (header, null);
            
            //! 解析消息体
            IMessage message = m_dispatcher.TryCreateMessage((MessageCommand)header.TypeCmd);
            if (message != null) {
                //! 消费缓冲区
                bool isOk = recvBuf.PopDataToProtobuf(ref message, header.CalcBodyLen());
                if (!isOk) {
                    outErrCode = MessageParseErrorCode.eParseError;
                }
            } else {
                outErrCode = MessageParseErrorCode.eUnkonwnMessage;
            }

            return (header, message);
        }
        
        public void DisconnectServer(string errmsg, bool auto_connect = true)
        {
            enable_auto_connect = auto_connect;
            if (!tcp_session.IsStateConnected()) return;
            Debug.LogWarning($"DisconnectServer: {errmsg}, auto_connect = {enable_auto_connect}");
            lock (this)
            {
                m_ctsTcp.Cancel();
                m_ctsUDP.Cancel();
                
                tcp_session.SafeClose();
                tcp_session.Reset();
                udp_session.Reset();
                F_Notify_Disconnect?.Invoke(errmsg);
            }
        }






        


        

        
        private void ParseNotify_Command(IMessage msg)
        {
            // 处理XorCode、SecurityResult等消息
            bool isOk = m_dispatcher.OnProtobufMessage(msg);
            if (!isOk) {
                // Debug.Log("此消息不是核心层的消息，传递给业务层！");
                F_Notify_Command?.Invoke(msg);
            }
        }



        private void OnXorCode(XorBodyRsp xorBody)
        {
            // 生成md5码
            tcp_session.xor_code = (byte)(xorBody.XorCode ^ tcp_session.xor_code);
            tcp_session.md5 = Utils.Utils.ComputeMD5(ConfigXML.secure_code + "_" + tcp_session.xor_code);
            
            // udp_session.xor_code = (byte)(xorBody.XorCode ^ udp_session.xor_code);
            // udp_session.md5 = Utils.ComputeMD5(ClientConfig.secure_code + "_" + udp_session.xor_code);

            Debug.Log($"收到服务器发来的异或码：{xorBody.XorCode}:{tcp_session.xor_code}");

            SecurityCheckReq secureBody = new()
            {
                AppId = ConfigXML.appid,
                AppVersion = ConfigXML.version,
                AppMd5 = tcp_session.md5
            };
            Debug.Log($"向服务器发送安全验证包，消息体大小为：{secureBody.CalculateSize()}");
            CreateTcpPackage(secureBody);
        }

        private void OnSecurityResult(SecurityCheckRsp result)
        {
            //分析安全验证结果
            if (result.ResultCode != SecurityCheckRsp.Types.ResultCode.ESuccess) {
                Debug.Log("Security error..." + result.ResultCode);
                DisconnectServer("DisconnectServer");
            }
            else {
                state = E_ClientSocketState.eSecure;
                
                SessionID = result.SessionId;
                Debug.Log($"客户端收到服务端的连接唯一标识：{result.SessionId}");
                Debug.Log($"客户端收到服务端的Udp端口：{result.ServerUdpPort}");
                
                // 绑定Udp端口
                StartUdp(tcp_session.ServerTcpEP.Address.ToString(), (int)result.ServerUdpPort);
                
                F_Notify_Secure?.Invoke("收到安全验证结果，安全验证通过！");
                
                var localUdpEp = (IPEndPoint)udp_session.sock.Client.LocalEndPoint;
                UdpPortRegisterReq req = new()
                {
                    SessionId = this.SessionID,
                    ClientUdpIp = localUdpEp.Address.ToString(),
                    ClientUdpPort = (uint)localUdpEp.Port,
                };
                Debug.Log($"向服务器申请注册客户端Udp端口[{localUdpEp}]");
                CreateTcpPackage(req);
            }
        }

        private void OnUdpPortRegisterResponse(UdpPortRegisterRsp response)
        {
            Debug.Log($"UdpPort注册结果: {response.Status}, {response.SessionId}");
        }

        


        //**************************************************收到心跳包*******************************************
        private void OnHeart(HeartBody heartBody)
        {
            if (!tcp_session.IsStateSecure()) return;

            float ftime = Time.realtimeSinceStartup - tcp_session.last_max_heart_time;
            if (ftime < ConfigXML.max_heart_time) return;
            
            Debug.Log($"收到服务器的心跳包");

            // HeartBody heartBody = new();
            // ReadPackage(ref heartBody, bodyLen);
        }

        private void SendHeart()
        {
            if (!tcp_session.IsStateSecure()) return;

            float timeDiff = Time.realtimeSinceStartup - tcp_session.last_max_heart_time;
            if (timeDiff < ConfigXML.max_heart_time) return;

            HeartBody heartBody = new();

            tcp_session.last_max_heart_time = Time.realtimeSinceStartup;
            CreateTcpPackage(heartBody);
            // Debug.Log($"发送Tcp心跳包");
            // CreateUdpPackage(heartBody);
            // Debug.Log($"发送Udp心跳包");
        }
    }



}
