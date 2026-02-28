using System.Collections.Generic;
using Character;
using UnityEngine;
using Yy.Protocol.App;

namespace Manager
{
    public class OtherPlayerData
    {
        public PlayerBaseData gamedata = new();
        public OtherPlayerController controller;
    }

    public class SelfPlayerData
    {
        public PlayerBaseData basedata =  new();
        public SelfPlayerController controller;
    }
    
    public class PlayerDataManager: MonoBehaviour
    {
        public static PlayerDataManager instance;

        private Dictionary<ulong, OtherPlayerData> others_data_dict; // 其他玩家的数据
        public SelfPlayerData SelfData { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject); // 如果已有实例，销毁当前这个重复的
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(this);
            
            others_data_dict = new Dictionary<ulong, OtherPlayerData>();
            SelfData = new SelfPlayerData();
        }

        public void SetSelfData(PlayerBaseData base_data, SelfPlayerController controller)
        {
            SelfData.basedata =  base_data.Clone();
            SelfData.controller = controller;
        }

        public static AccountBaseData AccountData => instance.SelfData.basedata.AccountData;
        public static ulong Uid => instance.SelfData.basedata.AccountData.Uid;
        public static PlayerBaseData Basedata => instance.SelfData.basedata;
        public static string UserName => instance.SelfData.basedata.AccountData.Username;

        public void ResetOtherDatas()
        {
            others_data_dict.Clear();
        }
        
        public bool DestroyOtherPlayer(ulong uid)
        {
            if (others_data_dict.Remove(uid, out OtherPlayerData player) == false) 
                return false;
            
            if (player?.controller)
                UnityEngine.Object.Destroy(player.controller.gameObject);
            
            return true;
        }
        
        public bool TryGetOtherPlayer(ulong uid, out OtherPlayerData playerData)
        {
            return others_data_dict.TryGetValue(uid, out playerData);
        }

        public Dictionary<ulong, OtherPlayerData>.ValueCollection GetAllOtherPlayers()
        {
            return others_data_dict.Values;
        }

        public bool HasOtherPlayer(ulong uid)
        {
            return others_data_dict.ContainsKey(uid);
        }
        
        public bool AddOtherPlayer(PlayerBaseData basedata, OtherPlayerController controller)
        {
            var other = new OtherPlayerData
            {
                gamedata = basedata,
                controller = controller
            };
            return others_data_dict.TryAdd(other.gamedata.AccountData.Uid, other);
        }
        
        public void AddOrUpdateOtherPlayer(PlayerBaseData basedata, OtherPlayerController controller)
        {
            var other = new OtherPlayerData
            {
                gamedata = basedata,
                controller = controller
            };
            others_data_dict[other.gamedata.AccountData.Uid] = other;
        }
    }
}
