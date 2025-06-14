using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using LibraryOfAngela.Model;
using UnityEngine.UI;
using TMPro;
using LibraryOfAngela.Extension.Framework;
using HarmonyLib;
using System.Reflection.Emit;
using LOR_XML;
using StoryScene;
using Mod;
using System.IO;
using WorkParser;
using System.Collections;
using System.Reflection;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;

namespace LibraryOfAngela.Story
{
    class StoryPatch : Singleton<StoryPatch>
    {
        class ReplaceKeyInfo
        {
            public UIStoryProgressIconSlot origin;
            public UIStoryProgressIconSlot replace;
            public KeyCode targetCode;
        }

        private UIIconManager.IconSet empty;
        private Dictionary<UIStoryProgressIconSlot, CustomStoryIconInfo> slotInfo;
        private Dictionary<string, List<string>> storyTypes;
        private Dictionary<KeyCode, List<ReplaceKeyInfo>> replaceInfos;
        private HashSet<UIStoryProgressIconSlot> replaceableTargets;
        public Dictionary<LorId, Func<bool>> visibleConditions;
        public Dictionary<LorId, CustomStoryInfo> storyInfos = new Dictionary<LorId, CustomStoryInfo>(); 
        private List<UIStoryProgressPanel> iconAddedContainers;
        private Image ketherIconReplacer = null;
        private Image originKetherIcon = null;
        private StageStoryInfo currentStory;
        private Action<int> currentEventListener;
        private Func<int, bool> currentClickListener;
        private Func<int, IEnumerator> currentTransitionCoroutine;
        private bool isCustomTransitionHandle;

        public void Initialize()
        {
            slotInfo = new Dictionary<UIStoryProgressIconSlot, CustomStoryIconInfo>();
            visibleConditions = new Dictionary<LorId, Func<bool>>();
            storyTypes = new Dictionary<string, List<string>>();

            InternalExtension.SetRange(GetType());
            var method = typeof(StageClassInfoList).GetNestedTypes(AccessTools.all)
    .SelectMany(d => d.GetMethods(AccessTools.all))
    .First(c => c.Name.Contains("GetWorkshopDataFromBooks") && c.ReturnType == typeof(bool));

            method.DeclaringType.PatchInternal(method.Name, flag: PatchInternalFlag.POSTFIX, patchName: "HandleStoryVisible");
        }

        public static void AddStoryIcon(UIStoryProgressIconSlot slot, CustomStoryIconInfo info)
        {
            Instance.slotInfo[slot] = info;
            foreach (var d in info.stageIds)
            {
                Instance.storyInfos[new LorId(d.packageId, d.id)] = d;
            }
        }

