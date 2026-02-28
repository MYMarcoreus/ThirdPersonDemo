using System;
using System.Collections;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using Yy.Protocol.App;

namespace UI
{
    public class RoomPanel : BasePanel<RoomPanel>
    {
        #region Inspector面板属性

        [SerializeField] private Button buttonEnterCreate;
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private InputField roomNameInput;
        [SerializeField] private InputField roomCapacityInput;
        
        [SerializeField] private Button buttonBackLogin;
        
        [SerializeField] private Button buttonSearch;
        [SerializeField] private GameObject roomInfoPanel;
        [SerializeField] private GameObject room_item_parent;
        [SerializeField] private GameObject room_item_prefab;
        
        [SerializeField] private Button btnTestAddRoom;
        
        [SerializeField] private Button buttonCreateRoom;
        [SerializeField] private Text tipInfo;
        #endregion
        
        private Coroutine timeoutCoroutine_;
        
        private Coroutine refreshCoroutine;
        
        
        
        // SetActivate(true)时才会加载脚本然后开始执行 Awake-> OnEnable -->Start
        protected override void Awake()
        {
            base.Awake(); // 调用基类的 Awake()，以设置单例

            // LoginPanel 自己的初始化逻辑
            Debug.Log("RoomPanel Awake");
            roomCapacityInput.contentType = InputField.ContentType.IntegerNumber;
        }

        private void Start()
        {
            NetworkManager.instance.client_gate.RegisterMessageCallback<SearchRoomRsp>(OnSearchRoomRsp);
            NetworkManager.instance.client_gate.RegisterMessageCallback<CreateRoomRsp>(OnCreateRoomRsp);
            NetworkManager.instance.client_gate.RegisterMessageCallback<QuitLoginRsp>(OnQuitLoginRsp);
            
            ClearPanel();
            
            // 搜索房间
            buttonSearch.onClick.AddListener(ButtonOnClick_SearchRoom);
            // 创建房间
            buttonEnterCreate.onClick.AddListener(() => {
                Debug.Log("RoomPanel  buttonEnterCreate Clicked");
                createRoomPanel.SetActive(true);
                roomInfoPanel.SetActive(false);
            });
            buttonCreateRoom.onClick.AddListener(ButtonOnClick_CreateRoom);
            // 返回登录界面
            buttonBackLogin.onClick.AddListener(ButtonOnClick_QuitLogin);
            
            // 测试新建房间项的按钮 
            btnTestAddRoom.gameObject.SetActive(false);
            btnTestAddRoom.onClick.AddListener(() =>
            {
                GameObject item_obj =  Instantiate(room_item_prefab, room_item_parent.transform, false);
                item_obj.transform.localScale = Vector3.one;
                
                PrefabRoomItem item = item_obj.GetComponent<PrefabRoomItem>();
                RoomDetailData room_data = new RoomDetailData
                {
                    Capacity = 8,
                    Name = "TestRoomName",
                    OwnerUid = 2,
                    RoomId = 123123123,
                    ExistPlayerDatas =
                    {
                        new AccountBaseData
                        {
                            Uid = 1,
                            Username = "TestPlayer1",
                        },
                        new AccountBaseData
                        {
                            Uid = 1,
                            Username = "TestPlayer1",
                        },
                    }
                };
                item.SetRoomInfo(room_data);
            });
        }
        
        void StartAutoRefreshRooms() {
            if (refreshCoroutine != null) StopCoroutine(refreshCoroutine);
            refreshCoroutine = StartCoroutine(AutoRefreshCoroutine());
            return; 
            
            IEnumerator AutoRefreshCoroutine() {
                while (true) {
                    SendSearchRoomReq();
                    yield return new WaitForSeconds(2f);  // 每10秒刷新一次
                }
            }
        }

        public void ClearPanel()
        {
            createRoomPanel.SetActive(false);
            roomInfoPanel.SetActive(false);
            ClearAllRoomItems();
        }

        public void ClearAllRoomItems()
        {
            for (int i = 0; i < room_item_parent.transform.childCount; i++)
            {
                Destroy(room_item_parent.transform.GetChild(i).gameObject);
            }
        }

