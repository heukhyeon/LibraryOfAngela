using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.BattleUI
{
    struct BattlePageTarget
    {
        public ILoAArtworkCache cache;
        public string key;
    }

    class BattlePagePatch : Singleton<BattlePagePatch>
    {
        private Dictionary<LorId, BattlePageTarget> iconDic = new Dictionary<LorId, BattlePageTarget>();
        private Dictionary<LorId, LorId[]> sharedLimitDic = new Dictionary<LorId, LorId[]>();
        private Dictionary<UIOriginCardSlot, BattlePageComponent> components = new Dictionary<UIOriginCardSlot, BattlePageComponent>();
        private Dictionary<BattleDiceCardUI, BattlePageComponent> components2 = new Dictionary<BattleDiceCardUI, BattlePageComponent>();

        internal Dictionary<LorId, CustomBattlePageHolder> customHolders = new Dictionary<LorId, CustomBattlePageHolder>();

        public void Initialize()
        {
            foreach(var mod in LoAModCache.Instance.Where(x => x.BattlePageConfig != null))
            {
                var dic = FrameworkExtension.GetSafeAction(() => mod.BattlePageConfig.CustomIcons);
                if (dic != null)
                {
                    foreach (var keyValue in dic)
                    {
                        iconDic[keyValue.Key] = new BattlePageTarget { cache = mod.Artworks, key = keyValue.Value };
                    }
                }

                var sharedId = FrameworkExtension.GetSafeAction(() => mod.BattlePageConfig.SharedIds);
                if (sharedId != null)
                {
                    foreach (var arr in sharedId)
                    {
                        foreach(var key in arr)
                        {
                            sharedLimitDic[key] = arr;
                        }
                    }
                }

                var holders = FrameworkExtension.GetSafeAction(() => mod.BattlePageConfig.CustomHolder);
                if (holders != null)
                {
                    foreach(var h in holders)
                    {
                        h.cache = mod.Artworks;
                        h.rangeCustom = h.rangeHsv != default(Vector3);
                        foreach (var key in h.targetIds)
                        {
                            customHolders[key] = h;
                        }
                    }
                }
            }
            if (iconDic.Count > 0 || sharedLimitDic.Count > 0)
            {
                InternalExtension.SetRange(GetType());
                // nested type 관련 패치가 있다면 여기에 유지
            }
        }

        [HarmonyPatch(typeof(UIInvenCardSlot), "SetSlotState")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetSlotState(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(List<DiceCardXmlInfo>), "FindAll");
            foreach(var code in instructions)
            {
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePagePatch), "ConvertSharedLimit"));
                }
                yield return code;
            }
        }

        [HarmonyPatch(typeof(UIOriginCardSlot), "SetHighlightedSlot")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetHighlightedSlot(IEnumerable<CodeInstruction> instructions)
        {
            var target = typeof(Vector3);
            bool fired = false;
            foreach(var code in instructions)
            {
                yield return code;
                if (!fired && code.Is(OpCodes.Ldelem, target))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePagePatch), "ConvertValidRangeIconHsv"));
                }
            }
        }

        [HarmonyPatch(typeof(UIOriginCardSlot), "SetData")]
        [HarmonyPostfix]
        public static void After_SetData(UIOriginCardSlot __instance, DiceCardItemModel cardmodel)
        {
            var key = cardmodel?.GetID();
            if (key != null && Instance.iconDic.ContainsKey(key))
            {
                var target = Instance.iconDic[key];
                __instance.img_RangeIcon.sprite = target.cache[target.key];
            }
            if (!Instance.components.ContainsKey(__instance))
            {
                Instance.components[__instance] = new BattlePageComponent();
            }
            Instance.components[__instance].UpdateTarget(cardmodel, __instance);
        }

        [HarmonyPatch(typeof(BattleDiceCardUI), "SetCard")]
        [HarmonyPostfix]
        public static void After_SetCard(BattleDiceCardUI __instance, BattleDiceCardModel cardModel)
        {
            if (cardModel?._xmlData == null) return;
            var key = cardModel.GetID();
            var target = Instance.customHolders.SafeGet(key);
            var exists = Instance.components2.ContainsKey(__instance);
            if (target == null && !exists) return;
            if (!exists)
            {
                Instance.components2[__instance] = new BattlePageComponent();
            }
            if (key != null && Instance.iconDic.ContainsKey(key))
            {
                var t2 = Instance.iconDic[key];
                __instance.img_icon.sprite = t2.cache[t2.key];
            }
            Instance.components2[__instance].UpdateTarget(__instance, target);
        }

        [HarmonyPatch(typeof(BattleCardAbilityDescXmlList), "GetAbilityKeywords")]
        [HarmonyPostfix]
        public static void After_GetAbilityKeywords(DiceCardXmlInfo card, ref List<string> __result)
        {
            if (card.Spec.Ranged == CardRange.Instance && Instance.iconDic.ContainsKey(card.id))
            {
                __result.Remove("Instant_Keyword");
            }
        }

        public static Predicate<DiceCardXmlInfo> ConvertSharedLimit(Predicate<DiceCardXmlInfo> origin, UIInvenCardSlot slot)
        {
            var sharedId = Instance.sharedLimitDic.SafeGet(slot.CardModel.GetID());
            if (sharedId == null) return origin;
            return (x) =>
            {
                return origin(x) || sharedId.Contains(x.id);
            };
        }
    
        private static Vector3 ConvertValidRangeIconHsv(Vector3 origin, UIOriginCardSlot instance)
        {
            try
            {
                var target = Instance.components.SafeGet(instance);
                if (target?.rangeCustom == true)
                {
                    return target.rangeHsv;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return origin;
        }
    }

    class BattlePageComponent
    {
        public bool rangeCustom;
        public Vector3 rangeHsv;

        bool applied;
        // 0 - 없음, 1 - 있음, -1 - 설정안함
        int detailExists = -1;
        Sprite originSpriteLeft;
        Sprite originSpriteRight;
        Image detailSlot;

        public void UpdateTarget(DiceCardItemModel cardmodel, UIOriginCardSlot slot)
        {
            if (cardmodel == null || slot.img_Frames == null) return;
            var target = BattlePagePatch.Instance.customHolders.SafeGet(cardmodel?.GetID());
            if (target == null)
            {
                if (applied)
                {
                    slot.img_Frames[0].sprite = originSpriteLeft;
                    if (detailExists == 1) detailSlot.sprite = originSpriteRight;
                    //slot.img_Frames[4].sprite = originSpriteRight;
                    applied = false;
                    rangeCustom = false;
                }
                return;
            }
            else if (!applied)
            {
                if (detailExists == -1)
                {
                    detailExists = slot is UIDetailCardSlot ? 1 : 0;
                    if (detailExists == 1) detailSlot = slot.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "[Image]BgFrame");
                }
                originSpriteLeft = slot.img_Frames[0].sprite;
                if (detailExists == 1) originSpriteRight = detailSlot.sprite;
                applied = true;
            }
            slot.colorFrame = new Color(1f, 1f, 1f, 1f);
            slot.colorLineardodge = new Color(1f, 1f, 1f, 0f);
            slot.costNumbers.SetContentColor(target.color);
            var sp = target.cache[target.artwork];
            slot.img_Frames[0].sprite = sp;
            if (detailExists == 1) detailSlot.sprite = target.cache[target.artwork + "_right"];
            slot.SetLinearDodgeColor(slot.colorLineardodge);
            slot.SetFrameColor(slot.colorFrame);
            if (target.rangeCustom)
            {
                rangeCustom = true;
                rangeHsv = target.rangeHsv;
                slot.SetRangeIconHsv(target.rangeHsv);
            }

        }
    
        public void UpdateTarget(BattleDiceCardUI slot, CustomBattlePageHolder target)
        {
            if (slot.img_Frames is null) return;

            if (target == null)
            {
                if (applied)
                {
                    slot.img_Frames[0].sprite = originSpriteLeft;
                    if (detailExists == 1) detailSlot.sprite = originSpriteRight;
                    //slot.img_Frames[4].sprite = originSpriteRight;
                    applied = false;
                }
                return;
            }
            else if (!applied)
            {
                if (detailExists == -1)
                {
                    detailExists = 1;
                    detailSlot = slot.img_Frames[4];
                }
                originSpriteLeft = slot.img_Frames[0].sprite;
                if (detailExists == 1) originSpriteRight = detailSlot.sprite;
                applied = true;
            }
            slot.colorFrame = new Color(1f, 1f, 1f, 1f);
            slot.colorLineardodge = new Color(0f, 0f, 0f, 0f);
            slot.colorLineardodge_deactive = new Color(0f, 0f, 0f, 0f);
            slot.costNumbers.SetContentColor(target.color);
            var sp = target.cache[target.artwork];
            slot.img_Frames[0].sprite = sp;
            if (detailExists == 1) detailSlot.sprite = target.cache[target.artwork + "_right"];
            slot.SetLinearDodgeColor(slot.colorLineardodge);
            slot.SetFrameColor(slot.colorFrame);
            if (target.rangeCustom) slot.SetRangeIconHsv(target.rangeHsv);
        }
    }
}