        /// <summary>
        /// 스토리 접대 아이콘 생성
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(UIStoryProgressPanel), "SetStoryLine")]
        [HarmonyPostfix]
        private static void After_SetStoryLine(UIStoryProgressPanel __instance)
        {
            if (Instance.iconAddedContainers == null) Instance.iconAddedContainers = new List<UIStoryProgressPanel>();

            if (Instance.iconAddedContainers.Contains(__instance))
            {
                try
                {
                    /**
 *  TimelineUtil Conflict
 *  다시 들어왔을때 비활성화됬을 수 있다.
 */
                    var invitationPanel = __instance.invitationPanel;
                    foreach (var pair in Instance.slotInfo)
                    {
                        var icon = pair.Key;
                        if (icon.currentSelectedLevelIndex != -1)
                        {
                            // from OnPointerClickLevelIcon
                            var idx = icon.currentSelectedLevelIndex;
                            icon.currentSelectedLevelIndex = -1;
                            icon.StoryProgressPanel.SelectedStory(false, idx, false);
                            icon.selectedIndex = -1;
                            if (icon.openFrameTarget != null)
                            {
                                icon.openFrameTarget.SetActive(false);
                            }
                        }
                        icon.SetDefault();
                        if (Instance.replaceableTargets?.Contains(icon) == true)
                        {
                            if (icon.gameObject.activeSelf)
                            {
                                foreach (var t in Instance.replaceInfos.Values)
                                {
                                    var t2 = t.Find(d => d.replace == icon);
                                    if (t2 != null)
                                    {
                                        t2.origin.gameObject.SetActive(true);
                                        break;
                                    }
                                }
                            }
                            icon.SetSlotData(new List<StageClassInfo>());
                            icon.SetActiveStory(false);
                            continue;
                        }
                        if (icon.transform.parent.gameObject.activeSelf)
                        {
                            icon.SetActiveStory(icon.storyData[0].currentState != StoryState.Close);
                        }
                    }
                    var timeline = TimelinePatch.Instance.CurrentTimeline;
                    // 어떤 다른 방법으로 서고 - 초대장간 싱크가 안맞는 경우 마지막 선택으로 맞추기 위해 강제 초기화
                    TimelinePatch.Instance.ResetTimeline(__instance);
                    TimelinePatch.Instance.RestoreTimeline(__instance, timeline, false);
                }
                catch (Exception e)
                {
                    Logger.Log("Exception in Restore StoryLine");
                    Logger.LogError(e);
                }
                return;
            }

            __instance.scroll_viewPort.movementType = ScrollRect.MovementType.Unrestricted;
            try 
            {
                Instance.iconAddedContainers.Add(__instance);
                var allStorys = __instance.iconList;
                var originIconSlot = allStorys.Find(x => x.currentStory == UIStoryLine.Rats);
                foreach (var mod in LoAModCache.StoryConfigs)
                {
                    var icons = mod.GetStoryIcons();
                    if (icons is null) continue;

                    foreach (var modIcon in icons)
                    {
                        try
                        {
                            modIcon.packageId = mod.packageId;
                            FillModIcon(__instance, modIcon, allStorys, originIconSlot);
                        }
                        catch (Exception e)
                        {
                            Logger.Log($"Exception in Story Icon Init {mod.packageId}, {modIcon.artwork}");
                            Logger.LogError(e);
                        }

                    }
                }

                ReplaceInit(__instance);
                TimelinePatch.Instance.Init(__instance);
            }
            catch (Exception e)
            {
                Logger.Log("Exception in StoryIcon List Init");
                Logger.LogError(e);
            }
        }

        private static void FillModIcon(
            UIStoryProgressPanel __instance,
            CustomStoryIconInfo modIcon, 
            List<UIStoryProgressIconSlot> allStorys, 
            UIStoryProgressIconSlot originIconSlot
            )
        {
            // 바닐라가 아닌 타임라인은 여기서 다루지 않는다.
            if (!string.IsNullOrEmpty(modIcon.storyTimeline)) return;

            var relatedObject = allStorys.Find(x => x.currentStory == modIcon.relatedStoryLinePosition);
            if (relatedObject == null) relatedObject = originIconSlot;
            var packageId = modIcon.packageId;
            var iconObj = UnityEngine.Object.Instantiate(relatedObject.gameObject, relatedObject.transform.parent).GetComponent<UIStoryProgressIconSlot>();
            iconObj.connectLineList = new List<GameObject>();
            iconObj.isChapterIcon = false;
            iconObj.currentStory = UIStoryLine.HanaAssociation;
            iconObj.Initialized(__instance);
            iconObj.gameObject.name = $"LoAStory_{packageId}_{modIcon.artwork}";

            modIcon.stageIds.ForEach(x =>
            {
                x.packageId = packageId;
                Instance.visibleConditions[new LorId(packageId, x.id)] = x.visibleCondition;
            });
            AddStoryIcon(iconObj, modIcon);

            if (modIcon.replaceKeyCode != KeyCode.None)
            {
                iconObj.SetSlotData(new List<StageClassInfo>());
                iconObj.gameObject.SetActive(false);
                if (Instance.replaceInfos is null) Instance.replaceInfos = new Dictionary<KeyCode, List<ReplaceKeyInfo>>();
                if (Instance.replaceableTargets is null) Instance.replaceableTargets = new HashSet<UIStoryProgressIconSlot>();
                if (!Instance.replaceInfos.ContainsKey(modIcon.replaceKeyCode))
                {
                    Instance.replaceInfos[modIcon.replaceKeyCode] = new List<ReplaceKeyInfo>();
                }
                Instance.replaceableTargets.Add(iconObj);
                Instance.replaceInfos[modIcon.replaceKeyCode].Add(new ReplaceKeyInfo
                {
                    origin = relatedObject,
                    replace = iconObj,
                    targetCode = modIcon.replaceKeyCode
                });
            }
            else
            {
                var lines = FrameworkExtension.GetSafeAction(() => modIcon.lines);
                if (lines != null && lines.Count > 0)
                {
                    var cnt = 0;

                    while (cnt < lines.Count)
                    {
                        var needAdd = cnt >= iconObj.connectLineList.Count;
                        var line = needAdd ? UnityEngine.Object.Instantiate(relatedObject.connectLineList[0], relatedObject.connectLineList[0].transform.parent) : iconObj.connectLineList[cnt];
                        line.name = $"LoAStory_{packageId}_{modIcon.artwork}_Line_{cnt}";
                        line.transform.localPosition += lines[cnt].position;
                        line.transform.localRotation = lines[cnt].rotation;
                        line.transform.localScale = lines[cnt].scale;
                        if (needAdd) iconObj.connectLineList.Add(line);
                        cnt++;
                    }
                }
                iconObj.transform.localPosition += new Vector3(modIcon.position.x, modIcon.position.y);
                iconObj.SetSlotData(new List<StageClassInfo>());
                iconObj.SetActiveStory(iconObj.storyData[0].currentState != StoryState.Close);
                __instance?.iconList?.Add(iconObj);
            }
        }

        private static void ReplaceInit(UIStoryProgressPanel __instance)
        {
            var baseTimeline = __instance.scroll_viewPort.content.Find("Icons");
            var obj = baseTimeline.gameObject.AddComponent<LoATimelineObserver>();
            obj.panel = __instance;
            LoATimelineObserver.Instances[__instance] = obj;

            if (Instance.replaceInfos != null && Instance.replaceInfos.Count > 0)
            {
                Logger.Log("Replacable Story Detect, Inject");
                obj.isKeyDetectEnabled = true;
                obj.targetKeyCodes = Instance.replaceInfos.Keys.ToArray();
                obj.onPress = (key) =>
                {
                    foreach (var k in Instance.replaceInfos[key])
                    {
                        var origin = k.origin.gameObject.activeSelf;
                        var replace = k.replace.gameObject.activeSelf;
                        if (origin == replace)
                        {
                            // .... What?
                            continue;
                        }
                        if (origin)
                        {
                            k.origin.gameObject.SetActive(false);
                            k.replace.gameObject.SetActive(true);
                        }
                        else
                        {
                            k.origin.gameObject.SetActive(true);
                            k.replace.gameObject.SetActive(false);
                        }
                    }
                };
            }
        }

        // 다시 진입했을때 올바른 접대 정보 바인딩
        [HarmonyPatch(typeof(UIStoryProgressIconSlot), "SetSlotData")]
        [HarmonyPrefix]
        private static void Before_SetSlotData(UIStoryProgressIconSlot __instance, ref List<StageClassInfo> data)
        {
            var icon = Instance.slotInfo.SafeGet(__instance);
            if (icon == null) return;
            data = icon.stageIds.Select(x =>
            {
                return StageClassInfoList.Instance.GetData(new LorId(x.packageId, x.id));
            }).ToList();
        }

        private const string CG_NOT_EXIST = "CG_NOT_EXIST_LOA";

        /// <summary>
        /// 서고에서 이야기 클릭시 스프라이트 지정
        /// </summary>
        /// <param name="slot"></param>
        [HarmonyPatch(typeof(UIBattleStoryPanel), "SelectedStory")]
        [HarmonyPostfix]
        private static void After_SelectedStory(UIBattleStoryPanel __instance, UIStoryProgressIconSlot slot, int idx)
        {
            if (Instance.slotInfo.ContainsKey(slot))
            {
                var packageId = slot.storyData[0].workshopID;
                var info = Instance.slotInfo[slot].stageIds[idx];
                var art = info.storyArtwork;
                if (art == CG_NOT_EXIST) return;

                if (!string.IsNullOrEmpty(art))
                {
                    __instance.storyInfoPanel.CG.sprite = LoAModCache.Instance[packageId].Artworks[art];
                }
                else 
                {
                    var modPath = ModContentManager.Instance.GetModPath(info.packageId);
                    var filePath = Path.Combine(modPath, "Assemblies", "ClearCG", $"{info.id}.png");
                    if (File.Exists(filePath))
                    {
                        var sp = LoAArtworks.Instance.CreateFileSprite(filePath);
                        var key = $"LoA_Dynamic_Sprite_ClearCG_{info.id}";
                        LoAArtworks.Instance.InputSprite(info.packageId, key, sp);
                        info.storyArtwork = key;
                        __instance.storyInfoPanel.CG.sprite = sp;
                    }
                    else
                    {
                        info.storyArtwork = CG_NOT_EXIST;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UISpriteDataManager), "GetStoryIcon")]
        [HarmonyPostfix]
        private static void After_GetStoryIcon(string story, ref UIIconManager.IconSet __result, Dictionary<string, UIIconManager.IconSet> ___StoryIconDic)
        {
            if (Instance.empty == null)
            {
                Instance.empty = ___StoryIconDic["None"];
            }
            if (__result != Instance.empty) return;
            if (___StoryIconDic.ContainsKey(story)) return;

            foreach (var mod in LoAModCache.Instance.Where(x => x.StoryConfig != null))
            {
                var sprite = mod.Artworks.GetNullable(story);
                if (sprite != null)
                {
                    var glowSprite = mod.Artworks.GetNullable(story + "_glow") ?? sprite;
                    __result = new UIIconManager.IconSet
                    {
                        type = story,
                        icon = sprite,
                        iconGlow = glowSprite
                    };
                    ___StoryIconDic[story] = __result;
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(UIStoryProgressIconSlot), "OnPointerClickLevelIcon")]
        [HarmonyPrefix]
        private static void Before_OnPointerClickLevelIcon(int index, UIStoryProgressIconSlot __instance)
        {
            try
            {
                var idx = __instance.currentSelectedLevelIndex;
                // ...?
                if (index < 0)
                {
                    return;
                }
                // 그냥 눌렀다가 다시 때는 경우거나 처음이 다시 눌리는 경우, 문제 없음
                if (index == 0 && (index == idx  || idx == -1))
                {
                    return;
                }

                var info = Instance.slotInfo.SafeGet(__instance);
                if (info is null)
                {
                    return;
                }
                var artworkBefore = idx < 0 ? "" : __instance.storyData[idx].storyType;
                var artworkAfter = __instance.storyData[index].storyType;
                if (artworkBefore != artworkAfter && !string.IsNullOrEmpty(artworkAfter))
                {
                    __instance.SetIcon(UISpriteDataManager.instance.GetStoryIcon(artworkAfter));
                    __instance.SetSlotOpen(true);

                    return;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // 조건 비만족시 책장 조건이 맞아도 스토리가 보이지 않게 한다.
        private static void After_HandleStoryVisible(ref bool __result, StageClassInfo infoList)
        {
            try
            {
                if (__result == false) return;
                if (Instance.visibleConditions.SafeGet(infoList.id)?.Invoke() == false)
                {
                    __result = false;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(StageClassInfo), "get_currentState")]
        [HarmonyPostfix]
        private static void After_get_currentState(ref StoryState __result, StageClassInfo __instance)
        {
            if (__result == StoryState.Close) return;
            var condition = Instance.visibleConditions.SafeGet(__instance.id)?.Invoke() ?? true;
            if (condition) return;
            else __result = StoryState.Close;
        }

        [HarmonyPatch(typeof(UIInvitationRightMainPanel), "GetBookRecipe")]
        [HarmonyPostfix]
        private static void After_GetBookRecipe(UIInvitationRightMainPanel __instance, ref StageClassInfo __result)
        {
            var currentSelectedStage = __instance.invPanel.CurrentStage;
            if (currentSelectedStage is null || string.IsNullOrEmpty(currentSelectedStage.workshopID)) return;
            if (LoAModCache.Instance[currentSelectedStage.workshopID]?.StoryConfig is null) return;
            // 선택후 취소했을때 이상하게 초대장이 남는거 수정
            if (__instance.invPanel.currentSelectedStorySlot is null) return;
            __result = currentSelectedStage;
        }

        /// <summary>
        /// 접대 
        /// </summary>
        /// <param name="storySlot"></param>
        /// <param name="idx"></param>
        [HarmonyPatch(typeof(UIInvitationPanel), "SetApplyState")]
        [HarmonyPostfix]
        private static void After_SetApplyState(UIStoryProgressIconSlot storySlot, int idx, 
            UIInvitationInfoPanel ____invLeftInfoPanel, UIInvitationRightMainPanel ____invRightMainPanel)
        {
            var info = Instance.slotInfo.SafeGet(storySlot);
            if (info == null || info.stageIds.Count - 1 < idx) return;
            var matchedInfo = info.stageIds[idx];
            if (!string.IsNullOrEmpty(matchedInfo.invitationArtwork))
            {
                ____invRightMainPanel.SetInvBookApplyState(InvitationApply_State.BlackSilence);
                ____invLeftInfoPanel.SetShowState(UIInvShowInfoState.BlackSilence, null, null);
            }
        }

        [HarmonyPatch(typeof(UIInvitationRightMainPanel), "SetInvBookApplyState")]
        [HarmonyPostfix]
        private static void After_SetInvBookApplyState(UIInvitationPanel ___invPanel, Image ___img_endcontents_content)
        {
            var info = Instance.slotInfo.SafeGet(___invPanel.currentSelectedStorySlot);
            if (info == null) return;
            var matchedInfo = info.stageIds[___invPanel.currentStoryidx];
            if (!string.IsNullOrEmpty(matchedInfo.invitationArtwork))
            {
                var packageId = ___invPanel.CurrentStage.id.packageId;
                var artwork = LoAModCache.Instance[packageId].Artworks[matchedInfo.invitationArtwork];
                if (artwork != null) ___img_endcontents_content.sprite = artwork;
            }
        }

        private static bool isBgFgFixed = false;

        [HarmonyPatch(typeof(UI.UIController), "OpenStory", new Type[] { typeof(StageStoryInfo), typeof(StoryRoot.OnEndStoryFunc), typeof(bool), typeof(bool), typeof(bool) })]
        [HarmonyPostfix]
        private static void After_OpenStory_UI(StageStoryInfo storyInfo)
        {
            if (storyInfo?.packageId is null) return;
            var config = LoAModCache.Instance[storyInfo.packageId]?.StoryConfig;
            if (config != null)
            {
                var com = StoryRoot.Instance.GetComponentInChildren<CamFilterController>(true);
                if (com != null && CamFilterController._instance != com)
                {
                    Logger.Log("CamFilter Refresh in Root");
                    CamFilterController._instance = com;
                }
            }
        }

        [HarmonyPatch(typeof(BattleStoryUI), "OpenStory")]
        [HarmonyPostfix]
        private static void After_OpenStory(BattleStoryUI __instance)
        {
            var packageId = Instance.currentStory?.packageId;
            if (packageId != null && LoAModCache.Instance[packageId]?.StoryConfig != null)
            {
                var com = BattleManagerUI.Instance.ui_battleStory.GetComponentInChildren<CamFilterController>(true);
                if (com != null && CamFilterController._instance != com)
                {
                    Logger.Log("CamFilter Refresh in UI");
                    CamFilterController._instance = com;
                }
            }
            if (isBgFgFixed)
            {
                return;
            }
            Logger.Log("Battle Story Bg Fg Fix");
            isBgFgFixed = true;
            var bg = __instance.storyUI.transform.Find("Canvas_Background").GetComponent<Canvas>();
            var fg = __instance.storyUI.transform.Find("Canvas_Forground").GetComponent<Canvas>();
            fg.worldCamera = bg.worldCamera;
        }

        [HarmonyPatch(typeof(UIAlarmPopup), "SetAlarmTextForBlue")]
        [HarmonyPostfix]
        private static void After_SetAlarmTextForBlue(UIAlarmType alarmtype, TextMeshProUGUI ___txt_alarmForBlue)
        {
            if (alarmtype != UIAlarmType.StartBlackSilence) return;
            var invitationPanel = UI.UIController.Instance.GetUIPanel(UIPanelType.Invitation) as UIInvitationPanel;
            var info = Instance.slotInfo.SafeGet(invitationPanel.currentSelectedStorySlot);
            if (info == null) return;
            var matchedInfo = info.stageIds[invitationPanel.currentStoryidx];
            ___txt_alarmForBlue.text = matchedInfo.invitationMessage;
        }

        [HarmonyPatch(typeof(UIAlarmPopup), "SetTypeCloseFunc")]
        [HarmonyPrefix]
        private static bool Before_SetTypeCloseFunc(UIAlarmType ___currentAlarmType)
        {
            if (___currentAlarmType != UIAlarmType.StartBlackSilence) return true;

            var invitationPanel = UI.UIController.Instance.GetUIPanel(UIPanelType.Invitation) as UIInvitationPanel;
            var info = Instance.slotInfo.SafeGet(invitationPanel.currentSelectedStorySlot);
            if (info == null) return true;
            invitationPanel.currentSelectedStorySlot.SetDefault();
            UI.UIController.Instance.PrepareBattle(invitationPanel.CurrentStage, new List<DropBookXmlInfo>());
            return false;
        }
    
        [HarmonyPatch(typeof(StorySerializer), "LoadStageStory")]
        [HarmonyPostfix]
        private static void After_LoadStageStory(ref bool __result, StageStoryInfo stageStoryInfo)
        {
            Instance.currentEventListener = null;
            Instance.currentStory = stageStoryInfo;
            var m = LoAModCache.StoryConfigs.FirstOrDefault(x => x.packageId == stageStoryInfo.packageId);

            if (m != null)
            {
                string story = stageStoryInfo.story;

                if (LoAFramework.DEBUG)
                {
                    Logger.Log($"LoadStageStory :: ({stageStoryInfo.packageId} in {story})");
                }

                while (true)
                {
                    var handle = FrameworkExtension.GetSafeAction(() => m.HandleStoryOpen(story));
                    story = handle?.replaceStory ?? story;
                    Instance.currentEventListener = FrameworkExtension.GetSafeAction(() => handle?.onEvent);
                    Instance.currentClickListener = FrameworkExtension.GetSafeAction(() => handle?.onClick);
                    Instance.currentTransitionCoroutine = handle?.onTransitionCoroutine;
                    if (handle?.replaceStory is null) break;
                    else
                    {
                        if (LoAFramework.DEBUG)
                        {
                            Logger.Log($"Story Replaced :: ({stageStoryInfo.packageId} in {handle?.replaceStory})");
                        }
                        Instance.currentStory = new StageStoryInfo
                        {
                            cond = Instance.currentStory.cond,
                            chapter = Instance.currentStory.chapter,
                            packageId = Instance.currentStory.packageId,
                            story = story
                        };
                    }
                }

                var mod = LoAModCache.Instance[stageStoryInfo.packageId]?.mod as ILoALocalizeMod;
                if (mod != null)
                {
                    var langauge = GlobalGameManager.Instance.CurrentOption.language;
                    var basePath = ModContentManager.Instance.GetModPath(mod.packageId);
     
                    var ext = story.EndsWith(".xml") ? "" : ".xml";
                    var targetStory = Path.Combine(basePath, "Data", "StoryText", $"{langauge}_{story}{ext}");
                    var targetEffect = Path.Combine(basePath, "Data", "StoryEffect", $"{langauge}_{story}{ext}");
                    if (File.Exists(targetStory))
                    {
                        __result = StorySerializer.LoadStoryFile(targetStory, targetEffect, basePath);
                    }
                }
                if (__result)
                {
                    try
                    {
                        m.OnStoryLoaded(stageStoryInfo);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        [HarmonyPatch(typeof(StorySerializer), "HasEffectFile")]
        [HarmonyPostfix]
        private static void After_HasEffectFile(ref bool __result, StageStoryInfo stageStoryInfo)
        {
            if (__result) return;
            var mod = LoAModCache.Instance[stageStoryInfo.packageId]?.mod as ILoALocalizeMod;
            if (mod is null) return;
            var langauge = GlobalGameManager.Instance.CurrentOption.language;
            var basePath = ModContentManager.Instance.GetModPath(mod.packageId);
            string story = stageStoryInfo.story;

            var ext = story.EndsWith(".xml") ? "" : ".xml";
            var targetStory = Path.Combine(basePath, "Data", "StoryText", $"{langauge}_{story}{ext}");
            var targetEffect = Path.Combine(basePath, "Data", "StoryEffect", $"{langauge}_{story}{ext}");
            __result = File.Exists(targetStory);
        }

        [HarmonyPatch(typeof(StoryManager), "ChangeVisual")]
        [HarmonyPostfix]
        private static void After_ChangeVisual(Dialog d) 
        {
            try
            {
                Instance.currentEventListener?.Invoke(d.ID);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(StoryManager), "CanAnim")]
        [HarmonyPrefix]
        private static bool Before_CanAnim(Dialog d, ref bool __result, StoryManager __instance) 
        {
            try
            {
                if (Instance.isCustomTransitionHandle)
                {
                    __result = true;
                    return false;
                }

                var coroutine = Instance.currentTransitionCoroutine?.Invoke(d.ID);
                if (LoAFramework.DEBUG) Logger.Log($"Anim Change Check : {d.ID} // {coroutine != null}");
                if (coroutine != null)
                {
                    GameSceneManager.Instance.StartCoroutine(StartHandleCoroutine(coroutine, __instance));
                    __result = true;
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return true;
            }
        }

        private static IEnumerator StartHandleCoroutine(IEnumerator def, StoryManager __instance) {
            Logger.Log("Anim Start");
            Instance.isCustomTransitionHandle = true;
            yield return GameSceneManager.Instance.StartCoroutine(def);
            Instance.isCustomTransitionHandle = false;
            Logger.Log("Anim End");
            __instance.ChangeBgSpriteEvent();
        }

        [HarmonyPatch(typeof(StoryManager), "ClickEvent")]
        [HarmonyPrefix]
        private static void Before_ClickEvent(StoryManager __instance, out bool __state)
        {
            if (Instance.currentClickListener is null)
            {
                __state = false;
                return;
            }
            try
            {
                int lastId = __instance.dialogLogManager.dialogDataList.LastOrDefault()?.dialog?.ID ?? -1;
                var stop = Instance.currentClickListener.Invoke(lastId);
                if (stop)
                {
                    __state = true;
                    __instance.blockClick = true;
                }
                else
                {
                    __state = false;
                }
            }
            catch (InvalidOperationException)
            {
                __state = false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                __state = false;
            }
        }

        [HarmonyPatch(typeof(StoryManager), "ClickEvent")]
        [HarmonyPostfix]
        private static void After_ClickEvent(StoryManager __instance, bool __state)
        {
            if (__state)
            {
                __instance.blockClick = false;
            }
        }

        [HarmonyPatch(typeof(DialogLogManager), "Init")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_Init(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                yield return code;
                if (code.opcode == OpCodes.Call)
                {
                    var method = code.operand as MethodInfo;
                    if (method?.DeclaringType == typeof(Resources) && method?.Name == "Load")
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoryPatch), nameof(HandleModPortrait)));
                    }
                }
            }
        }

        private static Sprite HandleModPortrait(Sprite origin, Dialog target)
        {
            if (string.IsNullOrEmpty(Instance.currentStory?.packageId)) return origin;
            var mod = LoAModCache.Instance[Instance.currentStory.packageId];
            var config = mod?.StoryConfig;
            if (config is null) return origin;
            try
            {
                var name = config.GetStoryStandingCharacterName(target.Model);
                if (string.IsNullOrEmpty(name)) return origin;
                var portrait = config.GetStoryStandingPortrait(name);
                if (string.IsNullOrEmpty(portrait)) return origin;
                var image = mod.Artworks?.GetNullable(portrait);
                if (image is null) return origin;
                return image;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return origin;
            }

        }

        [HarmonyPatch(typeof(StageController), "CheckStoryBeforeBattle")]
        [HarmonyPostfix]
        private static void After_CheckStoryBeforeBattle(ref bool __result)
        {
            if (__result) return;
            var currentStage = StageController.Instance.GetStageModel().ClassInfo;
            var wave = StageController.Instance.CurrentWave;
            var mod = LoAModCache.StoryConfigs.FirstOrDefault(x => x.packageId == currentStage.workshopID);
            var story = FrameworkExtension.GetSafeAction(() => mod?.GetWaveStartStory(currentStage, wave));
            
            if (string.IsNullOrEmpty(story)) return;
            Logger.Log($"CheckStoryBeforeBattle : -> {currentStage.id} in " + story);
            StorySerializer.LoadStageStory(new StageStoryInfo
            {
                packageId = currentStage.workshopID,
                story = story,
                valid = true
            });
            SingletonBehavior<BattleManagerUI>.Instance.ui_battleStory.OpenStory(delegate
            {
                Logger.Log($"CheckStoryBeforeBattle End : -> {currentStage.id}");
                StageController.Instance.UpdatePhase(StageController.StagePhase.RoundStartPhase_UI);
            }, false, true);
            __result = true;
        }

        [HarmonyPatch(typeof(StageController), "CheckStoryAfterBattle")]
        [HarmonyPrefix]
        private static bool Before_CheckStoryAfterBattle(bool isWin)
        {
            if (!isWin) return true;

            var currentStage = StageController.Instance.GetStageModel().ClassInfo;
            var wave = StageController.Instance.CurrentWave;
            var mod = LoAModCache.StoryConfigs.FirstOrDefault(x => x.packageId == currentStage.workshopID);
            var story = FrameworkExtension.GetSafeAction(() => mod?.GetWaveEndStory(currentStage, wave));
            if (string.IsNullOrEmpty(story)) return true;
            StageStoryInfo storyInfo = new StageStoryInfo
            {
                packageId = currentStage.workshopID,
                story = story,
                valid = true
            };
            StorySerializer.LoadStageStory(storyInfo);
            SingletonBehavior<BattleSoundManager>.Instance.EndBgm();
            UI.UIController.Instance.OpenStory(storyInfo, delegate
            {
                GameSceneManager.Instance.ActivateBattleScene();
                StageController.Instance.CloseBattleScene();
            }, true);
            return false;
        }

        [HarmonyPatch(typeof(CamFilterController), "PillarOfLightRoutine")]
        [HarmonyPostfix]
        private static void After_PillarOfLightRoutine()
        {
            var layer = LayerMask.NameToLayer("Story");
            if (CamFilterController.Instance.pillaroflightParticleObj.layer != layer)
            {
                CamFilterController.Instance.pillaroflightParticleObj.layer = layer;
            }
        }

        [HarmonyPatch(typeof(CamFilterController), "BloomAndPillarOfLightRoutine")]
        [HarmonyPostfix]
        private static void After_BloomAndPillarOfLightRoutine()
        {
            var layer = LayerMask.NameToLayer("Story");
            if (CamFilterController.Instance.pillaroflightParticleObj.layer != layer)
            {
                CamFilterController.Instance.pillaroflightParticleObj.layer = layer;
            }
        }

        /// <summary>
        /// 1.
        /// this.waveList.SetData(stageModel);
        /// ->
        /// StoryPatch.CallBattlePrepare();
        /// this.waveList.SetData(stageModel);
        /// 
        /// 2.
        /// if (LibraryModel.Instance.GetEndContentState() == UIEndContentsState.KeterCompleteOpen)
        /// 
        /// ->
        /// 
        /// if (StoryPatch.WrappingKetherRealizationState(LibraryModel.Instance.GetEndContentState()) == UIEndContentsState.KeterCompleteOpen)
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UIBattleSettingPanel), "OnUIPhaseEnter")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_OnUIPhaseEnter(IEnumerable<CodeInstruction> instructions)
        {
            int i = 0;
            var codes = new List<CodeInstruction>(instructions);
            var getStageModel = AccessTools.Method(typeof(StageController), "GetStageModel");
            var endContent = AccessTools.Method(typeof(LibraryModel), "GetEndContentState");
            var wrapping = AccessTools.Method(typeof(StoryPatch), "WrappingKetherRealizationState");
            var fire1 = false;
            var fire2 = false;
            while (true)
            {
                if (i >= codes.Count) break;

                var code = codes[i];
                if (!fire1 && code.opcode == OpCodes.Ldarg_0)
                {
                    fire1 = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoryPatch), nameof(CallBattlePrepare)));
                }

                yield return code;
                if (!fire2 && code.Is(OpCodes.Callvirt, getStageModel))
                {
                    fire2 = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoryPatch), nameof(HandleStageRoute)));
                }
                if (code.Is(OpCodes.Callvirt, endContent))
                {
                    yield return new CodeInstruction(OpCodes.Call, wrapping);
                }
                i++;
            }
        }

        private static BattleStoryUI.OnEndStoryFunc HandleStoryEndUI(BattleStoryUI.OnEndStoryFunc origin, BattleStoryUI instance)
        {
            try
            {
                var next = IsHandledStory(instance.storyManager, instance.storyUI, instance.storyCamera);
                if (next != null)
                {
                    if (LoAFramework.DEBUG)
                    {
                        Logger.Log($"Story Handled in Battle : {next}");
                    }
                    return new BattleStoryUI.OnEndStoryFunc(next);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            if (LoAFramework.DEBUG)
            {
                Logger.Log($"Story Handled Skip in Battle");
            }
            return origin;
     
        }

        private static StoryRoot.OnEndStoryFunc HandleStoryEndRoot(StoryRoot.OnEndStoryFunc origin, StoryRoot instance)
        {
            try
            {
                var next = IsHandledStory(instance.storyManager, instance.storyUI, null);
                if (next != null)
                {
                    return new StoryRoot.OnEndStoryFunc(next);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return origin;
        }

        private static Action IsHandledStory(StoryManager manager, GameObject storyUI, Camera camera)
        {
            var packageId = Instance.currentStory?.packageId;
            if (packageId is null) return null;
            var end = FrameworkExtension.GetSafeAction(() => LoAModCache.Instance[packageId]?.StoryConfig?.HandleStoryEnd(Instance.currentStory.story, StoryManager.Instance.isJustRead));
            if (end is LoAStoryEnd.Next e)
            {
                var story = new StageStoryInfo
                {
                    packageId = packageId,
                    story = e.story,
                    chapter = 8,
                    cond = StageStoryCond.Start,
                    valid = true
                };
                if (GameSceneManager.Instance.battleScene.gameObject.activeSelf)
                {
                    return () =>
                    {
                        StorySerializer.LoadStageStory(story);
                        SingletonBehavior<BattleManagerUI>.Instance.ui_battleStory.OpenStory(delegate
                        {
                            e.onEnd();
                        }, false, true);
                    };
                }
                else
                {
                    return () =>
                    {
                        UI.UIController.Instance.OpenStory(story, () =>
                        {
                            e.onEnd();
                        }, true);
                    };
                }
            }
            else if (end is LoAStoryEnd.Branch b)
            {
                return () =>
                {
                    var acceptText = b.firstBranchText;
                    var deninedText = b.secondBranchText;
                    // 가속 켜져있으면 클릭이 안됨
                    manager._fastToggleOn = false;
                    manager._fastButtonDown = false;
                    if (!storyUI.activeSelf)
                    {
                        storyUI.SetActive(true);
                    }
                    if (camera != null)
                    {
                        camera.enabled = true;
                    }
                    manager.storySelectOptionPopup.Open();
                    manager.storySelectOptionPopup.selectOptionSlots[0].SetData(acceptText, true, () =>
                    {
                        UIControlManager.Instance.SelectSelectableForcely(StoryManager.Instance.storySelectOptionPopup.dummySelectable, false);
                        b.onSelect(0);
                    }, true);
                    manager.storySelectOptionPopup.selectOptionSlots[1].SetData(deninedText, true, () =>
                    {
                        UIControlManager.Instance.SelectSelectableForcely(StoryManager.Instance.storySelectOptionPopup.dummySelectable, false);
                        b.onSelect(1);
                    }, true);
                };
            }
            return null;
        }

        private static void CallBattlePrepare()
        {
            StageModel stageModel = Singleton<StageController>.Instance.GetStageModel();
            var floor = Singleton<StageController>.Instance.GetCurrentStageFloorModel();
            var id = stageModel.ClassInfo.id;

            try
            {
                foreach (var x in LoAModCache.StoryConfigs)
                {
                    try
                    {
                        if (id.packageId == x.packageId)
                        {
                            x.BattlePrepare(id, floor, Singleton<StageController>.Instance.CurrentWave);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static StageModel HandleStageRoute(StageModel origin)
        {
            var id = origin.ClassInfo.id;
            var result = origin;
            try
            {
                foreach (var x in LoAModCache.StoryConfigs)
                {
                    try
                    {
                        if (id.packageId == x.packageId)
                        {
                            StageClassInfo ret = x.HandleStageRoute(id, StageController.Instance.CurrentWave);
                            if (ret != null && ret != result.ClassInfo)
                            {
                                StageController.Instance.InitCommon(ret, false);
                                result = StageController.Instance.GetStageModel();
                                var enemyList = UI.UIController.Instance.GetUIPanel(UIPanelType.CharacterList) as UIEnemyCharacterListPanel;
                                enemyList.OnUpdatePhase();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return result;
        }

        private static UIEndContentsState WrappingKetherRealizationState(UIEndContentsState originState)
        {
            if (originState == UIEndContentsState.KeterCompleteOpen) return originState;
            var currentStageId = Singleton<StageController>.Instance.GetStageModel().ClassInfo.id;
            var wave = StageController.Instance.CurrentWave;
            var mod = LoAModCache.StoryConfigs.FirstOrDefault(x => x.packageId == currentStageId.packageId);
            if (mod == null) return originState;
            var matchedStage = mod.GetStoryIcons()
                .SelectMany(x => x.stageIds)
                .FirstOrDefault(x => x.id == currentStageId.id && x.settingInfos?.Any(d => d.wave == wave) == true);
            if (matchedStage == null) return originState;

            return UIEndContentsState.KeterCompleteOpen;
        }

        private static StoryBranch WrappingStoryBranch(StoryBranch branch)
        {
            if (branch == StoryBranch.None)
            {
                var packageId = Instance.currentStory?.packageId;
                if (string.IsNullOrEmpty(packageId)) return branch;
                var end = FrameworkExtension.GetSafeAction(() => LoAModCache.Instance[packageId].StoryConfig?.HandleStoryEnd(Instance.currentStory.story, StoryManager.Instance.isJustRead));
                if (end is null) return branch;

                return StoryBranch.Angela_KillorForgive;
            }

            return branch;
        }

        /// <summary>
        /// 위 <see cref="WrappingKetherRealizationState"/> 을 통해 임의의 모드가 총류 완전 개방의 형태로 보여줄때
        /// 케테르 아이콘을 현재 접대의 아이콘으로 교체한다.
        /// </summary>
        /// <param name="iscompleteopen"></param>
        [HarmonyPatch(typeof(UIBattleSettingPanel), "ChangeFrameByKeterCompleteOpen")]
        [HarmonyPostfix]
        private static void After_ChangeFrameByKeterCompleteOpen(bool iscompleteopen)
        {
            if (!iscompleteopen) return;
            if (Instance.originKetherIcon == null)
            {
                Instance.originKetherIcon = GameObject.Find("[Image]KeterIcon")?.GetComponent<Image>();
            }
            if (Instance.originKetherIcon == null) return;

            var currentStage = Singleton<StageController>.Instance.GetCurrentWaveModel()?.team?.stage ??
                StageController.Instance.GetStageModel()?.ClassInfo;

            var mod = LoAModCache.StoryConfigs.FirstOrDefault(x => x.packageId == currentStage.id.packageId);
            if (mod == null)
            {
                if (Instance.ketherIconReplacer != null && Instance.ketherIconReplacer.gameObject.activeSelf)
                {
                    Instance.ketherIconReplacer.gameObject.SetActive(false);
                    Instance.originKetherIcon.gameObject.SetActive(true);
                }
                return;
            }
            if (Instance.ketherIconReplacer == null)
            {
                Instance.ketherIconReplacer = UnityEngine.Object.Instantiate(Instance.originKetherIcon, Instance.originKetherIcon.transform.parent);
            }

            var matchedStage = mod.GetStoryIcons()
                .FirstOrDefault(x => x.stageIds.Any(d => d.id == currentStage.id.id));
            if (matchedStage == null) return;
            var sprite = UISpriteDataManager.instance.GetStoryIcon(currentStage._storyType);
            Instance.ketherIconReplacer.sprite = sprite.icon;
            Instance.ketherIconReplacer.gameObject.SetActive(true);
            Instance.originKetherIcon.gameObject.SetActive(false);
        }

        /*
        /// <summary>
        /// this.effectDefinition.storyBranch != StoryBranch.None 를
        /// WrappingStoryBranch(this.effectDefinition.storyBranch != StoryBranch.None)로 래핑한다.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        private static IEnumerable<CodeInstruction> Trans_EndStory(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var storyBranch = AccessTools.Field(typeof(SceneEffect), "storyBranch");
            foreach(var code in codes)
            {
                yield return code;
                if (code.Is(OpCodes.Ldfld, storyBranch))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoryPatch), "WrappingStoryBranch"));
                }
            }
        }*/
        [HarmonyPatch(typeof(BattleStoryUI), "EndStory")]
        [HarmonyPatch(typeof(StoryRoot), "EndStory")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_EndStory(IEnumerable<CodeInstruction> instructions)
        {
            var field1 = AccessTools.Field(typeof(BattleStoryUI), "_onEndStoryFunc");
            var field2 = AccessTools.Field(typeof(StoryRoot), "_onEndStoryFunc");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Ldfld, field1))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoryPatch), nameof(HandleStoryEndUI)));
                }
                else if (code.Is(OpCodes.Ldfld, field2))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StoryPatch), nameof(HandleStoryEndRoot)));
                }
            }
        }

    }
}
