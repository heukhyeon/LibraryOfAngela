using System;
using System.Collections;
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
            root?.StartCoroutine(AwaitAndRecheck(() =>
            {
                if (gameObject.activeInHierarchy && matchedTimeline != TimelinePatch.Instance.CurrentTimeline)
                {
                    // Logger.Log($"Timeline Diff : {matchedTimeline?.key}//{TimelinePatch.Instance.CurrentTimeline?.key}// {root} // {root.GetHashCode()}");
                    onTimelineConflict?.Invoke(root);
                }
            }));

        }

        private void OnDisable()
        {
            // 그냥 단순 열고 닫기는 감지 안함
            if (slot.openRect.activeSelf) return;

            root?.StartCoroutine(AwaitAndRecheck(() =>
            {
                if (!gameObject.activeSelf) onDisable?.Invoke(root);
            }));
        }

        /// <summary>
        /// 스토리 제어중 다 끄고 다시 킬때 비활성화로 인식될수 있다.
        /// 2프레임 기다린 후에도 꺼진거면 진짜 꺼진것.
        /// </summary>
        /// <returns></returns>
        private IEnumerator AwaitAndRecheck(Action check)
        {
            for (int i = 0; i < 2; i++) yield return YieldCache.waitFrame;
            check();
        }
    }
}
