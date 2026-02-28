using System;
using Google.Protobuf;
using Network.Core;
using UnityEngine;

namespace Network
{
    public class GateClient: GameClient_Cmd
    {
        private void Event_Connect(string info)
        {
            Debug.Log($"connect success: {info}");
        }

        private void Event_Secure(string info)
        {
            // 通过安全认证，发送登录请求
            Debug.Log($"connect secure: {info}");
            // PlayerManager.Instance.Send_EnterScene_Req();
        }

        private void Event_DisConnect(string info)
        {
            Debug.Log($"disconnect: {info}");
        }

        private void Event_Command(IMessage msg)
        {
            if (OnProtobufMessage(msg)) 
                return;
            
            Debug.Log($"无法识别的指令，关闭连接: {msg.Descriptor.FullName}");
            DisconnectServer("DisconnectServer");
        }
            
        

        public void ConnectToServer(string serverIP, int tcpPort, TNOTIFY_EVENT on_secure_event = null)
        {
            Debug.Log($"server server_ip: {serverIP}");
            F_Notify_Command = Event_Command;
            F_Notify_Connect = Event_Connect; 
            F_Notify_Secure = on_secure_event ?? Event_Secure;
            F_Notify_Disconnect = Event_DisConnect;

            StartTcp(serverIP, tcpPort);
        }
    }
}