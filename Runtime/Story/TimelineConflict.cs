// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LibraryOfAngela.Extension;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.Story
{
    class TimelineConflict : Singleton<TimelineConflict>
    {
        private Dictionary<UIStoryProgressPanel, Button> codReturnButton;
        private Dictionary<UIStoryProgressPanel, List<GameObject>> otherTimelineButtons;
        public Dictionary<UIStoryProgressPanel, List<UIStoryProgressIconSlot>> slots;

        public void Initialize()
        {
            bool otherTimelineDetect = false;
            foreach (var t in new string[] { "COD_Regret.StoryLineMaker, COD_Regret", "CityOfDramaMod.StoryLineMaker, COD_Monster" })
            {
                if (Type.GetType(t) != null)
                {
                    Logger.Log("Timeline - CityOfDrama Detected");
                    codReturnButton = new Dictionary<UIStoryProgressPanel, Button>();
                    otherTimelineDetect = true;
                    break;
                }
            }
            if (!otherTimelineDetect)
            {
                otherTimelineDetect = Type.GetType("ColdSun.LY_init, ColdSun") != null;
            }
            if (otherTimelineDetect)
            {
                otherTimelineButtons = new Dictionary<UIStoryProgressPanel, List<GameObject>>();
            }
        }

        public void CheckCoDEnabled(UIStoryProgressPanel panel)
        {
            if (codReturnButton is null) return;

            if (!codReturnButton.ContainsKey(panel))
            {
                var obj = Type.GetType("COD_Regret.StoryLineMaker, COD_Regret")?.GetField("returnStory");
                if (obj is null) obj = Type.GetType("CityOfDramaMod.StoryLineMaker, COD_Monster")?.GetField("returnStory");
                if (obj != null) codReturnButton[panel] = obj.GetValue(null) as Button;
                else codReturnButton[panel] = null;

            }

            var btn = codReturnButton[panel];
            if (btn is null) return;
            if (btn.gameObject.activeSelf)
            {
                Logger.Log("CityOfDrama Enabled Detect, Disable");
                btn.onClick.Invoke();
            }
        }

        public void CheckRCorpExperimentEnabled(UIStoryProgressPanel panel)
        {

        }

        public void FixTimelineRestore(UIStoryProgressPanel panel)
        {
            //TimelineUtil 과 충돌시 -> 그냥 리셋시켜버리면 충돌 해결됨
            panel.SetSelectFirstSlot();
        }

        public void ViewPortDisabled(UIStoryProgressPanel root)
        {
            // 부모도 꺼진 경우면 그냥 꺼질게 꺼진것
            if (!root.gameObject.activeInHierarchy) return;
            if (TimelinePatch.Instance.CurrentTimeline != null)
            {
                Logger.Log("Timeline Moved, Restore Other Icons");
                ToggleOtherStoryIcons(root, true);
                TimelinePatch.Instance.ResetTimeline(root);
            }
        }

        public void ToggleOtherStoryIcons(UIStoryProgressPanel currentPanel, bool enable)
        {
            Instance.slots?.SafeGet(currentPanel)?.ForEach(d =>
            {
                try
                {
                    d.closeRect.SetActive(enable);
                    if (!d.isChapterIcon)
                    {
                        d.SetActiveLine(enable);
                    }
                }
                catch (NullReferenceException)
                {
                    Logger.Log($"StorySlot Control NullReferenceException in {d.name}");
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            });
        }

        public void ToggleOtherTimelineEnterVisible(UIStoryProgressPanel currentPanel, bool enable)
        {
            if (otherTimelineButtons is null) return;
            if (!otherTimelineButtons.ContainsKey(currentPanel))
            {
                var objects = new List<GameObject>();
                try
                {
                    var cityOfStarPhase = currentPanel.chapterIconList[5];
                    foreach (var name in new string[] { "ColdSun_HZ_ChangeButton", "COD_ChangeBtn" })
                    {
                        var target = cityOfStarPhase.transform.parent.Find(name);
                        if (target != null)
                        {
                            objects.Add(target.gameObject);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Error in Timeline Enter Button Find");
                    Logger.LogError(e);
                }
                otherTimelineButtons[currentPanel] = objects;
            }
            foreach (var b in otherTimelineButtons[currentPanel])
            {
                b.SetActive(enable);
            }
        }
    }
}
