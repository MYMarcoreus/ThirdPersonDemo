using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public enum UIPanelType { eNone, eConnect, eLogin, eRoom, ePrepare }

    public static class UIState
    {
        public static UIPanelType TargetPanel = UIPanelType.eConnect;
    }

    
    public class UIManager : MonoBehaviour
    {
        public static UIManager instance;
        private Stack<PanelController> panel_stack_ = new();     
        
        [SerializeField] private GameObject _connectPanel;
        [SerializeField] private GameObject _loginPanel;
        [SerializeField] private GameObject _roomPanel;
        [SerializeField] private GameObject _preparePanel;

        private void OnApplicationQuit()
        {
            UIState.TargetPanel = UIPanelType.eConnect;
        }

        void Awake()
        {
            instance = this;
            Debug.Log("NetworkManager Awake");
            
            // 先遍历所有直接子物体，激活它们
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                Transform child = gameObject.transform.GetChild(i);
                child.gameObject.SetActive(true);
                // Debug.Log($"Activate {child.name}");
            }
        }
        
        void Start()
        {
            // 再遍历所有直接子物体，禁用它们
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                Transform child = gameObject.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            ShowDefaultPanel();
        }

        public void SetDefaultPanel(UIPanelType panelType)
        {
            UIState.TargetPanel = panelType;
        }

        void ShowDefaultPanel()
        {
            switch (UIState.TargetPanel)
            {
                case UIPanelType.eNone:
                    ConnectPanel.Instance.HidePanel();
                    LoginPanel.Instance.HidePanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.HidePanel();
                    break;
                case UIPanelType.eConnect:
                    ConnectPanel.Instance.ShowPanel();
                    LoginPanel.Instance.HidePanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.HidePanel();
                    break;
                case UIPanelType.eLogin:
                    ConnectPanel.Instance.HidePanel();
                    LoginPanel.Instance.ShowPanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.HidePanel();
                    break;
                case UIPanelType.eRoom:
                    ConnectPanel.Instance.HidePanel();
                    LoginPanel.Instance.HidePanel();
                    RoomPanel.Instance.ShowPanel();
                    PreparePanel.Instance.HidePanel();
                    break;
                case UIPanelType.ePrepare:
                    ConnectPanel.Instance.HidePanel();
                    LoginPanel.Instance.HidePanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.ShowPanel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void ShowPanel(UIPanelType  panelType)
        {
            switch (panelType)
            {
                case UIPanelType.eNone:
                    ConnectPanel.Instance.HidePanel();
                    LoginPanel.Instance.HidePanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.HidePanel();
                    break;
                case UIPanelType.eConnect:
                    ConnectPanel.Instance.ShowPanel();
                    LoginPanel.Instance.HidePanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.HidePanel();
                    break;
                case UIPanelType.eLogin:
                    ConnectPanel.Instance.HidePanel();
                    LoginPanel.Instance.ShowPanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.HidePanel();
                    break;
                case UIPanelType.eRoom:
                    ConnectPanel.Instance.HidePanel();
                    LoginPanel.Instance.HidePanel();
                    RoomPanel.Instance.ShowPanel();
                    PreparePanel.Instance.HidePanel();
                    break;
                case UIPanelType.ePrepare:
                    ConnectPanel.Instance.HidePanel();
                    LoginPanel.Instance.HidePanel();
                    RoomPanel.Instance.HidePanel();
                    PreparePanel.Instance.ShowPanel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void PushPanel(PanelController panel)
        {
            if (panel_stack_.Count > 0)
            {
                panel_stack_.Peek().OnPause();
            }
            
            panel_stack_.Push(panel);
            panel.OnEnter();
        }

        public void PopPanel()
        {
            if (panel_stack_.TryPop(out PanelController closed_panel))
            {
                closed_panel.OnExit();
                if(panel_stack_.TryPeek(out PanelController now_panel))
                {
                    now_panel.OnRecovery();  
                }
            }
        }
    }
}
