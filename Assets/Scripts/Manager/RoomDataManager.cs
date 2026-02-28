using UnityEngine;
using Yy.Protocol.App;

namespace Manager
{
    public class RoomDataManager: MonoBehaviour
    {
        public static RoomDataManager instance;
        
        public RoomDetailData room_data;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject); // 如果已有实例，销毁当前这个重复的
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(this);
        }
    }
}