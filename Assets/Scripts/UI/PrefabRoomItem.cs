using System;
using Manager;
using Network;
using UnityEngine;
using UnityEngine.UI;
using Yy.Protocol.App;

namespace UI
{
    public class PrefabRoomItem : MonoBehaviour
    {
        [SerializeField] private Button selfJoinButton;
        [SerializeField] private Text roomNameText;
        [SerializeField] private Text roomSizeText;
        [SerializeField] private Text tipInfo;

        private RoomDetailData room_data_;
        private GateClient gate_client_;
    
        // Start is called before the first frame update
        void Start()
        {
            selfJoinButton.onClick.AddListener(ButtonOnClick_JoinRoom);
            gate_client_ = NetworkManager.instance.client_gate;
            gate_client_.RegisterMessageCallback<SelfJoinRoomRsp>(OnSelfJoinRoomRsp);
        }

        public void SetRoomInfo(RoomDetailData room_data)
        {
            room_data_ = room_data;
            roomNameText.text = room_data_.Name;
            roomSizeText.text = $"{room_data_.ExistPlayerDatas.Count}/{room_data_.Capacity}";
        }

        private void ButtonOnClick_JoinRoom()
        {
            Debug.Log("RoomPanel ButtonOnClick_SearchRoom");
            SendSelfJoinRoomReq();
        }
        private void SendSelfJoinRoomReq()
        {
            var req = new SelfJoinRoomReq
            {
                RoomId = room_data_.RoomId,
                JoinnerData = PlayerDataManager.instance.SelfData.basedata.AccountData,
                UserToken = NetworkManager.instance.user_token
            };
            
            Debug.Log($"SendSelfJoinRoomReq: {req}");
            gate_client_.CreateTcpPackage(req);
        }
        private void OnSelfJoinRoomRsp(SelfJoinRoomRsp rsp)
        {
            if (rsp.Uid != PlayerDataManager.instance.SelfData.basedata.AccountData.Uid)
            {
                Debug.LogError("OnSelfJoinRoomRsp：收到了别人的请求！");
                return;
            }
            Debug.Log($"OnSelfJoinRoomRsp: {rsp}");

            switch (rsp.ResultCode)
            {
                case SelfJoinRoomRsp.Types.Status.ESuccess:
                    RoomPanel.Instance.ClearPanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.InitPanel(rsp.RoomData, rsp.RoomIp, (int)rsp.RoomPort);
                    PreparePanel.Instance.ShowPanel();
                    break;
                case SelfJoinRoomRsp.Types.Status.ERoomNotExist:
                    RoomPanel.Instance.ShowTipInfo(tipInfo, "加入房间：房间不存在");
                    break;
                case SelfJoinRoomRsp.Types.Status.EUnknownError:
                    RoomPanel.Instance.ShowTipInfo(tipInfo, "加入房间：未知错误");
                    break;
                case SelfJoinRoomRsp.Types.Status.ERoomFull:
                    RoomPanel.Instance.ShowTipInfo(tipInfo, "加入房间：房间已满");
                    break;
                case SelfJoinRoomRsp.Types.Status.EAlreadyJoined:
                    RoomPanel.Instance.ShowTipInfo(tipInfo, "加入房间：已经加入");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
