using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
using Yy.Protocol.App;

// 提供 Func<T> 等委托类型

namespace Network.Core
{
    public class ProtobufDispatcher_Name
    {
        public delegate void OnProtobufCallback(IMessage message);

        private readonly Dictionary<string, OnProtobufCallback> _callbacks = new();
        private readonly Dictionary<string, Func<IMessage>> _factories = new();

        public void RegisterMessageCallback<T>(Action<T> callback) where T : class, IMessage, new()
        {
            // 注册消息回调
            string name = new T().Descriptor.FullName;
            _callbacks[name] = MsgWrapper;
            _factories[name] = () => new T();
            Debug.Log($"注册消息回调: {name}");
            return;

            // 这里包装成OnProtobufCallback类型
            void MsgWrapper(IMessage msg)
            {
                if (msg is T tMsg)
                {
                    callback(tMsg);
                }
                else
                {
                    Debug.LogError($"消息类型不匹配，期望 {typeof(T)}，收到 {msg.GetType()}");
                }
            }
        }
        

        public IMessage TryCreateMessage(string fullName)
        {
            IMessage msg = _factories.TryGetValue(fullName, out Func<IMessage> factory) ? factory() : null;
            return msg;
        }

        public bool OnProtobufMessage(IMessage message)
        {
            string fullName = message.Descriptor.FullName;
            if (_callbacks.TryGetValue(fullName, out OnProtobufCallback callback))
            {
                callback(message);
                return true;
            }

            Debug.LogWarning($"未找到回调: {fullName}");
            return false;
        }
    }

}