using System;
using System.Collections;
using Manager;
using Network;
using Network.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ConnectPanel : BasePanel<ConnectPanel>
    {
        #region Inspector面板属性
        [SerializeField] private TMP_InputField inputServerIP;
        [SerializeField] private Button connectButton;
        [SerializeField] private Text   tipInfo;
        #endregion
        
        protected override void Awake()
        {
            base.Awake();
            // ConnectPanel 自己的初始化逻辑
            Debug.Log("ConnectPanel Awake");
        }

        private void Start()
        {
            connectButton.onClick.AddListener(ButtonOnClick_Connect);
            //输入框聚焦设置：无需点击输入框，即自动定位至输入框，可直接输入内容
            inputServerIP.ActivateInputField();
        }

        private bool ValidateInput()
        {
            if(!Utils.Utils.IsValidIPv4(inputServerIP.text))
            {
                ShowTipInfo(tipInfo,  "错误的IP地址");
                return false;
            }
            
            return true;
        }

        private void ButtonOnClick_Connect()
        {
            if (ValidateInput() == false) return;
            
            // 连接gate server
            NetworkManager.instance.client_gate.ConnectToServer(
                serverIP: inputServerIP.text,
                tcpPort: ConfigXML.gate_tcp_port,
                on_secure_event: info =>
                {
                    HidePanel();
                    LoginPanel.Instance.ShowPanel();
                }
            );
        }
            
    }
    
    
}
