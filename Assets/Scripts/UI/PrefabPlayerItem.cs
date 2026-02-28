using UnityEngine;
using UnityEngine.UI;
using Yy.Protocol.App;

namespace UI
{
    public class PrefabPlayerItem: MonoBehaviour
    {
        [SerializeField] private Text playerNameText;
        [SerializeField] private Text hostText;
        
        public  bool isHost; 
        public AccountBaseData player_data;

        public void SetText(AccountBaseData __player_data, bool is_host)
        {
            player_data = __player_data;
            playerNameText.text = player_data.Username;
            isHost = is_host;
            hostText.text = isHost ? "房主" : "";
        }
    }
}