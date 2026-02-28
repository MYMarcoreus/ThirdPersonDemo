using System;
using Google.Protobuf;
using Network;
using UnityEngine;

namespace Manager
{
    public class NetworkManager: MonoBehaviour
    {
        public static NetworkManager instance;
        public GateClient  client_gate;
        public LogicClient client_logic;
        [NonSerialized] public string user_token;
        [NonSerialized] public string scene_token;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject); // 如果已有实例，销毁当前这个重复的
                return;
            }
            
            Debug.Log("NetworkManager Awake");
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            
            client_gate = new GateClient();
            client_logic = new LogicClient();
        }

        public void ResetLogicClient()
        {
            client_logic.DisconnectServer("new", false);
            client_logic = new LogicClient();
        }

        public void Update()
        {
            client_gate.Update();
            client_logic.Update();
        }

        public void RegisterGateMessageCallback<T>(Action<T> callback)
            where T : class, IMessage, new()
        {
            client_gate.RegisterMessageCallback(callback);
        }

        public void RegisterLogicMessageCallback<T>(Action<T> callback)
            where T : class, IMessage, new()
        {
            client_logic.RegisterMessageCallback(callback);
        }
    }
}