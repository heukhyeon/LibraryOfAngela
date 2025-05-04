using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace LibraryOfAngela.BattleUI
{
    class EmotionOver5
    {
        public static void Initialize()
        {

        }

        [HarmonyPatch(typeof(BattleCharacterProfileEmotionUI), "Init")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_Init(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            int start = -1;
            var additionalCodes = new Queue<CodeInstruction>();
            var field1 = AccessTools.Field(typeof(BattleCharacterProfileEmotionUI), "txt_emotionLv_Next");
            var field2 = AccessTools.Field(typeof(BattleCharacterProfileEmotionUI), "txt_emotionLv_Prev");
            var method = AccessTools.Method(typeof(TMP_Text), "set_text");
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                try
                {
                    if (code.opcode == OpCodes.Switch)
                    {
                        start = (code.operand as Label[]).Length;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
                if (start > 5 && start < 10)
                {
                    var beforeArr = (code.operand as Label[]);
                    var labels = Enumerable.Range(0, 11).Select(x =>
                    {
                        if (x < start) return beforeArr[x];
                        else return default(Label);
                    }).ToArray();
                    var strs = new string[] { "VI", "VII", "VII", "IX", "X" };
                    for (int j = start; j < 11; j++)
                    {
                        var label = generator.DefineLabel();
                        var first = new CodeInstruction(OpCodes.Ldarg_0).WithLabels(label);
                        additionalCodes.Enqueue(first);
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ldfld, field1));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ldstr, strs[j - 6]));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Callvirt, method));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ldarg_0));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ldfld, field2));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ldstr, strs[j - 6]));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Callvirt, method));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ret));
                        labels[j] = label;
                    }
                    start = -1;
                    code.operand = labels;
                }
                yield return code;
            }
            foreach (var c in additionalCodes)
            {
                yield return c;
            }
        }

        [HarmonyPatch(typeof(BattleCharacterProfileEmotionUI), "UpdateEmotion")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_UpdateEmotion(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            int start = -1;
            var additionalCodes = new Queue<CodeInstruction>();
            var field1 = AccessTools.Field(typeof(BattleCharacterProfileEmotionUI), "txt_emotionLv_Next");
            var field3 = AccessTools.Field(typeof(BattleCharacterProfileEmotionUI), "Anim");
            var method = AccessTools.Method(typeof(TMP_Text), "set_text");
            Label brLabel = generator.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Is(OpCodes.Ldfld, field3))
                {
                    codes[i - 1] = codes[i - 1].WithLabels(brLabel);
                    break;
                }
            }

            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                try
                {
                    if (code.opcode == OpCodes.Switch)
                    {
                        start = (code.operand as Label[]).Length;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
                if (start > 5 && start < 10)
                {
                    var beforeArr = (code.operand as Label[]);
                    var labels = Enumerable.Range(0, 11).Select(x =>
                    {
                        if (x < start) return beforeArr[x];
                        else return default(Label);
                    }).ToArray();
                    var strs = new string[] { "VI", "VII", "VII", "IX", "X" };
                    for (int j = start; j < 11; j++)
                    {
                        var label = generator.DefineLabel();
                        var first = new CodeInstruction(OpCodes.Ldarg_0).WithLabels(label);
                        additionalCodes.Enqueue(first);
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ldfld, field1));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ldstr, strs[j - 6]));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Callvirt, method));
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Br_S, brLabel));
                        labels[j] = label;
                    }
                    start = -1;
                    code.operand = labels;
                }
                yield return code;
            }
            foreach (var c in additionalCodes)
            {
                yield return c;
            }
        }

        [HarmonyPatch(typeof(BattleUnitEmotionDetail), "MaxPlayPointAdderByLevel")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_MaxPlayPointAdderByLevel(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            int start = -1;
            var additionalCodes = new Queue<CodeInstruction>();
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                try
                {
                    if (code.opcode == OpCodes.Switch)
                    {
                        start = (code.operand as Label[]).Length;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
                if (start > 5 && start < 10)
                {
                    var beforeArr = (code.operand as Label[]);
                    var labels = Enumerable.Range(0, 11).Select(x =>
                    {
                        if (x < start) return beforeArr[x];
                        else return default(Label);
                    }).ToArray();
                    for (int j = start; j < 11; j++)
                    {
                        var label = generator.DefineLabel();
                        var first = new CodeInstruction(OpCodes.Ldc_I4_S, j).WithLabels(label);
                        additionalCodes.Enqueue(first);
                        additionalCodes.Enqueue(new CodeInstruction(OpCodes.Ret));
                        labels[j] = label;
                    }
                    start = -1;
                    code.operand = labels;
                }
                yield return code;
            }
            foreach (var c in additionalCodes)
            {
                yield return c;
            }
        }
    }
}
