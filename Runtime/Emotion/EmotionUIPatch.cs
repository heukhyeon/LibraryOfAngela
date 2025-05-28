using HarmonyLib;
using LibraryOfAngela.Battle;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using LOR_XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;

namespace LibraryOfAngela.Emotion
{
    class EmotionUIPatch : Singleton<EmotionUIPatch>
    {
        private Dictionary<LorId, Sprite> emotionImageCache = new Dictionary<LorId, Sprite>();
        private EmotionPassiveCardUI fourthCard;
        private HashSet<EmotionPassiveCardUI> appliedCards = new HashSet<EmotionPassiveCardUI>();

        public void Initialize()
        {
            InternalExtension.SetRange(GetType());
        }

        /// <summary>
        /// AbnormalityCardDescXmlList
        /// 환상체 정보 주입 혹은 바꿔치기
        /// </summary>
        /// <param name="cardID"></param>
        /// <param name="__result"></param>
        /// <param name="____dictionary"></param>
        [HarmonyPatch(typeof(AbnormalityCardDescXmlList), "GetAbnormalityCard")]
        [HarmonyPostfix]
        private static void After_GetAbnormalityCard(string cardID, ref AbnormalityCard __result, Dictionary<string, AbnormalityCard> ____dictionary)
        {
            var exists = ____dictionary.SafeGet(cardID);
            var item = LoAEmotionDictionary.Instance.cardIdAbnormalityCardDictionary.SafeGet(cardID);

            if (item is null) return;

            if (exists is null)
            {
                ____dictionary.Add(cardID, item.card);
                __result = item.card;
            }
            else if (LoAModCache.Instance[item.packageId].EmotionConfig.IsOverrideDesc(cardID, exists, item.card))
            {
                __result = item.card;
            }
        }

        /// <summary>
        /// 전투 이전 층의 전체 목록에서 총 환상체 책장 목록 수를 제어한다.
        /// </summary>
        /// <param name="floor"></param>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(UIAbnormalityPanel), "SetData")]
        [HarmonyPrefix]
        public static void Before_UIAbnormalityPanel_SetData(LibraryFloorModel floor, UIAbnormalityPanel __instance)
        {
            int cnt = __instance.AbCategoryPanel.Count;
            int refCnt = cnt;
            foreach (var config in LoAModCache.EmotionConfigs)
            {
                try
                {
                    config.HandleFloorAbnormalityCount(floor.Sephirah, ref refCnt);
                }
                catch (Exception)
                {

                }
            }
            if (cnt < refCnt)
            {
                while (cnt < refCnt)
                {
                    __instance.AbCategoryPanel.Add(UnityEngine.Object.Instantiate(__instance.AbCategoryPanel[0].gameObject, __instance.AbCategoryPanel[0].transform.parent).GetComponent<UIAbnormalityCategoryPanel>());
                    cnt++;
                }
            }
        }

        /// <summary>
        /// 층의 FloorLevel 은 SetData for문을 돌때 이상하게 계산되는 경향이 있다.
        /// 이걸 강제로 맞춰준다.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UIAbnormalityPanel), nameof(UIAbnormalityPanel.SetData))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetData(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                yield return code;

