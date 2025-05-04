using HarmonyLib;
using LibraryOfAngela.Implement;
using Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Battle
{
    class FixValidPatch
    {
        public static void Initialize()
        {
            InternalExtension.SetRange(typeof(FixValidPatch));
        }

        /// <summary>
        /// 얀 지령 (yanSpecial1, yanSpecial2)
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(DiceCardSelfAbility_yanSpecial1), "OnUseInstance")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_OnUseInstance_yanSpecial1(IEnumerable<CodeInstruction> instructions)
        {
            return FixActor(instructions, OpCodes.Ldarg_1, null);
        }

        /// <summary>
        /// 얀 지령 (yanSpecial1, yanSpecial2)
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(DiceCardSelfAbility_yanSpecial2), "OnUseInstance")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_OnUseInstance_yanSpecial2(IEnumerable<CodeInstruction> instructions)
        {
            return FixActor(instructions, OpCodes.Ldarg_1, null);
        }

        /// <summary>
        /// 미리내 패시브 (PassiveAbility_260002)
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(PassiveAbility_260002), "OnAddKeywordBufByCardForEvent")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_OnAddKeywordBufByCardForEvent(IEnumerable<CodeInstruction> instructions)
        {
            return FixActor(instructions, OpCodes.Ldfld, AccessTools.Field(typeof(PassiveAbilityBase), "owner"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(EmotionCardAbility_bluestar3), "OnRoundStart")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_OnRoundStart(IEnumerable<CodeInstruction> instructions)
        {
            var realCodes = new List<CodeInstruction>();
            var codes = new List<CodeInstruction>(instructions);
            var lastIndex = codes.FindLastIndex(x => x.opcode == OpCodes.Ldnull);
            var target = AccessTools.Method(typeof(UnityEngine.Object), "op_Equality");
            for (int i = 0; i < codes.Count; i++)
            {
                realCodes.Add(codes[i]);
                try
                {
                    if (codes[i].Is(OpCodes.Ldstr, "Creature/BlueStar_Atk")) 
                    {
                        realCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FixValidPatch), "FixBlueStarBoom")));
                    }
                    else if (i > lastIndex && codes[i].Is(OpCodes.Call, target))
                    {
                        realCodes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                        realCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FixValidPatch), "FixBlueStarLoop")));
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                    realCodes = new List<CodeInstruction>(instructions);
                    break;
                }
            }
            foreach (var c in realCodes)
            {
                yield return c;
            }
        }

        private static IEnumerable<CodeInstruction> FixActor(IEnumerable<CodeInstruction> origin, OpCode code, object operand)
        {
            var codes = new List<CodeInstruction>(origin);
            int i = 1;
            while (i < codes.Count) {
                var c = codes[i];
                if (c.opcode == OpCodes.Callvirt)
                {
                    MethodInfo info = c.operand as MethodInfo;
                    if (info.Name.StartsWith("AddKeywordBuf") && info.DeclaringType == typeof(BattleUnitBufListDetail))
                    {
                        if (codes[i - 1].opcode == OpCodes.Ldnull)
                        {
                            // 2, 3, 4, 5 <-- i == 5
                            // 2, 3, 4, 4-1, 5
                            if (code == OpCodes.Ldfld)
                            {
                                codes[i - 1] = new CodeInstruction(code, operand);
                                codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldarg_0));
                                i++;
                            }
                            else
                            {
                                codes[i - 1] = new CodeInstruction(code, operand);
                            }
        
                        }
                    }
                }
                i++;
            }

            foreach (var c in codes)
            {
                yield return c;
            }
        }

        private static SoundEffectPlayer blueStarLoop;

        private static string FixBlueStarBoom(string origin)
        {
            // 다른 모드에서 건드리고있는거니 크게 상관하지 않음
            if (origin != "Creature/BlueStar_Atk") return origin;
            // 아직 재생전임, 무시.
            if (blueStarLoop is null) return origin;
            // 재생시키지 못하도록 음원을 다르게 변경
            return null;
        }

        private static bool FixBlueStarLoop(bool origin, EmotionCardAbility_bluestar3 instance)
        {
            // null이 아닌경우, 다른 모드에서 알아서 할당한경우이므로 넘김
            if (!origin) return origin;
            // 이미 생성함. 무시
            if (blueStarLoop == null)
            {
                blueStarLoop = SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Creature/BlueStar_Bgm", true, 1f, SingletonBehavior<BattleSceneRoot>.Instance.currentMapObject.transform);
                LoA.AddPhaseCallback(StageController.StagePhase.RoundEndPhase, () =>
                {
                    if (blueStarLoop != null)
                    {
                        SingletonBehavior<BattleSoundManager>.Instance.StartBgm();
                        blueStarLoop.ManualDestroy();
                        blueStarLoop = null;
                    }
                }, true);
            }
            instance._loop = blueStarLoop;
            return false;
        }
    }
}
