using System;
using Manager;
using Network;
using Network.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yy.Protocol.App;

namespace UI
{
    public class PreparePanel: BasePanel<PreparePanel>
    {
        #region Inspector面板属性
        [SerializeField] private Button buttonLeave;
        [SerializeField] private Button buttonEnter;
        [SerializeField] private Button testbtn;
        [SerializeField] private GameObject player_item_parent;
        [SerializeField] private GameObject player_item_prefab;
        [SerializeField] private Text roomNameText;
        [SerializeField] private Text roomIdText;
        [SerializeField] private Text tipInfo;
        #endregion

        private RoomDetailData room_data_
        {
            get => RoomDataManager.instance.room_data;
            set => RoomDataManager.instance.room_data = value;
        }

        private string room_ip_;
        private int room_port_;
        
        private GateClient gate_client;
        private LogicClient logic_client;

        private void Start()
        {
            logic_client = NetworkManager.instance.client_logic;
            logic_client.RegisterMessageCallback<SceneLoginRsp>(OnSceneLoginRsp);
            logic_client.RegisterMessageCallback<S2CEnterScene>(OnEnterSceneRsp);
            
            gate_client = NetworkManager.instance.client_gate;
            gate_client.RegisterMessageCallback<OtherQuitRoomRsp>(OnOtherQuitRoomRsp);
            gate_client.RegisterMessageCallback<OtherJoinRoomRsp>(OnOtherJoinRoomRsp);
            gate_client.RegisterMessageCallback<SelfQuitRoomRsp>(OnSelfQuitRoomRsp);
            gate_client.RegisterMessageCallback<GetEnterSceneTokenRsp>(OnGetEnterSceneTokenRsp);

            ClearAllRoomItems();
            
            // Button
            buttonLeave.onClick.AddListener(ButtonOnClick_Leave);
            buttonEnter.onClick.AddListener(ButtonOnClick_Enter);
            testbtn.onClick.AddListener((() =>
            { 
                AddPlayerItem(new AccountBaseData{
                        Uid = 30213,
                        Username = "test_player"
                    }, 
                    true
                );
            }));
        }
        
        public void InitPanel(RoomDetailData data, string room_ip, int room_port)
        {
            Debug.Log($"PreparePanel::InitPanel {data}, {room_ip}, {room_port}");
            room_data_ = data;
            room_ip_ = room_ip;
            room_port_ = room_port;
            ClearAllRoomItems();
            foreach (AccountBaseData player_data in room_data_.ExistPlayerDatas)
            {
                bool is_host = player_data.Uid == room_data_.OwnerUid;
                AddPlayerItem(player_data, is_host);
            }
        }

        #region 玩家列表项管理
        
        /// <summary>
        /// 新增列表项 
        /// </summary>
        private void AddPlayerItem(AccountBaseData player_data, bool is_host)
        {
            GameObject item_obj =  Instantiate(player_item_prefab, player_item_parent.transform, false);
            item_obj.transform.localScale = Vector3.one;
            var item = item_obj.GetComponent<PrefabPlayerItem>();
            item.SetText(player_data, is_host);
        }

        /// <summary>
        /// 删除指定列表项 
        /// </summary>
        private void DelPlayerItem(UInt64 uid)
        {
            // 搜索列表项，删掉退出房间的玩家的列表项
            for (int i = 0; i < player_item_parent.transform.childCount; i++)
            {
                // 获取列表项管理组件
                GameObject obj = player_item_parent.transform.GetChild(i).gameObject;
                if (!obj.TryGetComponent(out PrefabPlayerItem item)) 
                    continue;
                        
                // 删除列表项
                if (item.player_data.Uid == uid) 
                    Destroy(obj);
            }
        }

        /// <summary>
        /// 删除所有列表项 
        /// </summary>
        private void ClearAllRoomItems()
        {
            for (int i = 0; i < player_item_parent.transform.childCount; i++)
            {
                Destroy(player_item_parent.transform.GetChild(i).gameObject);
            }
        }
        #endregion

