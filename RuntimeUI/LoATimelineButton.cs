using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LibraryOfAngela
{
    public class LoATimelineButton : MonoBehaviour
    {
        public LoATimelineSlot originSlot;
        
        [SerializeField]
        private UnityEvent onClear;
        private bool isToggled = false;
        public Action<bool> onChangeToggleStatus;
        public Action onEnterSlot = null;
        public Action<LoATimelineButton, string> onClickTimeline = null;
        public Color highlightColor;
        public Color normalColor;
        public GameObject releateObject;




        private void Awake()
        {
            isToggled = false;
        }

        public void Expand()
        {
            isToggled = true;
            onChangeToggleStatus(true);
        }
        public void Collapse()
        {
            isToggled = false;
            onChangeToggleStatus(false);
        }

        private void OnEnable()
        {
            //releateObject?.gameObject?.SetActive(true);
        }

        private void OnDisable()
        {
            Collapse();
            releateObject?.gameObject?.SetActive(false);
            onClear?.Invoke();
        }

        public void Toggle()
        {
            if (isToggled) Collapse();
            else Expand();
        }

        public void ClickTimeline(LoATimelineSlot slot)
        {
            onClickTimeline?.Invoke(this, slot.timeline);
            Collapse();
            onClear?.Invoke();
        }
	}
}
