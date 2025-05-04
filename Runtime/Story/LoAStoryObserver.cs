using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;

namespace LibraryOfAngela.Story
{
    class LoAStoryObserver : MonoBehaviour
    {
        public UIStoryProgressPanel root;
        public UIStoryProgressIconSlot slot;
        public Action<UIStoryProgressPanel> onTimelineConflict;
        public Action<UIStoryProgressPanel> onDisable;
        public TimelinePatch.TimelineData matchedTimeline;
        private void OnEnable()
        {
            if (matchedTimeline != TimelinePatch.Instance.CurrentTimeline)
            {
                onTimelineConflict?.Invoke(root);
            }
        }

        private void OnDisable()
        {
            // 그냥 단순 열고 닫기는 감지 안함
            if (slot.openRect.activeSelf) return;

            onDisable?.Invoke(root);
        }
    }
}
