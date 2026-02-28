using System;
using System.Linq;
using Character;
using Cinemachine;
using Network;
using Network.Core;
using UnityEngine;
using Utils;
using Yy.Protocol.App;

namespace Manager
{
    public class PlayerManager: MonoBehaviour
    {
        [Tooltip("Bind CinemachineCameraTarget to VirtualCamera")]
        public CinemachineVirtualCamera virtualCamera;
        
        // Resources.Load中的路径名：必须是以Resources文件夹的相对路径、必须省略文件的拓展名
        private GameObject other_player_prefab;
        private GameObject self_player_prefab ;
        
        private LogicClient logic_client_;
        
        private PlayerDataManager player_data_manager_;

        private const int    sendPackRate_ = 30; // 发包速率，1s发30个
        private UInt64 room_id_;
        
        //todo 网络消息先于PlayerManager系统初始化，如何处理？
        private void Awake()
        {
            other_player_prefab = Resources.Load<GameObject>("Prefabs/other_player");
            self_player_prefab = Resources.Load<GameObject>("Prefabs/self_player");
            
            logic_client_ = NetworkManager.instance.client_logic;
            logic_client_.RegisterMessageCallback<S2CLeaveScene>(OnRecv_PlayerLeave);
            logic_client_.RegisterMessageCallback<C2SMove>(OnRecv_SelfMovement);
            logic_client_.RegisterMessageCallback<C2SJumpAndGravity>(OnRecv_SelfJumpAndGravity);
            logic_client_.RegisterMessageCallback<S2COtherPlayerData>(OnRecv_OtherPlayerDataRsp);
            logic_client_.RegisterMessageCallback<S2CMove>(OnRecv_OtherMovement);
            logic_client_.RegisterMessageCallback<S2CJumpAndGravity>(OnRecv_OtherJumpAndGravity);
            
            player_data_manager_ = PlayerDataManager.instance;
            room_id_ = RoomDataManager.instance.room_data.RoomId;
        }

        private void Start()
        {
            //! ①加载自己的数据
            SpawnSelfPlayers(player_data_manager_.SelfData.basedata);

            // ②加载其它玩家的数据
            foreach (OtherPlayerData other_data in player_data_manager_.GetAllOtherPlayers().ToList())
            {
                SpawnOtherPlayers(other_data.gamedata);
                Debug.Log($"登录成功：设置玩家{other_data.gamedata.AccountData.Uid}的数据！");
            }
            InvokeRepeating(nameof(SendSelfStateSync), 0.5f, 1f / sendPackRate_);
        }
        
        private void SendSelfStateSync()
        {
            (PlayerMove move, PlayerJumpAndGravity jumpAndGravity) = player_data_manager_.SelfData.controller.PackNetSync();
            Send_SelfMovement(move);
            Send_SelfJumpAndGravity(jumpAndGravity);
        }

        private void SpawnSelfPlayers(PlayerBaseData basedata)
        {
            // 1. 生成预制体对象
            GameObject self_obj = UnityEngine.Object.Instantiate(self_player_prefab); 
            // 2. 找到角色中的 PlayerCameraRoot（用于绑定相机）
            Transform cameraRoot = self_obj.transform.Find("PlayerCameraRoot");
            if (cameraRoot == null)
            {
                Debug.LogError("PlayerCameraRoot not found in player prefab!");
                return;
            }
            // 3. 设置相机的 Follow 和 LookAt
            virtualCamera.Follow = cameraRoot;
            virtualCamera.LookAt = cameraRoot;
            
            // 设置角色的出生点
            (Vector3 pos, Quaternion rot) = Utils.Struct.ParseNetTransform(basedata.Movement.Transform);
            self_obj.transform.SetPositionAndRotation(pos, rot); 
            self_obj.name = $"self_{basedata.AccountData.Uid}";
            
            player_data_manager_.SelfData.controller = self_obj.GetComponent<SelfPlayerController>();

            Debug.Log($"登录成功：设置自身的基础数据：{basedata}");
        }
        
        private void SpawnOtherPlayers(PlayerBaseData basedata)
        {
            if (basedata.AccountData.Uid == PlayerDataManager.Uid)
            {
                Debug.Log($"[OtherData] 收到自己数据<{basedata.AccountData.Uid}>，已忽略。");
                return;
            }

            GameObject other_obj = UnityEngine.Object.Instantiate(other_player_prefab);
            (Vector3 pos, Quaternion rot) = Utils.Struct.ParseNetTransform(basedata.Movement.Transform);
            other_obj.transform.SetPositionAndRotation(pos, rot);
            other_obj.name = $"other_{basedata.AccountData.Uid}";

            player_data_manager_.AddOrUpdateOtherPlayer(
                basedata, 
                controller: other_obj.GetComponent<OtherPlayerController>()
            );

            Debug.Log($"[OtherData] 接收新玩家<{basedata.AccountData.Uid}>的数据完成。");
        }

