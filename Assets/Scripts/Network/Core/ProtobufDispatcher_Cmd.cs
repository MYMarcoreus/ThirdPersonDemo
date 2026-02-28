using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using UnityEngine;
using Yy.Protocol;
using Yy.Protocol.App;
using Yy.Protocol.Core;
using Yy.Protocol;

// 提供 Func<T> 等委托类型

namespace Network.Core
{
    public class ProtobufDispatcher_Cmd
    {
        public delegate void OnProtobufCallback(IMessage message);

        private readonly Dictionary<MessageCommand, OnProtobufCallback> _callbacks = new();
        private readonly Dictionary<MessageCommand, Func<IMessage>> _factories = new();

        public static Dictionary<MessageCommand, Func<IMessage>> IDToCreator { get; } = new()
        {
            // 连接协议相关
            { MessageCommand.MsgHeartBody, () => new HeartBody() },
            { MessageCommand.MsgXorBodyRsp, () => new XorBodyRsp() },
            { MessageCommand.MsgSecurityCheckReq, () => new SecurityCheckReq() },
            { MessageCommand.MsgSecurityCheckRsp, () => new SecurityCheckRsp() },
            { MessageCommand.MsgUdpPortRegisterReq, () => new UdpPortRegisterReq() },
            { MessageCommand.MsgUdpPortRegisterRsp, () => new UdpPortRegisterRsp() },
            // 登录模块：客户端->网关服->账号服
            { MessageCommand.MsgLoginReq, () => new LoginReq() },
            { MessageCommand.MsgLoginRsp, () => new LoginRsp() },
            { MessageCommand.MsgRegisterReq, () => new RegisterReq() },
            { MessageCommand.MsgRegisterRsp, () => new RegisterRsp() },
            { MessageCommand.MsgQuitLoginReq, () => new QuitLoginReq() },
            { MessageCommand.MsgQuitLoginRsp, () => new QuitLoginRsp() },
            // 房间模块：客户端->网关服->中心服
            { MessageCommand.MsgCreateRoomReq, () => new CreateRoomReq() },
            { MessageCommand.MsgCreateRoomRsp, () => new CreateRoomRsp() },
            { MessageCommand.MsgSearchRoomReq, () => new SearchRoomReq() },
            { MessageCommand.MsgSearchRoomRsp, () => new SearchRoomRsp() },
            { MessageCommand.MsgSelfJoinRoomReq, () => new SelfJoinRoomReq() },
            { MessageCommand.MsgSelfJoinRoomRsp, () => new SelfJoinRoomRsp() },
            { MessageCommand.MsgSelfQuitRoomReq, () => new SelfQuitRoomReq() },
            { MessageCommand.MsgSelfQuitRoomRsp, () => new SelfQuitRoomRsp() },
            { MessageCommand.MsgOtherJoinRoomRsp, () => new OtherJoinRoomRsp() },
            { MessageCommand.MsgOtherQuitRoomRsp, () => new OtherQuitRoomRsp() },
            { MessageCommand.MsgGetEnterSceneTokenReq, () => new GetEnterSceneTokenReq() },
            { MessageCommand.MsgGetEnterSceneTokenRsp, () => new GetEnterSceneTokenRsp() },
            // 场景模块：客户端->逻辑服
            { MessageCommand.MsgSceneLoginReq, () => new SceneLoginReq() },
            { MessageCommand.MsgSceneLoginRsp, () => new SceneLoginRsp() },
            { MessageCommand.MsgC2SenterScene, () => new C2SEnterScene() },
            { MessageCommand.MsgS2CenterScene, () => new S2CEnterScene() },
            { MessageCommand.MsgC2SleaveScene, () => new C2SLeaveScene() },
            { MessageCommand.MsgS2CleaveScene, () => new S2CLeaveScene() },
            { MessageCommand.MsgC2Smove, () => new C2SMove() },
            { MessageCommand.MsgS2Cmove, () => new S2CMove() },
            { MessageCommand.MsgC2SjumpAndGravity, () => new C2SJumpAndGravity() },
            { MessageCommand.MsgS2CjumpAndGravity, () => new S2CJumpAndGravity() },
            { MessageCommand.MsgC2SotherPlayerData, () => new C2SOtherPlayerData() },
            { MessageCommand.MsgS2CotherPlayerData, () => new S2COtherPlayerData() },
        };

        public static Dictionary<string, MessageCommand> NameToID { get; } = IDToCreator.ToDictionary(
            kvp => kvp.Value().Descriptor.FullName,
            kvp => kvp.Key
        );


        public void RegisterMessageCallback<T>(Action<T> callback) where T : class, IMessage, new()
        {
            // 注册消息回调
            string full_name = new T().Descriptor.FullName;
            MessageCommand id = NameToID[full_name];
            _callbacks[id] = MsgWrapper;
            _factories[id] = () => new T();
            Debug.Log($"注册消息回调: {full_name}");
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
        

        public IMessage TryCreateMessage(MessageCommand msg_id)
        {
            IMessage msg = _factories.TryGetValue(msg_id, out Func<IMessage> factory) ? factory() : null;
            return msg;
        }

        public bool OnProtobufMessage(IMessage message)
        {
            string full_name = message.Descriptor.FullName;
            MessageCommand id = NameToID[full_name];
            if (_callbacks.TryGetValue(id, out OnProtobufCallback callback))
            {
                callback(message);
                return true;
            }

            Debug.LogWarning($"未找到回调: {full_name}");
            return false;
        }
    }

}