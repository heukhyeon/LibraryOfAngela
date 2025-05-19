using HarmonyLib;
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
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleUIPatch), nameof(HandleCustomUsableCard)));
                }
            }
        }

        private static void HandleCustomUsableCard(BattleUnitCardsInHandUI instance, int num)
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
                if (script is ILoACustomUsableCard c)
                {
                    var useable = c.IsUsable(owner);
                    if (owner is null)
                    {
                        Logger.Log("HandleCustomUsableCard Called, But Owner Not Detect, Maybe Other Logic Conflict...? Ignore.");
                        return;
                    }
                    c.OnHandle(ui, owner, card);
                    if (!useable)
                    {
                        ui.SetEgoLock();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"HandleCustomUsableCard Error in {num} // {instance._cardList.Count} // {card.GetID()} // {card?.GetName()} // Owner Exists : {owner != null} // {owner?.UnitData.unitData.name}");
                Logger.LogError(e);
            }
        }

    }
}