        #region 消息处理
        /// <summary>
        /// 发送自己的移动数据
        /// </summary>
        /// <param name="move"></param>
        public void Send_SelfMovement(PlayerMove move)
        {
            if (!logic_client_.tcp_session.IsStateLoggedIn()) return;
            
            C2SMove selfMove = new()
            {
                Uid = PlayerDataManager.Uid,
                Movement = move,
                RoomId = room_id_,
            };
            print($"<{logic_client_.udp_session.sock.Client.LocalEndPoint}>向<{logic_client_.udp_session.server_udp_ep}>发送UDP的C2SMove: {selfMove}");

            // logic_client_.CreateUdpPackage(selfMove);
            logic_client_.CreateTcpPackage(selfMove);
        }

        /// <summary>
        /// 申请其他人的详细数据
        /// </summary>
        private static float temp_time = 0;
        public void Send_OtherPlayerDataRequest(UInt64 otheruid)
        {
            if (!logic_client_.tcp_session.IsStateLoggedIn()) return;

            float value = Time.realtimeSinceStartup - temp_time;
            if (value < 0.1f) return;
            temp_time = Time.realtimeSinceStartup;

            C2SOtherPlayerData mem = new() {
                RequesterUid = PlayerDataManager.Uid,
                RequestedUid = otheruid,
                RoomId = room_id_
            };
            logic_client_.CreateTcpPackage(mem);

            Debug.Log($"{mem.RequesterUid}请求其它玩家的数据{mem.RequestedUid}");
        }

        /// <summary>
        /// 发送自己Animator数据
        /// </summary>
        public void Send_SelfJumpAndGravity(PlayerJumpAndGravity jump)
        {
            if (!logic_client_.tcp_session.IsStateLoggedIn()) return;
            
            C2SJumpAndGravity selfJumpAndGravity = new()
            {
                Uid = PlayerDataManager.Uid,
                JumpAndGravity = jump,
                RoomId = room_id_
            };
            logic_client_.CreateTcpPackage(selfJumpAndGravity);
        }

        /// <summary>
        /// 收到其他人详细数据
        /// </summary>
        private void OnRecv_OtherPlayerDataRsp(S2COtherPlayerData otherBasedata)
        {
            Debug.Log($"收到其它玩家的数据<{otherBasedata.OtherData.AccountData.Uid}>");
            if (otherBasedata.RoomId != room_id_)
            {
                Debug.LogError("收到其它房间的玩家数据！");
                return;
            }
            SpawnOtherPlayers(otherBasedata.OtherData);
        }

        /// <summary>
        /// 收到其他人移动
        /// </summary>
        private void OnRecv_OtherMovement(S2CMove move)
        {
            if (move.RoomId != room_id_)
            {
                Debug.LogError("收到其它房间的玩家数据！");
                return;
            }
            
            // 收到了move但是没有该玩家的初始数据，申请获取？
            if (!player_data_manager_.TryGetOtherPlayer(move.Uid, out OtherPlayerData otherPlayer))
            {
                Send_OtherPlayerDataRequest(move.Uid);
                return;
            }

            OtherPlayerController otherController = otherPlayer.controller;
            otherController.SetMove(move.Movement);
        }

        /// <summary>
        /// 收到某人的JumpAndGravity
        /// </summary>
        private void OnRecv_OtherJumpAndGravity(S2CJumpAndGravity otherJump)
        {
            if (otherJump.RoomId != room_id_)
            {
                Debug.LogError("收到其它房间的玩家数据！");
                return;
            }
            
            if (!player_data_manager_.TryGetOtherPlayer(otherJump.Uid, out OtherPlayerData otherPlayer))
            {
                Send_OtherPlayerDataRequest(otherJump.Uid);
                return;
            }

            OtherPlayerController otherController = otherPlayer.controller;
            otherController.SetJumpAndGravity(otherJump.JumpAndGravity);
        }

        /// <summary>
        /// 收到某人离开
        /// </summary>
        private void OnRecv_PlayerLeave(S2CLeaveScene mem)
        {
            // if (mem.RoomId != room_id_)
            // {
            //     Debug.LogError("收到其它房间的玩家数据！");
            // }
            
            if (player_data_manager_.DestroyOtherPlayer(mem.Uid))
            {
                Debug.Log("player leave and destroy:..." + mem.Uid);
            }
            else
            {
                Debug.Log($"PlayerLeave: 没有这个UID {mem.Uid}");
            }
        }

        private void OnRecv_SelfMovement(C2SMove move)
        {
            (Vector3 pos, Quaternion rot) = Utils.Struct.ParseNetTransform(move.Movement.Transform);;

            //todo
        }

        private void OnRecv_SelfJumpAndGravity(C2SJumpAndGravity selfJump)
        {
            //todo
        }
        #endregion
    }
}