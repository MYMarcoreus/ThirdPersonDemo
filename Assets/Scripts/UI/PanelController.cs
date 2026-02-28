using System;
using UnityEngine;

namespace UI
{
    public class PanelController: MonoBehaviour
    {
        public void ShowPanel()
        {
            gameObject.SetActive(true);
        }

        public void HidePanel()
        {
            gameObject.SetActive(false);
        }
        
        public virtual void OnEnter()
        {
            ShowPanel();
        }

        public virtual void OnExit()
        {
            HidePanel();
        }

        public virtual void OnRecovery()
        {
            ShowPanel();
        }
        
        public virtual void OnPause()
        {
            HidePanel();
        }



    }
}