        #region 别人加入和离开房间
        /// <summary>
        /// 他人加入房间，增加列表项
        /// </summary>
        private void OnOtherJoinRoomRsp(OtherJoinRoomRsp rsp)
        {
            Debug.Log($"OnOtherJoinRoomRsp: {rsp}");
            switch (rsp.ResultCode)
            {
                case OtherJoinRoomRsp.Types.Status.ESuccess:
                    // 新增列表项
                    AddPlayerItem(rsp.JoinnerData, false);
                    break;
                case OtherJoinRoomRsp.Types.Status.ERoomNotExist:
                    Debug.Log("OnOtherJoinRoomRsp：ERoomNotExist");
                    break;
                case OtherJoinRoomRsp.Types.Status.EUnknownError:
                    Debug.Log("OnOtherJoinRoomRsp：EUnknownError");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 他人离开房间， 删除列表项
        /// </summary>
        private void OnOtherQuitRoomRsp(OtherQuitRoomRsp rsp)
        {
            Debug.Log($"OnOtherQuitRoomRsp: {rsp}");
            switch (rsp.ResultCode)
            {
                case OtherQuitRoomRsp.Types.Status.ESuccess:
                    // 删除列表项
                    DelPlayerItem(rsp.Uid);
                    break;
                case OtherQuitRoomRsp.Types.Status.ERoomNotExist:
                    Debug.Log("OtherQuitRoomRsp：ERoomNotExist");
                    break;
                case OtherQuitRoomRsp.Types.Status.EUnknownError:
                    Debug.Log("OtherQuitRoomRsp：EUnknownError");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region 自己离开房间
        private void ButtonOnClick_Leave()
        {
            Debug.Log("RoomPanel ButtonOnClick_Leave");
            SendQuitRoom();
        }

        private void SendQuitRoom()
        {
            var req = new SelfQuitRoomReq
            {
                RoomId = room_data_.RoomId,
                Uid = PlayerDataManager.instance.SelfData.basedata.AccountData.Uid,
                UserToken = NetworkManager.instance.user_token,
            };
            gate_client.CreateTcpPackage(req);
        }

        private void OnSelfQuitRoomRsp(SelfQuitRoomRsp rsp)
        {
            switch (rsp.ResultCode)
            {
                case SelfQuitRoomRsp.Types.Status.ESuccess:
                    Debug.Log($"OnSelfQuitRoomRsp: {rsp}");
                    HidePanel();
                    RoomDataManager.instance.room_data = null;
                    PlayerDataManager.instance.ResetOtherDatas();
                    RoomPanel.Instance.ShowPanel();
                    break;
                case SelfQuitRoomRsp.Types.Status.ERoomNotExist:
                    Debug.Log("SelfQuitRoomRsp ERoomNotExist");
                    break;
                case SelfQuitRoomRsp.Types.Status.EUnknownError:
                    Debug.Log("SelfQuitRoomRsp EUnknownError");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region 进入场景
        private void ButtonOnClick_Enter()
        {
            Debug.Log("RoomPanel ButtonOnClick_Enter");
            // 连接房间所在的逻辑服
            logic_client.SetSecureEvent(info =>
            {
                SendGetEnterSceneTokenReq();
                logic_client.SetSecureEvent(null);
            });
            logic_client.ConnectToServer(room_ip_, room_port_);
        }
        private void SendGetEnterSceneTokenReq()
        {
            Debug.Log($"RoomPanel SendGetEnterSceneTokenReq: {room_data_}");
            var req = new GetEnterSceneTokenReq
            {
                RoomId    = room_data_.RoomId,
                Uid       = PlayerDataManager.instance.SelfData.basedata.AccountData.Uid,
                UserToken = NetworkManager.instance.user_token,
            };
            gate_client.CreateTcpPackage(req);
        }
        private void OnGetEnterSceneTokenRsp(GetEnterSceneTokenRsp rsp)
        {
            if (rsp.RoomId != room_data_.RoomId || rsp.Uid != PlayerDataManager.instance.SelfData.basedata.AccountData.Uid)
            {
                Debug.Log($"GetEnterSceneTokenRsp收到不属于自己的token: {rsp}");
                return;
            }
            Debug.Log($"收到SceneToken: {rsp}");
            NetworkManager.instance.scene_token = rsp.SceneToken;
            SendSceneLoginReq();
        }
        private void SendSceneLoginReq()
        {
            var req = new SceneLoginReq 
            {
                Uid = PlayerDataManager.instance.SelfData.basedata.AccountData.Uid,
                RoomId = room_data_.RoomId,
                SceneToken = NetworkManager.instance.scene_token,
                UserToken = NetworkManager.instance.user_token,
            };
            Debug.Log($"发送逻辑服登录请求: {req}");
            logic_client.CreateTcpPackage(req);
        }

        private void OnSceneLoginRsp(SceneLoginRsp rsp)
        {
            Debug.Log($"逻辑服登录成功: {rsp}");
            if (!rsp.IsOk)
            {
                Debug.Log($"SceneLoginRsp Failed: {rsp}");
                return;
            }
            SendEnterSceneReq();
        }

        private void SendEnterSceneReq()
        {
            var req = new C2SEnterScene 
            {
                Uid = PlayerDataManager.instance.SelfData.basedata.AccountData.Uid,
                RoomId = room_data_.RoomId,
            };
            Debug.Log($"发送进入场景请求: {req}");
            logic_client.CreateTcpPackage(req); //todo  逻辑服收不到响应
        }
        private void OnEnterSceneRsp(S2CEnterScene rsp)
        {
            Debug.Log($"OnEnterSceneRsp: {rsp}");
            if (rsp.RoomId != room_data_.RoomId || rsp.Uid != PlayerDataManager.instance.SelfData.basedata.AccountData.Uid)
            {
                Debug.Log($"GetEnterSceneTokenRsp收到不属于自己的S2CEnterScene消息, {rsp}");
                return;
            }
            if (rsp.Result == false)
            {
                Debug.Log("EnterSceneReq Failed");
                return;
            } 
            
            logic_client.state = E_ClientSocketState.eLoggedIn;
            
            // 初始化玩家自身数据
            Debug.Log($"EnterSceneReq收到自己玩家数据: {rsp.SelfData}");
            PlayerDataManager.instance.SetSelfData(rsp.SelfData, null);
            // 初始化其它玩家游戏数据
            foreach (PlayerBaseData other_base_data in rsp.OtherDatas)
            {
                Debug.Log($"EnterSceneReq收到其它玩家数据: {other_base_data}");
                PlayerDataManager.instance.AddOtherPlayer(other_base_data, null);
            }

            RoomDataManager.instance.room_data = room_data_;
            
            Debug.Log($"Load Playground Scene");
            SceneManager.LoadScene("Playground");
            
            HidePanel();
        }
        #endregion 
    }
}