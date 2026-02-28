using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class BasePanel<T> : PanelController where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected Coroutine currentTipRoutine;
        
        protected virtual void Awake()
        {
            // 设置单例
            if (Instance && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this as T;
            HidePanel();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tip_info"></param>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        public void ShowTipInfo(Text tip_info, string message, float duration = 3f)
        {
            tip_info.text = message;
            Debug.Log($"tipinfo: {message}");
            tip_info.gameObject.SetActive(true);

            // 如果之前有一个协程在运行，先停掉。目的：防止多个协程同时运行造成 UI 闪烁或逻辑混乱
            // （多个协程会有各自的Delay，然后会导致在计时结束后陆续争用同一个tipInfo，本质上和共享并重置Delay是一样的）。
            if (currentTipRoutine != null)
                StopCoroutine(currentTipRoutine);
            
            // 启动协程，几秒后隐藏提示
            currentTipRoutine = StartCoroutine(HideAfterDelay(duration));  // 显示 duration 秒
            return;

            IEnumerator HideAfterDelay(float delay)
            {
                // 暂停协程直到条件满足（如等待几秒）
                yield return new WaitForSeconds(delay);
                // 条件满足后（等待时间结束），继续执行下一条语句
                tip_info.gameObject.SetActive(false);
            }
        }
    }

}