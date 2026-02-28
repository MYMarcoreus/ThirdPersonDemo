using System;
using Google.Protobuf;
using Network.Core;
using UnityEngine;

namespace Network
{
    public class LogicClient: GameClient_Cmd
    {
        private TNOTIFY_EVENT on_secure_event_;

        public void SetSecureEvent(TNOTIFY_EVENT on_secure_event)
        {
            on_secure_event_ =  on_secure_event;
        }
        
        private void Event_Connected(string info)
        {
            Debug.Log($"connect success: {info}");
        }

        private void Event_Secure(string info)
        {
            // 通过安全认证，发送登录请求
            Debug.Log($"connect secure: {info}"); 
            on_secure_event_?.Invoke(info);
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
            
        

        public void ConnectToServer(string serverIP, int tcpPort)
        {
            Debug.Log($"server server_ip: {serverIP}");
            F_Notify_Command = Event_Command;
            F_Notify_Connect = Event_Connected; 
            F_Notify_Secure = Event_Secure;
            F_Notify_Disconnect = Event_DisConnect;

            StartTcp(serverIP, tcpPort);
        }
    }
}