                if (code.opcode == OpCodes.Callvirt && code.operand is MethodInfo m && m.Name == "get_Level")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EmotionUIPatch), nameof(FixLevelCheck)));
                }
            }
        }

        public static int FixLevelCheck(int origin, LibraryFloorModel floor)
        {
            if (origin < floor.Level)
            {
                return floor.Level;
            }
            return origin;
        }

        /// <summary>
        /// 전투 이전 층의 에고 책장 목록 수를 제어한다.
        /// </summary>
        /// <param name="floor"></param>
        /// <param name="___slotList"></param>
        [HarmonyPatch(typeof(UIEgoCardPanel), "SetData")]
        [HarmonyPrefix]
        public static void Before_UIEgoCardPanel_SetData(LibraryFloorModel floor, List<UIEgoCardPreviewSlot> ___slotList)
        {
            int cnt = ___slotList.Count;
            int refCnt = cnt;
            foreach (var config in LoAModCache.EmotionConfigs)
            {
                try
                {
                    config.HandleFloorAbnormalityCount(floor.Sephirah, ref refCnt);
                }
                catch (Exception)
                {

                }
            }

            while (cnt < refCnt)
            {
                var target = ___slotList[0].gameObject;
                ___slotList.Add(UnityEngine.Object.Instantiate(target, target.transform.parent).GetComponent<UIEgoCardPreviewSlot>());
                cnt++;
            }
        }

        /// <summary>
        /// 전투 이전 층의 에고 책장 정보를 주입한다.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="cardModel"></param>
        [HarmonyPatch(typeof(UIEgoCardPreviewSlot), "Init")]
        [HarmonyPostfix]
        private static void After_EgoPreviewSlot_Init(UIEgoCardPreviewSlot __instance, DiceCardItemModel cardModel)
        {
            if (cardModel?.ClassInfo is null) return;

            if (LoAModCache.Instance[cardModel.ClassInfo.workshopID] != null)
            {
                __instance.cardName.text = cardModel.ClassInfo.workshopName;
                if (__instance.artwork.sprite == null)
                {
                    __instance.artwork.sprite = CustomizingCardArtworkLoader.Instance.GetSpecificArtworkSprite(cardModel.ClassInfo.workshopID, cardModel.GetArtworkSrc());
                }
            }
        }

        /// <summary>
        /// 모드 환상체 책장 선택시 다이얼로그가 제대로 출력되지 않는 부분을 수정한다.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(AbnormalityCardDescXmlList), "GetAbnormalityDialogue")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_GetAbnormalityDialogue(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(Dictionary<string, AbnormalityCard>), "ContainsKey");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EmotionUIPatch), "ConvertValidAbnormalityDialogue"));
                }
            }
        }

        private static bool ConvertValidAbnormalityDialogue(bool origin, string cardID)
        {
            if (origin) return origin;
            var target = LoAEmotionDictionary.Instance.cardIdAbnormalityCardDictionary.SafeGet(cardID);
            if (target?.card is null) return origin;
            AbnormalityCardDescXmlList.Instance._dictionary.Add(cardID, target.card);
            return true;
        }

        /// <summary>
        /// 모드 환상체 책장 스프라이트 로드
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(EmotionPassiveCardUI), "SetSprites")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetSprites(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo field = null;
            var target = typeof(Resources).GetMethods(AccessTools.all).First(x => x.IsGenericMethod && x.Name == "Load").MakeGenericMethod(typeof(Sprite));

            foreach (var code in instructions)
            {
                yield return code;
                if (field is null && code.opcode == OpCodes.Ldfld && code.operand is FieldInfo op && op.Name == "_card")
                {
                    field = op;
                }

                if (code.Is(OpCodes.Call, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EmotionUIPatch), "ConvertModSprite"));
                }
            }
        }

        /// <summary>
        /// 층의 환상체 책장 미리보기에서의 스프라이트 로드
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UIAbnormalityCardPreviewSlot), "Init")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_Init(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo field = null;
            var target = typeof(Resources).GetMethods(AccessTools.all).First(x => x.IsGenericMethod && x.Name == "Load").MakeGenericMethod(typeof(Sprite));
            foreach (var code in instructions)
            {
                yield return code;
                if (code.opcode == OpCodes.Ldfld && code.operand is FieldInfo op && op.Name == "Card")
                {
                    field = op;
                }
                if (code.Is(OpCodes.Call, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EmotionUIPatch), "ConvertModSprite"));
                }
            }
        }

        private static Sprite ConvertModSprite(Sprite origin, EmotionCardXmlInfo card)
        {
            if (origin is null && card is LoAEmotionInfo c)
            {
                var id = new LorId(c.packageId, c.id);
                var sp = Instance.emotionImageCache.SafeGet(id);
                if (!(sp is null)) return sp;

                var key = LoAEmotionDictionary.Instance.infoPackageIdDictionary.SafeGet(card);
                if (key is null) return origin;
                var artwork = card.Artwork;
                sp = CustomizingCardArtworkLoader.Instance.GetSpecificArtworkSprite(key, artwork.EndsWith(".png") ? artwork : $"{artwork}.png");
                if (sp is null)
                {
                    sp = LoAModCache.Instance[key]?.Artworks[card.Artwork];
                }
                if (!(sp is null))
                {
                    Instance.emotionImageCache[id] = sp;
                }
                return sp ?? origin;
            }
            return origin;
        }

        /// <summary>
        /// 환상체 책장 선택시 4번째 환상체 책장 출력
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="cardList"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(LevelUpUI), "Init")]
        [HarmonyPrefix]
        private static bool Before_Init(LevelUpUI __instance, List<EmotionCardXmlInfo> cardList)
        {
            if (Instance.fourthCard != null && cardList.Count < 4)
            {
                __instance.candidates = __instance.candidates.Where(x => x != Instance.fourthCard).ToArray();
                UnityEngine.Object.Destroy(Instance.fourthCard);
                Instance.fourthCard = null;
                foreach (var ui in __instance.candidates)
                {
                    ui.transform.localScale = Vector3.one;
                }
            }

            bool fourthCardFlag = false;
            if (__instance.candidates.Length < 4 && cardList.Count == 4)
            {
                fourthCardFlag = EmotionPatch.Instance.IsMySelecting() || cardList.Any(x => LoAEmotionDictionary.Instance.infoPackageIdDictionary.ContainsKey(x));
            }

            if (fourthCardFlag)
            {
                Instance.fourthCard = UnityEngine.Object.Instantiate(__instance.candidates[0].gameObject, __instance.candidates[0].transform.parent)
                    .GetComponent<EmotionPassiveCardUI>();
                Instance.fourthCard.gameObject.name = "LoAEmotionCard_4th";

                __instance.candidates = new EmotionPassiveCardUI[]
                {
                    __instance.candidates[0],
                    __instance.candidates[1],
                    __instance.candidates[2],
                    Instance.fourthCard
                };
                var rate = Vector3.one * (3f / 4);
                foreach (var ui in __instance.candidates)
                {
                    ui.transform.localScale = rate;
                }
            }

            return true;
        }


        // 환상체 책장 선택시 문구 변경
        [HarmonyPatch(typeof(LevelUpUI), "InitBase")]
        [HarmonyPostfix]
        private static void After_InitBase(LevelUpUI __instance)
        {
            string artwork = "";
            string title = "";
            int level = 0;
            bool flag = false;
            var panelInfo = EmotionPatch.Instance.currentPanelInfo;
            if (panelInfo != null)
            {
                artwork = panelInfo.artwork;
                title = panelInfo.title;
                level = panelInfo.level;
                flag = true;
            }
            else if (EgoPatch.instance.egoInfo != null)
            {
                artwork = EgoPatch.instance.egoInfo.artwork;
                title = EgoPatch.instance.egoInfo.title;
                level = EgoPatch.instance.egoInfo.level;
                flag = true;
            }

            if (!flag) return;

            var key = EmotionPatch.Instance.currentPanelInfo?.packageId;
            var icon = LoAModCache.Instance[key]?.Artworks?.GetNullable(artwork);
            if (icon is null)
            {
                icon = UISpriteDataManager.instance.GetStoryIcon(artwork)?.icon;
            }
            __instance.FloorIconImage.sprite = icon;
            __instance.ego_FloorIconImage.sprite = icon;
            __instance.txt_SelectDesc.text = title;
            __instance.txt_BtnSelectDesc.text = title;

            for (int i = 0; i < __instance._emotionLevels.Length; i++)
            {
                flag = i <= level;
                __instance._emotionLevels[i].Set(flag, i == level, flag);
            }
            __instance._curEmotionLvIconIdx = level;
        }

        /// <summary>
        /// 모드 환상체 책장 틴트 변경
        /// </summary>
        /// <param name="card"></param>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(EmotionPassiveCardUI), "Init")]
        [HarmonyPostfix]
        private static void After_EmotionCard_Init(EmotionCardXmlInfo card, EmotionPassiveCardUI __instance)
        {
            bool flag = false;
            if (card is LoAEmotionInfo info)
            {
                var config = LoAModCache.Instance[info.packageId].EmotionConfig;
                var uiConfig = config?.GetCustomEmotionConfig(card);
                if (uiConfig != null)
                {
                    var color = uiConfig.color;
                    var mod = LoAModCache.Instance[info.packageId];
                    __instance.img_LeftTotalFrame.sprite = mod.Artworks[uiConfig.artwork];
                    __instance._rightBg.sprite = mod.Artworks[uiConfig.detailArtwork ?? (uiConfig.artwork + "_Detail")];
                    __instance._rightFrame.enabled = false;
                    __instance._flavorText.fontMaterial.SetColor("_UnderlayColor", color);
                    __instance._abilityDesc.fontMaterial.SetColor("_UnderlayColor", color);
                    __instance._hOverImg.color = color;
                    TextMeshProMaterialSetter component = __instance.txt_Level.GetComponent<TextMeshProMaterialSetter>();
                    component.glowColor = color;
                    component.underlayColor = color;
                    component.enabled = false;
                    component.enabled = true;

                    if (!Instance.appliedCards.Contains(__instance))
                    {
                        Instance.appliedCards.Add(__instance);
                        __instance._leftFrameTitleLineardodge.gameObject.SetActive(false);
                    }
                    __instance._cardName.color = color;
                    __instance._cardName.fontMaterial.SetColor("_UnderlayColor", color);
                    __instance._rootImageBg.color = new Color(color.r, color.g, color.b, 0.25f);
                }
                else
                {
                    flag = true;
                }
            }
            else
            {
                flag = true;
                __instance._rightFrame.enabled = true;
            }

            if (flag && Instance.appliedCards.Contains(__instance))
            {
                __instance._leftFrameTitleLineardodge.gameObject.SetActive(true);
                __instance._cardName.color = new Color(0, 0, 0, 1);
                __instance._cardName.fontMaterial.SetColor("_UnderlayColor", new Color(0.9373f, 0.7608f, 0.5059f, 0.5f));
                Instance.appliedCards.Remove(__instance);

            }
        }

        [HarmonyPatch(typeof(UIEmotionPassiveCardInven), "SetSprites")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetSprites_CardInven(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo field = null;
            var target = typeof(Resources).GetMethods(AccessTools.all).First(x => x.IsGenericMethod && x.Name == "Load").MakeGenericMethod(typeof(Sprite));

            foreach (var code in instructions)
            {
                yield return code;
                if (field is null && code.opcode == OpCodes.Ldfld && code.operand is FieldInfo op && op.Name == "_card")
                {
                    field = op;
                }

                if (code.Is(OpCodes.Call, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EmotionUIPatch), "ConvertModSprite"));
                }
            }
        }
    }
}
