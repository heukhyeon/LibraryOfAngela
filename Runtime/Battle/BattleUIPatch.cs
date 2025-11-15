using BattleCharacterProfile;
using HarmonyLib;
using LibraryOfAngela.Buf;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.Battle
{
    class BattleUIPatch
    {
        // 세이브 시점에 초기화된다. (memory leak 방지)
        internal static Dictionary<DiceCardXmlInfo, int> keywordCountDic = new Dictionary<DiceCardXmlInfo, int>();

        [HarmonyPatch(typeof(KeywordListUI), "Init")]
        [HarmonyPrefix]
        private static void Before_Init(KeywordListUI __instance, DiceCardXmlInfo cardInfo, List<DiceBehaviour> behaviourList)
        {
            int sum = 0;
            if (keywordCountDic.ContainsKey(cardInfo))
            {
                sum = keywordCountDic[cardInfo];
            }
            else
            {
                sum += cardInfo.Keywords.Count;
                var hashSet = new HashSet<string>(cardInfo.Keywords);
                foreach(var t in BattleCardAbilityDescXmlList.Instance.GetAbilityKeywords(cardInfo))
                {
                    if (hashSet.Add(t))
                    {
                        sum++;
                    }
                }
                foreach(var t in behaviourList.SelectMany(x => BattleCardAbilityDescXmlList.Instance.GetAbilityKeywords_byScript(x.Script)))
                {
                    if (hashSet.Add(t))
                    {
                        sum++;
                    }
                }
                keywordCountDic[cardInfo] = sum;
            }
            CheckListOverV2(__instance.keywordList.Length, sum, __instance);
        }

        [HarmonyPatch(typeof(KeywordListUI), "Init")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Trans_Init(IEnumerable<CodeInstruction> instructions)
        {
            var changeHeight = AccessTools.Field(typeof(KeywordListUI), "CHANGE_Height");
            foreach (var code in instructions)
            {
                if (code.Is(OpCodes.Ldfld, changeHeight))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleUIPatch), "FixValidHeight"));
                }
                else
                {
                    yield return code;
                }
            }
        }

        [HarmonyPatch(typeof(BattleCharacterProfileUI), nameof(BattleCharacterProfileUI.SetHpUI))]
        [HarmonyPostfix]
        public static void After_SetHpUI(BattleCharacterProfileUI __instance)
        {
            try
            {
                ShieldControllerImpl.Instance.OnUpdateCharacterProfile(__instance);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [HarmonyPatch(typeof(RencounterManager), nameof(RencounterManager.EndRencounter))]
        [HarmonyPostfix]
        public static void After_EndRencounter()
        {
            try
            {
                ShieldControllerImpl.Instance.OnEndRencounter();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private static int CheckListOverV2(int arrayLength, int target, KeywordListUI instance)
        {
            if (arrayLength > target) return arrayLength;
            var delta = (target / arrayLength) + 1;
            var newLength = delta * arrayLength;
            var newArr = new KeywordUI[newLength];
            int addCount = 1;
            for (int i = 0; i < newLength; i++)
            {
                if (i < arrayLength)
                {
                    newArr[i] = instance.keywordList[i];
                    continue;
                }
                var last = newArr[i - 1];
                var obj = UnityEngine.Object.Instantiate(last, last.transform.parent);
                obj.gameObject.name = "LoA_Additional_Keyword_" + i;
                obj.transform.localPosition = obj.transform.localPosition + new UnityEngine.Vector3(0f, -380f * addCount, 0f);
                if (i > target) obj.gameObject.SetActive(false);
                newArr[i] = obj;
                addCount++;
            }
            instance.keywordList = newArr;
            return target + 1;
        }

        [HarmonyPatch(typeof(BattleUnitInformationUI_PassiveList), "SetData")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetData(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(List<BattleUnitInformationUI_PassiveList.BattleUnitInformationPassiveSlot>), "get_Count");
            var fired = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!fired && code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleUIPatch), nameof(FixValidPassiveSlots)));
                }
            }
        }

        private static int FixValidPassiveSlots(int origin, int i, BattleUnitInformationUI_PassiveList __instance)
        {
            var list = __instance.passiveSlotList;
            while (i >= origin)
            {
                BattleUnitInformationUI_PassiveList.BattleUnitInformationPassiveSlot battleUnitInformationPassiveSlot = new BattleUnitInformationUI_PassiveList.BattleUnitInformationPassiveSlot();
                RectTransform recttransform = UnityEngine.Object.Instantiate(list[0].Rect, list[0].Rect.parent);
                battleUnitInformationPassiveSlot.Rect = recttransform;
                for (int k = 0; k < battleUnitInformationPassiveSlot.Rect.childCount; k++)
                {
                    if (battleUnitInformationPassiveSlot.Rect.GetChild(k).gameObject.name.Contains("Glow"))
                    {
                        battleUnitInformationPassiveSlot.img_IconGlow = battleUnitInformationPassiveSlot.Rect.GetChild(k).gameObject.GetComponent<Image>();
                    }
                    else
                    {
                        if (battleUnitInformationPassiveSlot.Rect.GetChild(k).gameObject.name.Contains("Desc"))
                        {
                            battleUnitInformationPassiveSlot.txt_PassiveDesc = battleUnitInformationPassiveSlot.Rect.GetChild(k).gameObject.GetComponent<TextMeshProUGUI>();
                        }
                        else
                        {
                            battleUnitInformationPassiveSlot.img_Icon = recttransform.GetChild(k).gameObject.GetComponent<Image>();
                        }
                    }
                }
                recttransform.gameObject.SetActive(false);
                list.Add(battleUnitInformationPassiveSlot);
                origin++;
            }
            return origin;
        }

        private static float FixValidHeight(float origin, int num)
        {
            if (num <= 4) return origin;

            return (380f / 4f) * (num * 2);
        }

        /// <summary>
        /// 			this._cardList[num].SetCard(list2[num], Array.Empty<BattleDiceCardUI.Option>());
        /// 			this._cardList[num].SetDefault();
        /// 			this._cardList[num].ResetSiblingIndex();
        /// ->			
        /// 			this._cardList[num].SetCard(list2[num], Array.Empty<BattleDiceCardUI.Option>());
        /// 			this._cardList[num].SetDefault();
        /// 			BattleUIPatch.HandleCustomUsableCard(this, num, battleUnitModel);
        /// 			this._cardList[num].ResetSiblingIndex();
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitCardsInHandUI), nameof(BattleUnitCardsInHandUI.UpdateCardList))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_UpdateCardList(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(BattleDiceCardUI), nameof(BattleDiceCardUI.SetDefault));
            var fired = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!fired && code.Calls(target))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleUIPatch), nameof(HandleLoACardBinder)));
                }
            }
        }

        private static void HandleLoACardBinder(BattleUnitCardsInHandUI instance, int num)
        {
            if (num < 0) {
                return;
            }

            BattleUnitModel owner = instance._selectedUnit;
            if (owner is null)
            {
                owner = instance._hOveredUnit;
            }

            var ui = instance._cardList[num];
            var card = ui.CardModel;
            try
            {
                var script = card._script ?? card.CreateDiceCardSelfAbilityScript();
                if (script is ILoACardUIBinder c)
                {
                    if (owner is null)
                    {
                        Logger.Log("HandleCustomUsableCard Called, But Owner Not Detect, Maybe Other Logic Conflict...? Ignore.");
                        return;
                    }
                    c.OnHandle(ui, owner, card);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"HandleLoACardBinder Error in {num} // {instance._cardList.Count} // {card.GetID()} // {card?.GetName()} // Owner Exists : {owner != null} // {owner?.UnitData.unitData.name}");
                Logger.LogError(e);
            }
        }



        // 최적화 패치용
        private static Dictionary<BattleUnitInformationUI_PassiveList, List<PassiveAbilityBase>> infos = new Dictionary<BattleUnitInformationUI_PassiveList, List<PassiveAbilityBase>>();

        /// <summary>
        /// 여러 책장 선택할때 패시브 수에 따라서 불필요하게 다시 모든 데이터 설정하는게 렉 유발.
        /// 캐싱해서 중복인경우 무시하게 수정
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="passivelist"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitInformationUI_PassiveList), nameof(BattleUnitInformationUI_PassiveList.SetData))]
        [HarmonyPrefix]
        private static bool Before_SetData(BattleUnitInformationUI_PassiveList __instance, List<PassiveAbilityBase> passivelist)
        {
            bool flag = false;
            if (infos.ContainsKey(__instance))
            {
                var p = infos[__instance];
                flag = CheckDiff(p, passivelist, (a1, a2) => a1.id == a2.id);
            }
            if (!flag)
            {
                infos[__instance] = passivelist;
            }
            return !flag;
        }

        /// <summary>
        /// 여러 책장 선택할때 패시브 수에 따라서 불필요하게 다시 모든 데이터 설정하는게 렉 유발.
        /// 캐싱해서 중복인경우 무시하게 수정
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="passivelist"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitInformationUI), nameof(BattleUnitInformationUI.SetAbnormalityCardList))]
        [HarmonyPrefix]
        private static bool Before_SetAbnormalityCardList(BattleUnitInformationUI __instance, List<BattleEmotionCardModel> cards)
        {
            var isEqual = CheckDiff(__instance.AbnormalityCardList.Where(d => d.Card != null), cards, (a, b) => a.Card == b.XmlInfo);
            return !isEqual;
        }

        private static bool CheckDiff<T, R>(IEnumerable<T> a1, List<R> a2, Func<T, R, bool> diff)
        {
            try
            {
                int index = 0;
                // 환상체 책장 초기화 문제
                if (a2.Count == 0) return false;

                foreach (var previous in a1)
                {
                    if (index >= a2.Count || !diff(previous, a2[index]))
                    {
                        return false;
                    }
                    index++;
                }

                return a2.Count == index - 1;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return false;
            }
        }

    }
}
