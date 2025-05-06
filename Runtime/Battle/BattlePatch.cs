using BattleCharacterProfile;
using HarmonyLib;
using LibraryOfAngela.BattleUI;
using LibraryOfAngela.Buf;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;
using LibraryOfAngela.Util;
using LOR_BattleUnit_UI;
using LOR_DiceSystem;
using Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static LibraryOfAngela.Extension.Framework.FrameworkExtension;
namespace LibraryOfAngela.Battle
{
    class BattlePatch
    {
        public static void Initialize()
        {
            InternalExtension.SetRange(typeof(BattlePatch));
            Logger.Log("BattlePatch Start");

            typeof(RencounterManager).GetNestedTypes(AccessTools.all).FirstOrDefault(x => x.Name.Contains("MoveRoutine"))
                .PatchInternal("MoveNext", flag: PatchInternalFlag.TRANSPILER);

            Logger.Log("BattlePatch Complete");
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), "UpdateDiceFinalValue")]
        [HarmonyPostfix]
        private static void After_UpdateDiceFinalValue(BattleDiceBehavior __instance, ref int ____diceResultValue, ref int ____diceFinalResultValue)
        {
            var originValue = ____diceFinalResultValue;
            var vanillaValue = ____diceResultValue;
            var returnValue = ____diceFinalResultValue;
            foreach (var target in BattleInterfaceCache.Of<ICustomFinalValueDice>(__instance.owner))
            {
                target.OnUpdateFinalValue(__instance, ref returnValue, ref vanillaValue, originValue);
            }
            ____diceFinalResultValue = returnValue;
            ____diceResultValue = vanillaValue;
        }

        // 회복값 제어
        [HarmonyPatch(typeof(BattleUnitModel), "RecoverHP")]
        [HarmonyPrefix]
        private static bool Before_RecoverHP(BattleUnitModel __instance, ref int v)
        {
            bool isNegativeFirst = v <= 0;
            bool flag = false;
            foreach (var x in BattleObjectManager.instance
                .GetAliveList(__instance.faction)
                .SelectMany(x => BattleInterfaceCache.Of<IHandleRecover>(x)))
            {
                try
                {
                    v += x.GetHpRecoverBonus(__instance, v);
                    flag = flag || x.isDamageIfNegative;
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            if (v < 0 && flag)
            {
                __instance.TakeDamage(v, DamageType.Card_Ability);
            }
            return isNegativeFirst || v >= 0;
        }

        // 흐트러짐 회복값 제어
        [HarmonyPatch(typeof(BattleUnitBreakDetail), "RecoverBreak")]
        [HarmonyPrefix]
        private static bool Before_RecoverBreak(BattleUnitBreakDetail __instance, ref int value)
        {
            bool isNegativeFirst = value <= 0;
            bool flag = false;
            foreach (var x in BattleObjectManager.instance
                .GetAliveList(__instance._self.faction)
                .SelectMany(x => BattleInterfaceCache.Of<IHandleRecover>(x)))
            {
                try
                {
                    value += x.GetBreakRecoverBonus(__instance._self, value);
                    flag = flag || x.isDamageIfNegative;
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            if (value < 0 && flag)
            {
                __instance._self.breakDetail.TakeBreakDamage(value, DamageType.Card_Ability, null, AtkResist.Normal);
            }
            return isNegativeFirst || value >= 0;
        }

        // 버프 부여 값 제어
        [HarmonyPatch(typeof(BattleUnitBufListDetail), "ModifyStack")]
        [HarmonyPostfix]
        private static void After_ModifyStack(BattleUnitBuf buf, int stack, ref int __result)
        {
            var origin = __result;
            if (buf.IsDestroyed()) return;

            foreach (var b in BattleInterfaceCache.Of<IHandleModifyBufStack>(buf._owner))
            {
                try
                {
                    __result = b.ModifyStack(buf, stack, __result);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            if ((__result == 0 && origin != __result) || buf.IsDestroyed())
            {
                try
                {
                    buf.Destroy();
                    buf._owner.bufListDetail._bufList.Remove(buf);
                    buf._owner.bufListDetail._readyBufList.Remove(buf);
                    buf._owner.bufListDetail._readyReadyBufList.Remove(buf);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), "RecoverPlayPoint")]
        [HarmonyPrefix]
        private static void Before_RecoverPlayPoint(ref int value, BattlePlayingCardSlotDetail __instance)
        {
            foreach (var effect in BattleInterfaceCache.Of<IHandleRecoverPlayPoint>(__instance._self))
            {
                try
                {
                    effect.OnHandleRecoverPlayPoint(ref value);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
        [HarmonyPatch(typeof(BattleAllyCardDetail), "DrawCards")]
        [HarmonyPrefix]
        private static void Before_DrawCards(ref int count, BattlePlayingCardSlotDetail __instance)
        {
            foreach (var effect in BattleInterfaceCache.Of<IHandleDrawCard>(__instance._self))
            {
                try
                {
                    effect.OnHandleDrawCards(ref count);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "TakeDamage")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_TakeDamage(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = AccessTools.Method(typeof(BattleUnitModel), "IsImmuneDmg", new Type[] { typeof(DamageType), typeof(KeywordBuf) });
            var shotFlag = false;
            var flag = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!shotFlag && code.Is(OpCodes.Call, targetMethod))
                {
                    shotFlag = true;
                }
                else if (shotFlag && !flag && code.opcode == OpCodes.Ldloc_1)
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "HandleChangeDamage"));
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitBreakDetail), "TakeBreakDamage")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_TakeBreakDamage(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = AccessTools.Method(typeof(BattleUnitModel), "BeforeTakeBreakDamage");
            var flag = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!flag && code.Is(OpCodes.Callvirt, targetMethod))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldarga_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "HandleChangeBreakDamage"));
                }
            }
        }

        /// <summary>
        /// if (this.CanRecoverHp(v)) -> if (OnCheckRecoverHp(this.CanRecoverHp(v), v, this))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitModel), "RecoverHP")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_RecoverHP(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var targetMethod = AccessTools.Method(typeof(BattleUnitModel), "CanRecoverHp");
            var callMethod = AccessTools.Method(typeof(BattlePatch), "OnCheckRecoverHp");
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (codes[i].Is(OpCodes.Call, targetMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, callMethod);
                }
            }
        }

        /// <summary>
        /// if (this.CanRecoverHp(v)) -> if (OnCheckRecoverHp(this.CanRecoverHp(v), v, this))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitBreakDetail), "RecoverBreak")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_RecoverBreak(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var targetMethod = AccessTools.Method(typeof(BattleUnitModel), "CanRecoverBreak");
            var callMethod = AccessTools.Method(typeof(BattlePatch), "OnCheckRecoverBreak");
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (codes[i].Is(OpCodes.Call, targetMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BattleUnitBreakDetail), "_self"));
                    yield return new CodeInstruction(OpCodes.Call, callMethod);
                }
            }
        }

        /// <summary>
        /// in BattleUnitBuf_bleeding
        /// 
        /// if (this._owner.bufListDetail.GetActivatedBuf(KeywordBuf.BloodStackBlock) == null) -> 
        /// 
        /// if (this._owner.bufListDetail.GetActivatedBuf(KeywordBuf.BloodStackBlock) == null && BalttePatch.OnCheckStackImmuneBleeding(this))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitBuf_bleeding), "AfterDiceAction")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_AfterDiceAction(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var lastTrue = codes.FindLastIndex(x => x.opcode == OpCodes.Brtrue);
            if (lastTrue == -1)
            {
                foreach (var code in codes)
                {
                    yield return code;
                }
                yield break;
            }
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];

                if (i == lastTrue)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "OnCheckStackImmuneBleeding"));
                    yield return new CodeInstruction(OpCodes.Brtrue, codes[i].operand);
                }
            }
        }

        /// <summary>
        /// 팀의 감정코인 총량을 컨트롤
        /// 
        /// if (this._emotionCoinNumber < this._currentLevelNeedEmotionMaxCoin)
        /// 
        /// ->
        /// 
        /// if (BattlePatch.HandleEmotionCoinNumber(this._emotionCoinNumber, this) < this._currentLevelNeedEmotionMaxCoin)
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(EmotionBattleTeamModel), "UpdateCoin")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_UpdateCoin(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Field(typeof(EmotionBattleTeamModel), "_emotionCoinNumber");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Ldfld, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "HandleEmotionCoinNumber"));
                }
            }
        }



        [HarmonyPatch(typeof(BattleUnitEmotionDetail), "CreateEmotionCoin")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CreateEmotionCoin(IEnumerable<CodeInstruction> instructions)
        {
            return AddOnGetEmotionCoin(ConvertMaximumLevelWrapping(instructions, false));
        }

        [HarmonyPatch(typeof(BattleCharacterProfileUI), "OnAcquireCoin")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_OnAcquireCoin(IEnumerable<CodeInstruction> instructions)
        {
            return ConvertMaximumLevelWrapping(instructions, true);
        }

        [HarmonyPatch(typeof(BattleUnitEmotionDetail), "CheckLevelUp")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CheckLevelUp(IEnumerable<CodeInstruction> instructions)
        {
            return ConvertMaximumLevelWrapping(instructions, false);
        }


        /// <summary>
        /// 1. 
        /// this.EmotionLevel >= this.MaximumEmotionLevel
        /// ->
        /// this.EmotionLevel >=  BattlePatch.CheckGetEmotionCoin(this.MaximumEmotionLevel, this)
        /// 
        /// 2.
        /// this._self.passiveDetail.OnLevelUpEmotion();
        /// ->
        /// this._self.passiveDetail.OnLevelUpEmotion();
        /// BattlePatch.OnLevelUp(this);
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitEmotionDetail), "LevelUp")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_LevelUp(IEnumerable<CodeInstruction> instructions)
        {
            var fired = false;
            var target = AccessTools.Method(typeof(BattleUnitEmotionDetail), "GetNeedEmotionCoin");
            var codes = new List<CodeInstruction>(ConvertMaximumLevelWrapping(instructions, false));
            // 마지막 ret 은 따로 쏠것이므로 for문에서는 무시한다.
            for (int i = 0; i < codes.Count - 1; i++)
            {
                var code = codes[i];
                yield return code;
                if (!fired && code.Is(OpCodes.Call, target))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "CheckUIEmotionCoinNumber"));
                }
            }
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "OnLevelUp"));
            yield return codes.Last();
        }

        struct UseRecord
        {
            public BattlePlayingCardDataInUnitModel releasedCard;
            public int order;
        }

        [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), "AddCard")]
        [HarmonyPrefix]
        private static void Before_AddCard(BattlePlayingCardSlotDetail __instance, BattleDiceCardModel card, out UseRecord __state)
        {
            __state = new UseRecord();
            try
            {
                __state.order = __instance._self.cardOrder;
                if (__state.order >= 0)
                {
                    __state.releasedCard = __instance.cardAry[__instance._self.cardOrder];
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), "AddCard")]
        [HarmonyPostfix]
        private static void After_AddCard(BattlePlayingCardSlotDetail __instance, BattleDiceCardModel card, UseRecord __state)
        {
            try
            {
                var appliedCard = __instance.cardAry.Find(c => c?.card == card);
                if (appliedCard is null && __state.releasedCard is null) return;

                foreach (var x in BattleInterfaceCache.Of<IHandleAddCard>(__instance._self))
                {
                    try
                    {
                        x.OnAddCard(appliedCard, __state.releasedCard);
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

        /// <summary>
        /// diceCardSelfAbilityBase.OnUseInstance(this._self, card, target);
        /// 
        /// -> 
        /// 
        /// diceCardSelfAbilityBase.OnUseInstance(this._self, card, target);
        /// BattlePatch.OnUseInstanceExtension(this._self, card, target, targetSlot);
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), "AddCard")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_AddCard(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(DiceCardSelfAbilityBase), "OnUseInstance");
            var target2 = AccessTools.Method(typeof(BattleUnitModel), "CanChangeAttackTarget");
            var fired = false;
            var fired2 = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!fired && code.Is(OpCodes.Callvirt, target))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BattlePlayingCardSlotDetail), "_self"));
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "OnUseInstanceExtension"));
                }
                if (!fired2 && code.Is(OpCodes.Callvirt, target2))
                {
                    fired2 = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "OnHandleApplyCard"));
                }
            }
        }

        /// <summary>
        /// cardAry != null && battleUnitModel.IsControlable()
        /// ->
        /// cardAry != null && BattlePatch.HandleSwitableTarget(battleUnitModel.IsControlable(), battleUnitModel)
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch(typeof(SpeedDiceUI), "ShowSwitchableTarget")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ShowSwitchableTarget(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(BattleUnitModel), "IsControlable");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "HandleSwitchableTarget"));
                }
            }
        }

        private static IEnumerable<CodeInstruction> Trans_MoveNext(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo targetField = AccessTools.Field(typeof(BattleUnitModel), nameof(BattleUnitModel.faction));
            var codes = instructions.ToList();
            FieldInfo instanceField = null;

            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo field && field.FieldType == typeof(RencounterManager) && field.Name.Contains("this") && codes[i - 1].opcode == OpCodes.Ldarg_0)
                {
                    instanceField = field;
                    break;
                }
            }

            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (codes[i].LoadsField(targetField) && instanceField != null)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, instanceField);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), nameof(BattlePatch.FixValidFaction)));
                }
            }
        }

        private static IEnumerable<CodeInstruction> ConvertMaximumLevelWrapping(IEnumerable<CodeInstruction> origin, bool isProfile)
        {
            var fired = false;
            var target = AccessTools.Method(typeof(BattleUnitEmotionDetail), "get_MaximumEmotionLevel");
            foreach (var code in origin)
            {
                yield return code;
                if (!fired && (code.Is(OpCodes.Call, target) || code.Is(OpCodes.Callvirt, target)))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    if (isProfile)
                    {
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BattleCharacterProfileUI), "_unitModel"));
                        yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(BattleUnitModel), "get_emotionDetail"));
                    }

                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "CheckGetEmotionCoin"));
                }
            }
        }

        private static IEnumerable<CodeInstruction> AddOnGetEmotionCoin(IEnumerable<CodeInstruction> origin)
        {
            var fired = false;
            foreach (var code in origin)
            {
                yield return code;
                if (!fired && code.opcode == OpCodes.Starg_S && code.operand is byte b && b == 0x02)
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "HandleGetEmotionCoin"));
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitBufListDetail), "AddKeywordBufThisRoundByEtc")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_AddKeywordBufThisRoundByEtc(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return WrappingOnGiveKeywordBuf(instructions, original);
        }

        [HarmonyPatch(typeof(BattleUnitBufListDetail), "AddKeywordBufByEtc")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_AddKeywordBufByEtc(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return WrappingOnGiveKeywordBuf(instructions, original);
        }

        [HarmonyPatch(typeof(BattleUnitBufListDetail), "AddKeywordBufThisRoundByCard")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_AddKeywordBufThisRoundByCard(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return WrappingOnGiveKeywordBuf(instructions, original);
        }

        [HarmonyPatch(typeof(BattleUnitBufListDetail), "AddKeywordBufByCard")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_AddKeywordBufByCard(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return WrappingOnGiveKeywordBuf(instructions, original);
        }

        [HarmonyPatch(typeof(BattleUnitBufListDetail), "AddKeywordBufNextNextByCard")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_AddKeywordBufNextNextByCard(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return WrappingOnGiveKeywordBuf(instructions, original);
        }



        private static IEnumerable<CodeInstruction> WrappingOnGiveKeywordBuf(IEnumerable<CodeInstruction> origin, MethodBase original)
        {
            var codes = new List<CodeInstruction>(origin);
            var target = AccessTools.Method(typeof(BattleUnitBufListDetail), "CheckGift");
            var target2 = AccessTools.Method(typeof(BattleUnitBufListDetail), "AddNewKeywordBufInList");
            var isCard = original.Name.EndsWith("ByCard");
            OpCode targetOpCode = OpCodes.Ldc_I4_0;
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                yield return code;
                if (code.Is(OpCodes.Call, target2))
                {
                    for (int j = i; j > 0; j--)
                    {
                        var code2 = codes[j];
                        var opCode = code2.opcode;
                        if (opCode == OpCodes.Ldc_I4_0 || opCode == OpCodes.Ldc_I4_1 || opCode == OpCodes.Ldc_I4_2)
                        {
                            targetOpCode = opCode;
                            break;
                        }
                    }
                }
                if (code.Is(OpCodes.Call, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(targetOpCode);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, isCard ? 1 : 0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), nameof(HandleOnGiveKeywordBuf)));
                }
            }
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "AddCardToHand")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_AddCardToHand(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(BattleDiceCardModel), "CreateDiceCardSelfAbilityScript");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), "HandleValidScript"));
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitBuf_warpCharge), nameof(BattleUnitBuf.OnAddBuf))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_OnAddBuf(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var fired = false;
            foreach (var code in instructions)
            {
                if ((code.opcode == OpCodes.Ble_S || code.opcode == OpCodes.Ble) && !fired)
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), nameof(HandleChargeMaximum)));
                }
                yield return code;
            }
        }

        [HarmonyPatch(typeof(BattleUnitBuf_smoke), nameof(BattleUnitBuf.OnAddBuf))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler_Smoke_OnAddBuf(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var local = generator.DeclareLocal(typeof(int));

            yield return new CodeInstruction(OpCodes.Ldc_I4_S, 10);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), nameof(HandleSmokeMaximum)));
            yield return new CodeInstruction(OpCodes.Stloc, local);

            foreach (var c in codes)
            {
                if (c.opcode == OpCodes.Ldc_I4_S && (sbyte) c.operand == 10)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, local);
                }
                else
                {
                    yield return c;
                }
            }
        }

        private static int HandleSmokeMaximum(int current, BattleUnitBuf target)
        {
            return HandleKeywordMaximum(target._owner, KeywordBuf.Smoke, current);
        }

        private static int HandleChargeMaximum(int current, BattleUnitBuf target, ref int realNum)
        {
            realNum = HandleKeywordMaximum(target._owner, KeywordBuf.WarpCharge, current);
            return realNum;
        }

        // 연기는 10 하드코딩이라 충전과는 좀 다르게 처리 필요. 처리 자체는 동일할텐데 트랜스파일러가 달라야함
        /*        private static int HandleSmokeMaximum(int current, BattleUnitBuf target, ref int realNum)
                {
                    realNum = HandleKeywordMaximum(target._owner, KeywordBuf.Smoke, current);
                    return realNum;
                }*/

        private static int HandleKeywordMaximum(BattleUnitModel owner, KeywordBuf target, int current)
        {
            try
            {
                if (owner is null) return current;

                var c = current;
                foreach (var b in BattleInterfaceCache.Of<IHandleKeywordMaximum>(owner))
                {
                    var next = b.OnKeywordStackMaximum(target, c);
                    if (next > c)
                    {
                        c = next;
                    }
                }
                return c;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return current;
        }


        private static void HandleOnGiveKeywordBuf(BattleUnitBufListDetail targetDetail, BattleUnitBuf buf, int stack, BattleUnitModel actor, BufReadyType type, bool isCard)
        {
            try
            {
                var target = targetDetail._self;
                foreach (var b in BattleInterfaceCache.Of<IOnGiveKeywordBuf>(actor))
                {
                    b.OnGiveKeywordBuf(target, buf, stack, type, isCard);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static DiceCardSelfAbilityBase HandleValidScript(DiceCardSelfAbilityBase origin, BattleDiceCardModel card)
        {
            if (card._xmlData?.id?.IsWorkshop() != true)
                return origin;
            if (LoAModCache.Instance[card.GetID().packageId] != null)
            {
                return card._script;
            }
            return origin;
        }

        private static int HandleChangeDamage(int origin, ref int originRef, BattleUnitModel owner, int damage, DamageType type, BattleUnitModel attacker, KeywordBuf bufType)
        {
            try
            {
                foreach (var effect in BattleInterfaceCache.Of<IHandleChangeDamage>(owner))
                {
                    effect.HandleDamage(damage, ref originRef, type, attacker, bufType);
                }
                return originRef;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return origin;
            }
        }

        private static bool HandleChangeBreakDamage(bool origin, ref int originRef, BattleUnitBreakDetail owner, DamageType type, BattleUnitModel attacker, KeywordBuf bufType)
        {
            try
            {
                foreach (var effect in BattleInterfaceCache.Of<IHandleChangeDamage>(owner._self))
                {
                    effect.HandleBreakDamage(originRef, ref originRef, type, attacker, bufType);
                }
                return origin || originRef == 0;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return origin;
            }
        }




        private static Faction FixValidFaction(Faction origin, RencounterManager instance)
        {
            try
            {
                if (instance._currentLibrarianBehaviourResult.diceBehaviourResult.result == Result.Win && origin != Faction.Player)
                {
                    return Faction.Player;
                }
                if (instance._currentEnemyBehaviourResult.diceBehaviourResult.result == Result.Win && origin != Faction.Enemy)
                {
                    return Faction.Enemy;
                }
            }
            catch (NullReferenceException)
            {
                try
                {
                    Logger.Log($"Called Move Routine, But Behaviour Result is Null... What ?? {instance._currentLibrarianBehaviourResult != null} // {instance._currentEnemyBehaviourResult != null}");
                }
                catch
                {

                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return origin;
        }
        private static int CheckUIEmotionCoinNumber(int origin, BattleUnitEmotionDetail instance)
        {
            if (instance.EmotionLevel >= instance.MaximumEmotionLevel)
            {
                foreach (var x in BattleInterfaceCache.Of<IHandleEmotionLevelUp>(instance._self))
                {
                    try
                    {
                        if (x.IsOverGetEmotionCoin) return BattleUnitEmotionDetail.GetNeedEmotionCoin(instance.MaximumEmotionLevel);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
            return origin;
        }

        private static int CheckGetEmotionCoin(int originMaxLevel, BattleUnitEmotionDetail instance)
        {
            var level = instance.EmotionLevel;
            if (level >= originMaxLevel)
            {
                foreach (var x in BattleInterfaceCache.Of<IHandleEmotionLevelUp>(instance._self))
                {
                    try
                    {
                        if (x.IsOverGetEmotionCoin) return level + 1;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
            return originMaxLevel;
        }

        private static void HandleGetEmotionCoin(BattleUnitEmotionDetail instance, EmotionCoinType coinType, int count)
        {
            if (count == 0) return;

            foreach (var effect in BattleInterfaceCache.Of<IHandleOnGetEmotionCoin>(instance._self))
            {
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        effect.OnGetEmotionCoin(coinType);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
        }

        private static void OnLevelUp(BattleUnitEmotionDetail instance)
        {
            foreach (var x in BattleInterfaceCache.Of<IHandleEmotionLevelUp>(instance._self))
            {
                try
                {
                    x.OnLevelUpEmotion();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        private static bool OnCheckRecoverHp(bool origin, int value, BattleUnitModel owner)
        {
            if (!origin)
            {
                foreach (var x in BattleInterfaceCache.Of<IPreventRecover>(owner))
                {
                    x.OnRecoverHpFailed(value);
                }
            }
            return origin;
        }

        private static bool OnCheckRecoverBreak(bool origin, int value, BattleUnitModel owner)
        {
            if (!origin)
            {
                foreach (var x in BattleInterfaceCache.Of<IPreventRecover>(owner))
                {
                    x.OnRecoverBreakFailed(value);
                }
            }
            return origin;
        }

        private static bool HandleSwitchableTarget(bool origin, BattleUnitModel target)
        {
            if (origin) return true;
            foreach (var t in BattleInterfaceCache.Of<IAutoController>(target))
            {
                if (t.IsSwitchable)
                {
                    return true;
                }
            }
            return origin;
        }

        public static bool OnCheckStackImmuneBleeding(BattleUnitBuf buf)
        {
            return (SceneBufPatch.currentBuf as IPreventBufStack)?.IsStackImmune(buf, buf._owner) == true;
        }

        [HarmonyPatch(typeof(BattleUnitEmotionDetail), "GetAccumulatedEmotionCoinNum")]
        [HarmonyPostfix]
        private static void After_GetAccumulatedEmotionCoinNum(ref int __result, BattleUnitEmotionDetail __instance)
        {
            foreach (var effect in BattleInterfaceCache.Of<IHandleAccumulatedEmotionCoinNum>(__instance._self))
            {
                try
                {
                    __result = effect.GetAccumulatedEmotionCoinNum(__result);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitEmotionDetail), "OnDie")]
        [HarmonyPostfix]
        private static void After_OnDie(BattleUnitModel killer, BattleUnitEmotionDetail __instance)
        {
            try
            {
                foreach (var effect in BattleInterfaceCache.Of<IOnDieExtension>(__instance._self))
                {
                    effect.OnDie(killer);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "GetBreakDamageReductionAll")]
        [HarmonyPostfix]
        private static void After_GetBreakDamageReductionAll(int dmg, DamageType dmgType, BattleUnitModel attacker, BattleUnitModel __instance, ref int __result)
        {
            foreach (var effect in BattleInterfaceCache.Of<IGetBreakDamageReductionAll>(__instance))
            {
                try
                {
                    __result += effect.GetBreakDamageReductionAll(dmg, dmgType, attacker);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }



        public static bool isStandbyAttackDice(BattleDiceBehavior behaviour)
        {
            if (behaviour == null) return false;
            return behaviour.IsAttackDice(behaviour.Detail) == true && behaviour?.card?.card?.GetSpec()?.Ranged == CardRange.Far;
        }



        private static void OnUseInstanceExtension(DiceCardSelfAbilityBase ability, BattleUnitModel unit, BattleDiceCardModel card, BattleUnitModel targetUnit, int targetSpeedDiceIndex)
        {
            if (ability is IExtensionUseInstance p)
            {
                p.OnUseInstance(unit, card, targetUnit, targetSpeedDiceIndex);
            }
            foreach (var listener in BattleInterfaceCache.Of<IUseInstanceListener>(unit))
            {
                listener.OnUseInstance(card, targetUnit);
            }
        }

        private static bool OnHandleApplyCard(bool origin, BattlePlayingCardSlotDetail instance)
        {
            bool result = origin;
            try
            {
                var card = instance.cardAry[instance._self.cardOrder];

                foreach (var effect in BattleInterfaceCache.Of<IHandleApplyCard>(instance._self))
                {
                    effect.OnApplyCard(card, instance._self.cardOrder);
                }

                if (instance.cardAry[instance._self.cardOrder] != card)
                {
                    result = false;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }


            return result;
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnTakeBreakDamageByAttack")]
        [HarmonyPostfix]
        private static void After_OnTakeBreakDamageByAttack(BattleDiceBehavior atkDice, int breakdmg, BattleUnitModel __instance)
        {
            foreach (var x in BattleInterfaceCache.Of<IBufBreakDamageListener>(atkDice.owner))
            {
                x.OnTakeBreakDamageByAttack(atkDice, breakdmg);
            };
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.SpendCard))]
        [HarmonyPrefix]
        private static void Before_SpendCard(BattleDiceCardModel card, out bool __state)
        {
            __state = card.costSpended;
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.SpendCard))]
        [HarmonyPostfix]
        private static void After_SpendCard(BattleDiceCardModel card, BattleUnitModel ____self, bool __state)
        {
            if (__state)
            {
                HandleSpendCard(card, ____self, null, false);
            }
        }

        [HarmonyPatch(typeof(BattlePersonalEgoCardDetail), "SpendCard")]
        [HarmonyPostfix]
        private static void After_SpendCard(BattleDiceCardModel card, BattleUnitModel ____self, ref bool __result,
            List<BattleDiceCardModel> ____cardInUse,
            List<BattleDiceCardModel> ____cardInHand)
        {
            if (__result)
            {
                var script = card.CreateDiceCardSelfAbilityScript();
                if (script is IRepeatPersonalCard sc && sc?.isReturnToHandImmediately(____self, card) == true)
                {
                    ____cardInUse.Remove(card);
                    ____cardInHand.Add(card);
                }
                HandleSpendCard(card, ____self, script, true);
            }
        }

        private static void HandleSpendCard(BattleDiceCardModel card, BattleUnitModel owner, object script, bool isPersonal)
        {
            try
            {
                if (script is null) script = card.CreateDiceCardSelfAbilityScript();
                bool flag = false;
                ILoACardListController controller = null;
                foreach (var b in BattleInterfaceCache.Of<IHandleSpendCard>(owner))
                {
                    if (controller is null)
                    {
                        controller = isPersonal ? new LoACardListControllerImpl(owner.personalEgoDetail) : new LoACardListControllerImpl(owner.allyCardDetail);
                    }
                    b.OnSpendCard(owner, card, controller);
                    if (b == script) flag = true;
                }
                if (!flag && script is IHandleSpendCard b2)
                {
                    if (controller is null)
                    {
                        controller = isPersonal ? new LoACardListControllerImpl(owner.personalEgoDetail) : new LoACardListControllerImpl(owner.allyCardDetail);
                    }
                    b2.OnSpendCard(owner, card, controller);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }


        

        [HarmonyPatch(typeof(BattleUnitPassiveDetail), "OnStartTargetedByAreaAtk")]
        [HarmonyPostfix]
        private static void After_OnStartTargetedByAreaAtk(BattlePlayingCardDataInUnitModel attackerCard, BattleUnitPassiveDetail __instance)
        {
            foreach (var eff in BattleInterfaceCache.Of<IOnStartTargetedByAreaAtk>(__instance._self))
            {
                try
                {
                    eff.OnStartTargetedByAreaAtk(attackerCard);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), nameof(BattleDiceBehavior.ApplyDiceStatBonus))]
        [HarmonyPrefix]
        private static void Before_ApplyDiceStatBonus(BattleDiceBehavior __instance, ref DiceStatBonus bonus)
        {
            try
            {
                if (__instance?.card is null || bonus is null) return;

                var b = bonus;
                b = SceneBufPatch.currentBuf?.ConvertDiceStatBonus(__instance, b) ?? b;
                foreach (var x in BattleInterfaceCache.Of<IHandleDiceStatBonus>(__instance.owner))
                {
                    b = x.ConvertDiceStatBonus(__instance, b) ?? b;
                }
                bonus = b;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // 합 결과 강제 조정 
        [HarmonyPatch(typeof(BattleParryingManager), "GetDecisionResult")]
        [HarmonyPostfix]
        private static void After_GetDecisionResult(BattleParryingManager.ParryingTeam teamA, BattleParryingManager.ParryingTeam teamB, ref BattleParryingManager.ParryingDecisionResult __result)
        {
            var result = __result;
            var enemyHandlers = BattleInterfaceCache.Of<IHandleParryingResult>(teamA.unit).Select(x => x.OnDecisionResult(DecisionToResult(result, true)))
                .Where(x => x != null)
                .OfType<Result>()
                .OrderBy(x => (int)x)
                .ToList();

            var allyHandlers = BattleInterfaceCache.Of<IHandleParryingResult>(teamB.unit).Select(x => x.OnDecisionResult(DecisionToResult(result, false)))
                .Where(x => x != null)
                .OfType<Result>()
                .OrderBy(x => (int)x)
                .ToList();

            var enemyExists = enemyHandlers.Count() > 0;
            var librarianExists = allyHandlers.Count() > 0;

            if (!enemyExists && !librarianExists) return;
            // 아군만 합 강제 조정을 하는 경우
            else if (!enemyExists && librarianExists)
            {
                var handlerB = allyHandlers.First();
                __result = handlerB == Result.Win ? BattleParryingManager.ParryingDecisionResult.WinLibrarian : handlerB == Result.Draw ? BattleParryingManager.ParryingDecisionResult.Draw : BattleParryingManager.ParryingDecisionResult.WinEnemy;

            }
            // 적만 합 조정을 하거나 , 혹은 양 쪽이 다 하는경우
            else
            {
                var handlerA = enemyHandlers.First();
                __result = handlerA == Result.Win ? BattleParryingManager.ParryingDecisionResult.WinEnemy : handlerA == Result.Draw ? BattleParryingManager.ParryingDecisionResult.Draw : BattleParryingManager.ParryingDecisionResult.WinLibrarian;
            }
        }

        private static Result DecisionToResult(BattleParryingManager.ParryingDecisionResult result, bool isEnemy)
        {
            if (result == BattleParryingManager.ParryingDecisionResult.Draw) return Result.Draw;
            if (result == BattleParryingManager.ParryingDecisionResult.WinEnemy && isEnemy) return Result.Win;
            if (result == BattleParryingManager.ParryingDecisionResult.WinLibrarian && !isEnemy) return Result.Win;
            return Result.Lose;
        }

        // 합 변경 불가 처리
        [HarmonyPatch(typeof(BattleUnitModel), "CanChangeAttackTarget")]
        [HarmonyPostfix]
        private static void After_CanChangeAttackTarget(BattleUnitModel __instance, int myIndex, int targetIndex, BattleUnitModel target, ref bool __result)
        {
            if (__result)
            {
                var myCard = __instance.cardSlotDetail.cardAry[myIndex];
                var targetCard = target.cardSlotDetail.cardAry[targetIndex];
                if (myCard == null) return;
                if (BattleInterfaceCache.Of<IForceOneSideBuf>(__instance).Any(x =>
                {
                    return x?.IsForceOneSideAction(myCard, targetCard) == true;
                }) || BattleInterfaceCache.Of<IForceOneSideBuf>(target).Any(x =>
                {
                    return x?.IsForceOneSideAction(targetCard, myCard) == true;
                }))
                {
                    __result = false;
                }
            }
            else
            {
                __result = (__instance.cardSlotDetail.cardAry[myIndex]?.cardAbility as IAggroAbility)?.CanForcelyAggro(target, myIndex, targetIndex) ?? __result;
                if (!__result)
                {
                    __result = BattleInterfaceCache.Of<IAggroAbility>(__instance).Any(x => x.CanForcelyAggro(target, myIndex, targetIndex));
                }
            }

        }

        [HarmonyPatch(typeof(BattleUnitModel), "AfterTakeDamage")]
        [HarmonyPostfix]
        private static void After_AfterTakeDamage(BattleUnitModel __instance, BattleUnitModel attacker, int dmg)
        {
            foreach (var passive in BattleInterfaceCache.Of<IAfterTakeDamageListener>(__instance))
            {
                passive.AfterTakeDamage(attacker, dmg);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "IsTargetableUnit")]
        [HarmonyPostfix]
        private static void After_IsTargetableUnit(BattleDiceCardModel card, BattleUnitModel actor, BattleUnitModel target, int targetDiceIdx, ref bool __result)
        {
            if (!__result) return;
            if (card._script is IExtensionUseInstance s)
            {
                __result = s.IsValidTarget(actor, card, target, targetDiceIdx);
            }
        }

        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnStandbyBehaviour")]
        [HarmonyPostfix]
        private static void After_OnStandbyBehaviour(BattlePlayingCardDataInUnitModel __instance, ref List<BattleDiceBehavior> __result)
        {
            foreach (var passive in BattleInterfaceCache.Of<IHandleStandbyDice>(__instance.owner))
            {
                passive.OnStandbyBehaviour(__result, __instance);
            }
            // 전투 책장은 OnUseCard 시점에 캐시에 등록되므로 이 시점에 예외적으로 처리
            if (__instance.cardAbility is IHandleStandbyDice p)
            {
                p.OnStandbyBehaviour(__result, __instance);
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnBreakGageZero")]
        [HarmonyPostfix]
        private static void After_OnBreakGageZero(BattleUnitModel __instance, ref bool __result)
        {
            if (__result) return;

            foreach (var buf in BattleInterfaceCache.Of<IHandleOnBreakGageZero>(__instance))
            {
                if (buf.OnBreakGageZero())
                {
                    __result = true;
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitBuf), "Destroy")]
        [HarmonyPrefix]
        private static bool Before_Destroy(BattleUnitBuf __instance)
        {
            if (BattleInterfaceCache.Of<IPreventBufDestroy>(__instance).Any(ab =>
            {
                try
                {
                    return ab.IsPreventBufDestroyed(__instance);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                    return false;
                }
            }) == true)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(BattleObjectManager), "RegisterUnit")]
        [HarmonyPostfix]
        private static void After_RegisterUnit(BattleUnitModel unit)
        {
            var id = new AssetBundleType.CorePage(unit.Book.BookId);
            LoAAssetBundles.Instance.LoadAssetBundle(id);
        }

        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "NextDice")]
        [HarmonyPrefix]
        private static void Before_NextDice(BattlePlayingCardDataInUnitModel __instance)
        {
            BattleResultPatch.SaveLibmusDice(__instance.currentBehavior);
            var owner = __instance.owner;
            if (owner == null) return;
            Stack<BattleDiceBehavior> items = null;
            foreach (var passive in BattleInterfaceCache.Of<IHandleNextDice>(__instance.owner))
            {
                try
                {
                    foreach (var dice in passive.BeforeNextDice(__instance))
                    {
                        if (items is null)
                        {
                            items = new Stack<BattleDiceBehavior>();
                        }
                        items.Push(dice);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            if (items != null)
            {
                __instance.cardBehaviorQueue = new Queue<BattleDiceBehavior>(Enumerable.Concat(items, __instance.cardBehaviorQueue));
            }

            return;
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "Init")]
        [HarmonyPostfix]
        private static void After_AllyCard_Init(BattleAllyCardDetail __instance, List<DiceCardXmlInfo> deck)
        {
            foreach (var d in deck)
            {
                var request = new AssetBundleType.BattlePage(d.id);
                LoAAssetBundles.Instance.LoadAssetBundle(request);
            }
        }

        // 반격 주사위로는 호크마 수호 발동 안되는거 수정. (반격 주사위는 재사용이므로 NextDice가 기본적으로는 미호출임)
        [HarmonyPatch(typeof(BattleParryingManager), "CheckParryingEnd")]
        [HarmonyPrefix]
        private static bool Before_CheckParryingEnd(BattleParryingManager __instance)
        {
            if (FillBehaviour(__instance._teamLibrarian, __instance._teamEnemy)) return true;
            if (FillBehaviour(__instance._teamEnemy, __instance._teamLibrarian)) return true;
            return true;
        }

        private static bool FillBehaviour(BattleParryingManager.ParryingTeam owner, BattleParryingManager.ParryingTeam target)
        {
            if (owner.isKeepedCard && !target.DiceExists())
            {
                BattleDiceBehavior behaviour = null;
                foreach (var passive in BattleInterfaceCache.Of<IHandleNextDice>(owner.unit))
                {
                    try
                    {
                        foreach (var dice in passive.BeforeNextDice(owner.playingCard))
                        {
                            if (dice.Type != BehaviourType.Standby)
                            {
                                behaviour = dice;
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
                if (behaviour != null)
                {
                    owner.playingCard.currentBehavior = behaviour;
                    return true;
                }
            }
            return false;
        }

        private static int HandleEmotionCoinNumber(int current, EmotionBattleTeamModel team)
        {
            foreach (var b in BattleObjectManager.instance.GetAliveList(team.faction).SelectMany(x => BattleInterfaceCache.Of<IHandleNeedTeamLevelUpEmotionCoin>(x)))
            {
                current = b.HandleTeamCoin(current, team);
            }
            return current;
        }

        [HarmonyPatch(typeof(PassiveAbilityBase), "OnCreated")]
        [HarmonyPostfix]
        private static void After_OnCreated(PassiveAbilityBase __instance)
        {
            if (BattlePhasePatch.IsWaveStartCalled) return;
            if (__instance is IAllCharacterBufController p) InjectAllControllerBuf(p);
        }

        [HarmonyPatch(typeof(EmotionCardAbilityBase), "OnSelectEmotionOnce")]
        [HarmonyPostfix]
        private static void After_OnSelectEmotionOnce(EmotionCardAbilityBase __instance)
        {
            if (__instance is IAllCharacterBufController p) InjectAllControllerBuf(p);
        }

        private static int callCount = 0;

        [HarmonyPatch(typeof(StageController), "StartAction")]
        [HarmonyPrefix]
        private static bool Before_StartAction(BattlePlayingCardDataInUnitModel card)
        {
            ParryingOneSideAction current = null;
            bool isChanged = false;

            foreach (var b in BattleObjectManager.instance.GetAliveList(false)
                .SelectMany(x => BattleInterfaceCache.Of<IHandleParryingOnesideController>(x)))
            {
                if (current is null)
                {
                    current = new ParryingOneSideAction.OneSide(card);
                }
                var next = b.HandleParryingOneside(current);
                if (next is null || next == current) continue;
                isChanged = true;
                current = next;
                Logger.Log($"Force Action Handle Detect From OneSide, Controller : {b.GetType().FullName}");
            }
            if (isChanged) return ForceHandle(current);
            return true;
        }

        [HarmonyPatch(typeof(StageController), "StartParrying")]
        [HarmonyPrefix]
        private static bool Before_StartParrying(BattlePlayingCardDataInUnitModel cardA, BattlePlayingCardDataInUnitModel cardB)
        {
            ParryingOneSideAction current = null;
            bool isChanged = false;

            foreach (var b in BattleObjectManager.instance.GetAliveList(false)
                .SelectMany(x => BattleInterfaceCache.Of<IHandleParryingOnesideController>(x)))
            {
                if (current is null)
                {
                    current = new ParryingOneSideAction.Parrying(cardA, cardB);
                }
                var next = b.HandleParryingOneside(current);
                if (next is null || next == current) continue;
                isChanged = true;
                current = next;
                Logger.Log($"Force Action Handle Detect From Parrying, Controller : {b.GetType().FullName}");
            }
            if (isChanged) return ForceHandle(current);
            return true;
        }

        private static bool ForceHandle(ParryingOneSideAction current)
        {
            if (callCount++ > 5)
            {
                Logger.Log("It suspects an infinite loop and ignores the forced behavior. Please look at the logs and respond accordingly.");
                return true;
            }
            if (current is ParryingOneSideAction.OneSide one)
            {
                StageController.Instance.StartAction(one.card);
                callCount--;
                return false;
            }
            else if (current is ParryingOneSideAction.Parrying pa)
            {
                StageController.Instance.StartParrying(pa.card1, pa.card2);
                callCount--;
                return false;
            }
            return true;
        }


        private static float _lastElepsedTime = 0f;

        [HarmonyPatch(typeof(BattleAllyCardDetail), "PlayTurnAutoForPlayer")]
        [HarmonyFinalizer]
        private static Exception Finalize_PlayTurnAutoForPlayer(int idx, BattleAllyCardDetail __instance, ref Exception __exception)
        {
            // 지정할 대상 없어서 터진 경우
            if (__exception is NullReferenceException && BattleObjectManager.instance.GetAliveListExceptFaction(__instance._self.faction).All(d => !d.IsTargetable(__instance._self)))
            {
                var time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (time - _lastElepsedTime > 2000f)
                {
                    _lastElepsedTime = time;
                    Logger.Log("Exception detection due to untargetability. Ignore.");
                }
                return null;
            }
            return __exception;
        }

        [HarmonyPatch(typeof(BattleUnitDiceActionUI), "UpdateDiceAnimation")]
        [HarmonyFinalizer]
        private static Exception Finalize_UpdateDiceAnimation(BattleUnitDiceActionUI __instance, CompareBehaviourUIType type, DiceUITiming timing, ref Exception __exception)
        {
            if (__exception is ArgumentOutOfRangeException)
            {
                var builder = new StringBuilder("ArgumentOutOfRangeException detection UpdateDiceAnimation\n");
                builder.AppendLine($"type : {type} // timing : {timing}");
                builder.AppendLine($"remainDiceAnimSlotList : {__instance._remainDiceAnimSlotList.Count}");
                builder.AppendLine($"startBehaviours : {__instance.currentResult[0].currentAllDiceState.StartBehaviours.Count}");
                builder.AppendLine($"expectedEnd : {__instance.currentResult[0].currentAllDiceState.StartBehaviours.Count + 1}");
                Logger.Log(builder.ToString());
                return null;
            }
            return __exception;
        }

        public static void InjectAllControllerBuf(IAllCharacterBufController controller)
        {
            var bufs = controller.bufs;
            bufs.RemoveAll(d =>
            {
                if (!d._destroyed) d.Destroy();
                return true;
            });
            foreach (var unit in BattleObjectManager.instance._unitList)
            {
                if (unit is null || unit.IsDead()) continue;
                var b = controller.CreateBuf(unit);
                if (b != null)
                {
                    bufs.Add(b);
                    if (b._owner is null) unit.bufListDetail.AddBuf(b);
                }
            }
        }

        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), nameof(BattlePlayingCardDataInUnitModel.OnStandbyBehaviour))]
        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), nameof(BattlePlayingCardDataInUnitModel.ResetCardQueueWithoutStandby))]
        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), nameof(BattlePlayingCardDataInUnitModel.ResetCardQueue))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_HandleCreateDiceBehaviourList(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(BattleDiceCardModel), "CreateDiceCardBehaviorList");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattlePatch), nameof(HandleCreateDiceBehaviourList)));
                }
            }
        }

        private static List<BattleDiceBehavior> HandleCreateDiceBehaviourList(List<BattleDiceBehavior> origin, BattlePlayingCardDataInUnitModel card)
        {
            var owner = card?.owner ?? card?.card?.owner;
            if (owner is null) return origin;
            var ability = card.cardAbility;
            bool flag = false;

            foreach (var effect in BattleInterfaceCache.Of<IHandleCreateDiceCardBehaviourList>(owner))
            {
                try
                {
                    effect.OnCreateDiceCardBehaviorList(card, origin);
                    if (!flag && effect == ability)
                    {
                        flag = true;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            if (!flag && ability is IHandleCreateDiceCardBehaviourList p)
            {
                try
                {
                    p.OnCreateDiceCardBehaviorList(card, origin);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            return origin;
        }


    }

    struct ReservePassiveModel
    {
        public BattleUnitPassiveDetail owner;
        public LorId id;
    }


}