        private void ButtonOnClick_QuitLogin()
        {
            SendQuitLoginReq();
        }

        private void SendQuitLoginReq()
        {
            QuitLoginReq req = new QuitLoginReq
            {
                Uid = PlayerDataManager.Uid,
            };
            NetworkManager.instance.client_gate.CreateTcpPackage(req);
            print($"SendQuitLoginReq 发送：{req}");
        }

        private void OnQuitLoginRsp(QuitLoginRsp rsp)
        {
            print($"OnQuitLoginRsp：{rsp}");
            switch (rsp.ResultCode)
            {
                case QuitLoginRsp.Types.Status.ESuccess:
                    ClearPanel();
                    LoginPanel.Instance.ShowPanel();
                    HidePanel();
                    break;
                case QuitLoginRsp.Types.Status.EAccountNotExist:
                    ShowTipInfo(tipInfo, "返回登录：要退出的账号不存在");
                    break;
                case QuitLoginRsp.Types.Status.ENotLogin:
                    ShowTipInfo(tipInfo, "返回登录：账号未登录");
                    break;
                case QuitLoginRsp.Types.Status.EUnknownError:
                    ShowTipInfo(tipInfo, "返回登录：未知错误");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void ButtonOnClick_CreateRoom()
        {
            Debug.Log("RoomPanel ButtonOnClick_CreateRoom");
            uint capacity = uint.Parse(roomCapacityInput.text);
            if (capacity > 10)
            {
                ShowTipInfo(tipInfo, "创建房间：人数不能大于10", 2f);
                return;
            } 
            SendCreateRoomReq();
        }
        void SendCreateRoomReq()
        {
            CreateRoomReq req = new CreateRoomReq
            {
                OwnerData = PlayerDataManager.AccountData,
                Capacity = uint.Parse(roomCapacityInput.text),
                Name = roomNameInput.text,
                UserToken = NetworkManager.instance.user_token,
            };
            NetworkManager.instance.client_gate.CreateTcpPackage(req);
        }
        void OnCreateRoomRsp(CreateRoomRsp rsp)
        {
            switch (rsp.ResultCode)
            {
                // 创建房间成功
                case CreateRoomRsp.Types.Status.ESuccess:
                    PreparePanel.Instance.InitPanel(rsp.RoomData, rsp.RoomIp, (int)rsp.RoomPort);
                    Debug.Log($"RoomPanel OnCreateRoomRsp: {rsp}");
                    ClearPanel();
                    HidePanel();
                    PreparePanel.Instance.ShowPanel();
                    break;
                case CreateRoomRsp.Types.Status.ENoServer:
                    ShowTipInfo(tipInfo, "创建房间：服务器已满，创建失败！", 2f);
                    break;
                case CreateRoomRsp.Types.Status.EUnknownError:
                    ShowTipInfo(tipInfo, "创建房间：未知错误！", 2f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void ButtonOnClick_SearchRoom()
        {
            Debug.Log("RoomPanel ButtonOnClick_SearchRoom");
            SendSearchRoomReq();
        }
        void SendSearchRoomReq()
        {
            var req = new SearchRoomReq
            {
                Uid = PlayerDataManager.Uid,
                UserToken = NetworkManager.instance.user_token,
            };
            NetworkManager.instance.client_gate.CreateTcpPackage(req);
        }
        void OnSearchRoomRsp(SearchRoomRsp rsp)
        {
            if (rsp.Uid != PlayerDataManager.Uid)
            {
                ShowTipInfo(tipInfo, "OnSearchRoomRsp：收到了别人的搜索房间的请求！", 2f);
                return;
            }
            ClearAllRoomItems();
            
            // 生成房间列表项
            foreach (RoomDetailData room_data in rsp.RoomDatas)
            {
                GameObject item_obj =  Instantiate(room_item_prefab, room_item_parent.transform, false);
                item_obj.transform.localScale = Vector3.one;
                
                var item = item_obj.GetComponent<PrefabRoomItem>();
                item.SetRoomInfo(room_data);
            }
            UIManager.instance.ShowPanel(UIPanelType.eRoom);
            createRoomPanel.SetActive(false);
            roomInfoPanel.SetActive(true);
        }
    }
}