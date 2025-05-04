using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Model;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.Story
{
    class TimelinePatch : Singleton<TimelinePatch>
    {
        public class TimelineData
        {
            public string key;
            public CustomStoryTimeline model;
            public Dictionary<UIStoryProgressPanel, List<UIStoryProgressIconSlot>> slots;
            public List<CustomStoryIconInfo> infos;

        }

        private Dictionary<UIStoryProgressPanel, LoATimelineButton> timelineRoots = new Dictionary<UIStoryProgressPanel, LoATimelineButton>();
        private Dictionary<string, TimelineData> datas = new Dictionary<string, TimelineData>();
        private Dictionary<UIStoryProgressPanel, bool> removedChapter = new Dictionary<UIStoryProgressPanel, bool>();
        private Dictionary<UIStoryProgressPanel, ScrollRect> overlays = new Dictionary<UIStoryProgressPanel, ScrollRect>();

        private Button returnButton = null;
        private bool isChanging = false;
        public TimelineData CurrentTimeline { get; private set; } = null;

        public void InitData(List<CustomStoryTimeline> timelines)
        {
            if (timelines.Count == 0)
            {
                Logger.Log("Timeline Empty, Skip");
                return;
            }

            int cnt = 0;
            var builder = new StringBuilder("Timeline Exists\n");

            Dictionary<string, List<CustomStoryIconInfo>> dic = new Dictionary<string, List<CustomStoryIconInfo>>();
            foreach (var mod in LoAModCache.StoryConfigs)
            {
                try
                {
                    foreach (var d in mod.GetStoryIcons() ?? new List<CustomStoryIconInfo>())
                    {
                        if (string.IsNullOrEmpty(d.storyTimeline)) continue;
                        if (!dic.ContainsKey(d.storyTimeline)) dic[d.storyTimeline] = new List<CustomStoryIconInfo>();
                        d.packageId = mod.packageId;
                        dic[d.storyTimeline].Add(d);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }

            foreach (var d in timelines.GroupBy(x => x.id))
            {
                var value = d.First();
                foreach (var c in d)
                {
                    if (value.returnButtonPosition.x > c.returnButtonPosition.x)
                    {
                        value.returnButtonPosition = c.returnButtonPosition;
                    }
                }
                cnt++;
                builder.AppendLine("- " + value.name);
                datas[d.Key] = new TimelineData
                {
                    key = d.Key,
                    model = value,
                    slots = new Dictionary<UIStoryProgressPanel, List<UIStoryProgressIconSlot>>(),
                    infos = dic[d.Key]
                };
            }
            TimelineConflict.Instance.Initialize();
            Logger.Log(builder.ToString());
        }

        public void Init(UIStoryProgressPanel panel)
        {
            
            try
            {
                if (panel is null || timelineRoots.ContainsKey(panel) || datas.Count == 0) return;

                var firstSlot = panel.gradeFilter.gradeSlots[0].gameObject;
                var obj2 = LoAFramework.BattleUiBundle.LoadAsset<GameObject>("Assets/CustomSelector/LoATimelineOverlay.prefab");
                var overlay = UnityEngine.Object.Instantiate(obj2, panel.gradeFilter.transform.parent).GetComponent<ScrollRect>();
                overlays[panel] = overlay;

                var obj = LoAFramework.BattleUiBundle.LoadAsset<GameObject>("Assets/CustomSelector/LoATimelineButton.prefab");
                var button = UnityEngine.Object.Instantiate(obj, firstSlot.transform.parent).GetComponent<LoATimelineButton>();
                button.onClickTimeline = SelectTimeline;
                button.transform.SetAsFirstSibling();
                button.onEnterSlot = () =>
                {
                    UISoundManager.instance.PlayEffectSound(UISoundType.Ui_BookOver);
                };
                button.highlightColor = UIColorManager.Manager.GetUIColor(UIColor.Highlighted);
                button.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                button.onChangeToggleStatus = (enabled) =>
                {
                    overlay.transform.position = button.transform.position.Copy(y: button.transform.position.y - 20f);
                    overlay.gameObject.SetActive(enabled);
                };
                timelineRoots[panel] = button;

                // TimelineUtil 이 ScrollRect 추가함
                if (panel.gradeFilter.GetComponent<ScrollRect>() == null)
                {
                    Logger.Log("Scroll Not Detect. Move Grade Filter");
                    var pos = panel.gradeFilter.transform.position;
                    panel.gradeFilter.transform.position = pos.Copy(x: pos.x - 80f);
                }
                else
                {
                    Logger.Log("Scroll Detect. Skip Move Grade Filter");
                }

                var com = panel.iconList[0].closeRect.AddComponent<LoAStoryObserver>();
                com.slot = panel.iconList[0];
                // com.onDisable = CheckStoryDisableFromOtherMod; // 바닐라는 onDisable 감지 안함
                com.onTimelineConflict = CheckTimelineConflict;

                foreach (var data in datas.OrderBy(d => d.Value.model.name))
                {
                    var slot = UnityEngine.Object.Instantiate(button.originSlot, overlay.content);
                    var sp = UISpriteDataManager.instance.GetStoryIcon(data.Value.model.artwork);
                    slot.timeline = data.Value.key;
                    slot.icon.sprite = sp.icon;
                    slot.iconGlow.sprite = sp.iconGlow;
                    slot.gameObject.SetActive(true);
                    if (sp.iconGlow == null)
                    {
                        slot.iconGlow.gameObject.SetActive(false);
                    }
                    slot.text.text = data.Value.model.name;
                }

                if (TimelineConflict.Instance.slots is null) TimelineConflict.Instance.slots = new Dictionary<UIStoryProgressPanel, List<UIStoryProgressIconSlot>>();
                if (!TimelineConflict.Instance.slots.ContainsKey(panel))
                {
                    var slots = panel.GetComponentsInChildren<UIStoryProgressIconSlot>();
                    TimelineConflict.Instance.slots[panel] = new List<UIStoryProgressIconSlot>(slots);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Exception in Timeline Init");
                Logger.LogError(e);
            }
        }

        public void SelectTimeline(LoATimelineButton button, string key)
        {
            var currentPanel = timelineRoots.FirstOrDefault(d => d.Value == button).Key;
            Logger.Log($"Timeline Call :: {key}");
            if (currentPanel is null) return;
            currentPanel?.currentSlot?.SetSlotOpen(false);

            if (!LoATimelineObserver.Instances[currentPanel].gameObject.activeSelf)
            {
                Logger.Log("Timeline Conflict Detect. Storyline Reset");
                TimelineConflict.Instance.FixTimelineRestore(currentPanel);
            }

            TimelineConflict.Instance.CheckCoDEnabled(currentPanel);
            var data = datas.SafeGet(key);
            if (data is null) return;

            isChanging = true;
            if (!data.slots.ContainsKey(currentPanel)) InitTimeline(currentPanel, data);
            if (CurrentTimeline != null) ResetTimeline(currentPanel);
            CurrentTimeline = data;
            TimelineConflict.Instance.ToggleOtherStoryIcons(currentPanel, false);
            //TimelineConflict.Instance.ToggleOtherTimelineEnterVisible(currentPanel, false);
            currentPanel.chapterIconList.ForEach(d =>
            {
                d.gameObject.SetActive(false);
            });
            removedChapter[currentPanel] = true;

            UIStoryProgressIconSlot firstSlot = null;
            data.slots[currentPanel].ForEach(d =>
            {
                d.SetSlotData(new List<StageClassInfo>());
                d.SetActiveStory(true);
                if (firstSlot is null)
                {
                    firstSlot = d;
                }
            });
            if (firstSlot != null)
            {
                currentPanel.MoveIconTarget(firstSlot);
                currentPanel.targetScale = Vector3.one * 0.65f;
            }
            UISoundManager.instance.PlayEffectSound(UISoundType.Card_Over);
            UpdateReturnButton(currentPanel, data);
            isChanging = false;
        }

        public void ResetTimeline(UIStoryProgressPanel panel)
        {
            foreach (var d in datas)
            {
                var slots = d.Value.slots?.SafeGet(panel);
                if (slots is null || slots.Count == 0) continue;
                foreach (var s in slots)
                {
                    s.SetActiveStory(false);
                }
            }

            if (!(returnButton is null))
            {
                returnButton.gameObject.SetActive(false);
            }

            if (removedChapter.ContainsKey(panel) && removedChapter[panel] && panel.iconList[0].gameObject.activeSelf)
            {
                panel.chapterIconList.ForEach(d =>
                {
                    d.gameObject.SetActive(true);
                });
                removedChapter[panel] = false;
            }

            if (CurrentTimeline != null)
            {
                //TimelineConflict.Instance.ToggleOtherTimelineEnterVisible(panel, true);
                CurrentTimeline = null;
            }

            if (overlays.ContainsKey(panel))
            {
                overlays[panel].gameObject.SetActive(false);
            }
        }

        public UIStoryProgressIconSlot CreateSlot(UIStoryProgressPanel root, CustomStoryIconInfo modIcon)
        {
            var allStorys = root.iconList;
            var relatedObject = allStorys.Find(x => x.currentStory == modIcon.relatedStoryLinePosition);
            if (relatedObject == null) relatedObject = allStorys.Find(d => d.currentStory == UIStoryLine.Rats);

            var iconObj = UnityEngine.Object.Instantiate(relatedObject.gameObject, relatedObject.transform.parent).GetComponent<UIStoryProgressIconSlot>();
            iconObj.connectLineList = new List<GameObject>();
            iconObj.isChapterIcon = false;
            iconObj.currentStory = UIStoryLine.HanaAssociation;
            iconObj.Initialized(root);
            iconObj.gameObject.name = $"LoAStory_{modIcon.packageId}_{modIcon.artwork}";

            modIcon.stageIds.ForEach(x =>
            {
                x.packageId = modIcon.packageId;
                StoryPatch.Instance.visibleConditions[new LorId(x.packageId, x.id)] = x.visibleCondition;
            });

            var lines = FrameworkExtension.GetSafeAction(() => modIcon.lines);
            if (lines != null && lines.Count > 0)
            {
                var cnt = 0;

                while (cnt < lines.Count)
                {
                    var needAdd = cnt >= iconObj.connectLineList.Count;
                    var line = needAdd ? UnityEngine.Object.Instantiate(relatedObject.connectLineList[0], relatedObject.connectLineList[0].transform.parent) : iconObj.connectLineList[cnt];
                    line.name = $"LoAStory_{modIcon.packageId}_{modIcon.artwork}_Line_{cnt}";
                    line.transform.localPosition = relatedObject.connectLineList[0].transform.localPosition + lines[cnt].position;
                    line.transform.localRotation = lines[cnt].rotation;
                    line.transform.localScale = lines[cnt].scale;
                    if (needAdd) iconObj.connectLineList.Add(line);
                    cnt++;
                }
            }
            iconObj.transform.localPosition += new Vector3(modIcon.position.x, modIcon.position.y);
            return iconObj;
        }

        private void InitTimeline(UIStoryProgressPanel root, TimelineData data)
        {
            var infos = new List<UIStoryProgressIconSlot>();
            int cnt = 0;
            foreach (var d in data.infos)
            {
                var icon = CreateSlot(root, d);
                root.iconList.Add(icon);
                StoryPatch.AddStoryIcon(icon, d);
                if (cnt == 0)
                {
                    icon.SetSlotData(new List<StageClassInfo>());
                    icon.SetActiveStory(true);
                    var observer = icon.closeRect.gameObject.AddComponent<LoAStoryObserver>();
                    observer.root = root;
                    observer.slot = icon;
                    observer.matchedTimeline = data;
                    observer.onTimelineConflict = CheckTimelineConflict;
                    observer.onDisable = CheckStoryDisableFromOtherMod;
                    cnt++;
                }
                infos.Add(icon);
            }
            data.slots[root] = infos;
        }

        private void CheckTimelineConflict(UIStoryProgressPanel root)
        {
            // if (!(CurrentTimeline is null)) return;
            if (root is null) return;

            foreach (var d in datas.Values)
            {
                if (!d.slots.ContainsKey(root)) continue;
                foreach (var c in d.slots[root])
                {
                    c.SetActiveStory(false);
                }
            }
        }

        private void CheckStoryDisableFromOtherMod(UIStoryProgressPanel root)
        {
            if (CurrentTimeline is null || returnButton is null || !root.gameObject.activeSelf || isChanging) return;
            if (!LoATimelineObserver.Instances[root].gameObject.activeSelf) return;
            Logger.Log("StoryIcon Disabled Without Return Button ... Maybe Other Mod Interaction?");
            returnButton.gameObject.SetActive(false);
            CurrentTimeline = null;
            TimelineConflict.Instance.ToggleOtherTimelineEnterVisible(root, true);
        }

        private void UpdateReturnButton(UIStoryProgressPanel panel, TimelineData data)
        {
            if (returnButton is null)
            {
                Logger.Log("Return Button Create");
                var obj = LoAFramework.BattleUiBundle.LoadAsset<GameObject>("Assets/CustomSelector/LoATimelineReturnButton.prefab");
                returnButton = UnityEngine.Object.Instantiate(obj, panel.scroll_viewPort.content).GetComponent<Button>();
                returnButton.onClick.AddListener(() =>
                {
                    var origin = returnButton.GetComponentInParent<UIStoryProgressPanel>();
                    origin.SetStoryLine();
                });
            }

            returnButton.transform.localScale = Vector3.one;
            returnButton.gameObject.SetActive(true);
            var slots = data.slots[panel];
            UIStoryProgressIconSlot leftObj = null;
            Vector3 p = Vector3.zero;
            for (int i = 0; i < slots.Count; i++)
            {
                var obj = slots[i].transform;
                var pos = obj.position;
                if (i == 0)
                {
                    leftObj = slots[i];
                    p = pos;
                }
                else if (p.x > pos.x)
                {
                    leftObj = slots[i];
                    p = pos;
                }
                else if (p.x == pos.x && p.y > pos.y)
                {
                    leftObj = slots[i];
                    p = pos;
                }
            }
            if (returnButton.transform.parent != leftObj.transform.parent)
            {
                returnButton.transform.SetParent(leftObj.transform.parent);
            }
            Vector2 originPos = leftObj.transform.localPosition;
            returnButton.transform.localPosition = originPos + data.model.returnButtonPosition;
        }
    }
}
