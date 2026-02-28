using System;
using System.Collections;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yy.Protocol.App;
using Network;
using Network.Core;
using Unity.VisualScripting;

namespace UI
{
    public class LoginPanel : BasePanel<LoginPanel>
    {
        #region Inspector面板属性
        [SerializeField] private TMP_InputField inputAccountName;
        [SerializeField] private TMP_InputField inputAccountPassword;
        [SerializeField] private Button         buttonLogin;
        [SerializeField] private Button         buttonRegister;
        [SerializeField] private Text           tipInfo;
        #endregion
  
        private Coroutine timeout_coroutine_;
        private bool isDoing_Login_Register;
        private GateClient gate_client_;
        
        /// <summary>
        /// 脚本加载时设置单例为this
        /// </summary>
        protected override void Awake()
        {
            base.Awake(); // 调用基类的 Awake()，以设置单例

            // LoginPanel 自己的初始化逻辑
            Debug.Log("LoginPanel Awake");
        }
        
        /// <summary>
        /// Update前
        /// </summary>
        private void Start()
        {
            Debug.Log("LoginPanel Start");
            gate_client_ = NetworkManager.instance.client_gate;
            gate_client_.RegisterMessageCallback<LoginRsp>(OnLoginRsp);
            gate_client_.RegisterMessageCallback<RegisterRsp>(OnRegisterRsp);
            
            buttonLogin.onClick.AddListener(ButtonOnClick_Login);
            buttonRegister.onClick.AddListener(ButtonOnClick_Register);
        }

        private bool ValidateInput()
        {
            if (inputAccountName.text.Length > 12)
            {
                ShowTipInfo(tipInfo,  "账号长度不能超过12");
                return false;      
            }
            if (inputAccountPassword.text.Length > 12)
            {
                ShowTipInfo(tipInfo,  "密码长度不能超过12");
                return false;         
            }
            if (inputAccountName.text.Length < 4)
            {
                ShowTipInfo(tipInfo,  "账号长度不能小于4");
                return false;      
            }
            if (inputAccountPassword.text.Length < 4)
            {
                ShowTipInfo(tipInfo,  "密码长度不能小于4");
                return false;         
            }
            if (!Utils.Utils.IsAlphanumeric(inputAccountName.text))
            {
                ShowTipInfo(tipInfo,  "账号名只能包含字母和数字");
                return false;    
            }
            if (!Utils.Utils.IsAlphanumeric(inputAccountPassword.text))
            {
                ShowTipInfo(tipInfo,  "密码只能包含字母和数字");
                return false;    
            }
            
            return true;
        }
        




        #region 注册逻辑
        private void ButtonOnClick_Register()
        {
            if (isDoing_Login_Register) {
                ShowTipInfo(tipInfo, "登录/注册正在执行中");
                return;
            }
            if (ValidateInput() == false)
            {
                return;
            }
            
            if (gate_client_.IsSecure()) {
                isDoing_Login_Register = true;
                
                // 发送注册请求
                SendRegisterReq("已连接后发送注册请求");
                
                // 启动超时协程（比如 5 秒内没有响应就恢复按钮）
                timeout_coroutine_ = StartCoroutine(RegisterTimeoutCoroutine(5f));
            }
            else {
                ShowTipInfo(tipInfo,  "服务器未连接！");
            }
            
            return;
            
            IEnumerator RegisterTimeoutCoroutine(float timeoutSeconds)
            {
                yield return new WaitForSeconds(timeoutSeconds);

                ShowTipInfo(tipInfo,  "登录超时");
                isDoing_Login_Register = false;
            }
        }
        #endregion

        #region 登录逻辑
        private void ButtonOnClick_Login()
        {
            if (isDoing_Login_Register)
            {
                ShowTipInfo(tipInfo, "登录/注册正在执行中");
                return;
            }
            if (ValidateInput() == false)
            {
                return;
            }
            
            if (gate_client_.IsSecure()) {
                isDoing_Login_Register = true;
                
                // 发送登录请求
                SendLoginReq("已连接后发送登录请求");
                
                // 启动超时协程（比如 5 秒内没有响应就恢复按钮）
                timeout_coroutine_ = StartCoroutine(LoginTimeoutCoroutine(5f));
            }
            else {
                ShowTipInfo(tipInfo,  "服务器未连接！");
            }
            
            return;
            
            IEnumerator LoginTimeoutCoroutine(float timeoutSeconds)
            {
                yield return new WaitForSeconds(timeoutSeconds);

                ShowTipInfo(tipInfo,  "登录超时");
                isDoing_Login_Register = false;
            }
        }

        private void SendLoginReq(string info)
        {
            ShowTipInfo(tipInfo, $"发送登录请求：{info}");
            LoginReq req = new()
            {
                Username = inputAccountName.text,
                Password = inputAccountPassword.text,
            };
            Debug.Log($"封包发送登录请求");
            gate_client_.CreateTcpPackage(req);
        }

        private void OnLoginRsp(LoginRsp rsp)
        {
            if (timeout_coroutine_ != null)
            {
                StopCoroutine(timeout_coroutine_);
                timeout_coroutine_ = null;
            }
            
            switch (rsp.ResultCode)
            {
                case LoginRsp.Types.Status.ESuccess:
                    // ShowTipInfo(tipInfo,  "服务器：登录成功");
                    gate_client_.state = E_ClientSocketState.eLoggedIn;
                    PlayerDataManager.instance.SelfData.basedata.AccountData = rsp.AccountData;
                    Debug.Log($"登录成功，获取账号信息: {rsp.GetType().Name}, {rsp}");
                    NetworkManager.instance.user_token = rsp.Token;
                    HidePanel();
                    RoomPanel.Instance.ShowPanel();                        
                    break;
                case LoginRsp.Types.Status.EAccountNotExist:
                    ShowTipInfo(tipInfo,  "服务器：账号不存在");
                    break;
                case LoginRsp.Types.Status.EPasswordError:
                    ShowTipInfo(tipInfo,  "服务器：密码错误");
                    break;
                case LoginRsp.Types.Status.EUnknownError:
                    ShowTipInfo(tipInfo,  "服务器：未知错误");
                    break;
                case LoginRsp.Types.Status.EAlreadyLoggedIn:
                    ShowTipInfo(tipInfo,  "服务器：禁止重复登录");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            isDoing_Login_Register = false;
        }
        
        private void SendRegisterReq(string info)
        {
            ShowTipInfo(tipInfo, $"发送登录请求：{info}");
            RegisterReq req = new()
            {
                Username = inputAccountName.text,
                Password = inputAccountPassword.text
            };
            Debug.Log($"封包发送登录请求");
            gate_client_.CreateTcpPackage(req);
        }
        
        private void OnRegisterRsp(RegisterRsp rsp)
        {
            if (timeout_coroutine_ != null)
            {
                StopCoroutine(timeout_coroutine_);
                timeout_coroutine_ = null;
            }
            
            switch (rsp.ResultCode)
            {
                case RegisterRsp.Types.Status.ESuccess:
                    ShowTipInfo(tipInfo,  "服务器：注册成功");
                    break;
                case RegisterRsp.Types.Status.EAccountAlreadyExist:
                    ShowTipInfo(tipInfo,  "服务器：账号已存在");
                    break;
                case RegisterRsp.Types.Status.EUnknownError:
                    ShowTipInfo(tipInfo,  "服务器：未知错误");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            isDoing_Login_Register = false;
        }
        
        #endregion
    }
}
