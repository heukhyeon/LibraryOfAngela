using HarmonyLib;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Battle
{
    class ExhaustPatch
    {
        public static void Initialize()
        {
            InternalExtension.SetRange(typeof(ExhaustPatch));
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustAllCardsInHand")]
        [HarmonyPrefix]
        private static void Before_ExhaustAllCardsInHand(BattleAllyCardDetail __instance, out List<BattleDiceCardModel> __state)
        {
            __state = new List<BattleDiceCardModel>(__instance._cardInHand);
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustAllCardsInHand")]
        [HarmonyPostfix]
        private static void After_ExhaustAllCardsInHand(BattleAllyCardDetail __instance, List<BattleDiceCardModel> __state)
        {
            ExhaustCards(__instance, __state);
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustAllCards")]
        [HarmonyPrefix]
        private static void Before_ExhaustAllCards(BattleAllyCardDetail __instance, out List<BattleDiceCardModel> __state)
        {
            __state = __instance.GetAllDeck();
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustAllCards")]
        [HarmonyPostfix]
        private static void After_ExhaustAllCards(BattleAllyCardDetail __instance, List<BattleDiceCardModel> __state)
        {
            ExhaustCards(__instance, __state);
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustACard")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ExhaustACard(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(List<BattleDiceCardModel>), "Remove", parameters: new Type[] { typeof(BattleDiceCardModel) });

            foreach (var c in instructions)
            {
                if (c.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExhaustPatch), "ExhaustCard"));
                }
                yield return c;
            }
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustACardAnywhere")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ExhaustACardAnywhere(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var lastIndex = codes.FindLastIndex(d => d.opcode == OpCodes.Stloc_0);
            for (int i = 0; i < codes.Count; i++)
            {
                if (i == lastIndex)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExhaustPatch), "ExhaustCardSafe"));
                }
                yield return codes[i];
            }
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustRandomCardInHand")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ExhaustRandomCardInHand(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_ExhaustACard(instructions);
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustRandomCardInDeck")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ExhaustRandomCardInDeck(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_ExhaustACard(instructions);
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustCard", new Type[] { typeof(LorId) })]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ExhaustCard_LorId(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_ExhaustACard(instructions);
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustCardInHand", new Type[] { typeof(LorId) })]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ExhaustCardInHand_LorId(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_ExhaustACard(instructions);
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "ExhaustCardInHand", new Type[] { typeof(BattleDiceCardModel) })]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ExhaustCardInHand_BattleDiceCardModel(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_ExhaustACard(instructions);
        }

        private static void ExhaustCards(BattleAllyCardDetail __instance, List<BattleDiceCardModel> cards)
        {
            foreach (var eff in BattleInterfaceCache.Of<IOnExhaustCard>(__instance._self))
            {
                foreach (var c in cards)
                {
                    try
                    {
                        eff.OnExhaustCard(c);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
        }

        private static BattleDiceCardModel ExhaustCard(BattleDiceCardModel c, BattleAllyCardDetail __instance)
        {
            foreach (var eff in BattleInterfaceCache.Of<IOnExhaustCard>(__instance._self))
            {
                try
                {
                    eff.OnExhaustCard(c);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            return c;
        }

        private static bool ExhaustCardSafe(bool removed, BattleAllyCardDetail __instance, BattleDiceCardModel c)
        {
            if (removed)
            {
                foreach (var eff in BattleInterfaceCache.Of<IOnExhaustCard>(__instance._self))
                {
                    try
                    {
                        eff.OnExhaustCard(c);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }

            return removed;
        }
    }
}
