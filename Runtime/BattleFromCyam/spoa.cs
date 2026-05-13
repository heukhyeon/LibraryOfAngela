

using HarmonyLib;
using HarmonyLib.Public.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static System.Reflection.Emit.OpCodes;


internal class ReversePatch<T>
{
    static Harmony reverseHarmony;
    static bool reversePatched = false;
    protected static MethodInfo targetMethod;
    protected static bool useSnapshot = true;
    static Patch[] snapshotPatches = new Patch[0];

    public static void RecheckReverse()
    {
        var snap = PatchManager.GetPatchInfo(targetMethod)?.transpilers ?? new Patch[0];
        if (!reversePatched)
        {
            DoReversePatch();
            snapshotPatches = snap;
        }
        else
        {
            if (!Enumerable.SequenceEqual(snapshotPatches, snap))
            {
                reverseHarmony.UnpatchSelf();
                DoReversePatch();
                snapshotPatches = snap;
            }
        }
    }

    static void DoReversePatch()
    {
        reverseHarmony = reverseHarmony ?? new Harmony("Cyaminthe.GLHF.ReversePatches." + typeof(T).Name);
        foreach (var method in typeof(T).GetMethods(all))
        {
            var target = method.GetCustomAttributes<HarmonyPatch>()?.FirstOrDefault();
            if (target != null)
            {
                var targetMethod = Method(target.info.declaringType, target.info.methodName, target.info.argumentTypes);
                if (targetMethod != null)
                {
                    var patcher = reverseHarmony.CreateReversePatcher(targetMethod, new HarmonyMethod(method));
                    if (useSnapshot && Harmony.GetPatchInfo(targetMethod) != null)
                    {
                        patcher.Patch(HarmonyReversePatchType.Snapshot);
                    }
                    else
                    {
                        patcher.Patch(HarmonyReversePatchType.Original);
                    }
                }
            }
        }
        reversePatched = true;
    }
}

internal class StartParryingOrAction : ReversePatch<StartParryingOrAction>
{
    static StartParryingOrAction()
    {
        targetMethod = Method(typeof(StageController), nameof(StageController.WaitUnitArrivePhase));
    }

    public static void Invoke(StageController controller, BattlePlayingCardDataInUnitModel card, bool skipBasicResponseSelect = false, BattlePlayingCardDataInUnitModel basicResponse = null)
    {
        try
        {
            RecheckReverse();
            card.owner.currentDiceAction = card;
            Stub(controller, Time.fixedDeltaTime, card.owner, skipBasicResponseSelect, basicResponse);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [HarmonyPatch(typeof(StageController), nameof(StageController.WaitUnitArrivePhase))]
    [HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Stub(StageController controller, float deltaTime, BattleUnitModel actingUnit, bool skipBasicResponseSelect, BattlePlayingCardDataInUnitModel basicResponse)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilgen)
        {
            if (instructions == null)
            {
                return null;
            }
            var codes = instructions.ToList();
            var unitListCount = PropertyGetter(typeof(List<BattleUnitModel>), nameof(List<BattleUnitModel>.Count));
            var firstCountIndex = codes.FindIndex(x => x.Calls(unitListCount));
            if (firstCountIndex < 0)
            {
                Debug.LogError("GLHF: REVERSE PATCH ERROR: COULD NOT EXTRACT STARTPARRYING/STARTACTION BLOCK");
                return new CodeInstruction[] { new CodeInstruction(Ret) };
            }
            var preFirstAccessor = codes.FindLastIndex(firstCountIndex, x => x.opcode == Ldloc_1);
            if (preFirstAccessor < 0)
            {
                Debug.LogError("GLHF: REVERSE PATCH ERROR: COULD NOT EXTRACT STARTPARRYING/STARTACTION BLOCK");
                return new CodeInstruction[] { new CodeInstruction(Ret) };
            }
            var loadUnitIndex = codes.FindIndex(c => c.opcode == Ldfld && c.operand is FieldInfo field && field.FieldType == typeof(BattleUnitModel) && field.Name == "arrivedUnit");
            if (loadUnitIndex < 0)
            {
                Debug.LogError("GLHF: REVERSE PATCH ERROR: COULD NOT EXTRACT STARTPARRYING/STARTACTION BLOCK");
                return new CodeInstruction[] { new CodeInstruction(Ret) };
            }
            var loadContainerIndex = codes.FindLastIndex(loadUnitIndex, c => c.opcode == Ldloc_0);
            if (loadContainerIndex < 0 || loadContainerIndex <= preFirstAccessor)
            {
                Debug.LogError("GLHF: REVERSE PATCH ERROR: COULD NOT EXTRACT STARTPARRYING/STARTACTION BLOCK");
                return new CodeInstruction[] { new CodeInstruction(Ret) };
            }
            codes.InsertRange(loadContainerIndex, new CodeInstruction[]
            {
                        new CodeInstruction(Ldloc_0).MoveLabelsFrom(codes[loadContainerIndex]).MoveLabelsFrom(codes[preFirstAccessor]),
                        new CodeInstruction(Ldarg_2),
                        new CodeInstruction(Stfld, codes[loadUnitIndex].operand)
            });
            codes.RemoveRange(preFirstAccessor, loadContainerIndex - preFirstAccessor);

            var destroyedGetter = PropertyGetter(typeof(BattlePlayingCardDataInUnitModel), nameof(BattlePlayingCardDataInUnitModel.isDestroyed));
            var areaSkipHelper = Method(typeof(StartParryingOrAction), nameof(StartParryingOrAction.SkipAreaCard));
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(destroyedGetter))
                {
                    codes.Insert(i, new CodeInstruction(Dup));
                    codes.Insert(i + 2, new CodeInstruction(Call, areaSkipHelper));
                    i += 2;
                }
            }

            var responseGotSetterIndex = codes.FindIndex(c => c.IsStloc(20));
            var exceptionBlockStartIndex = codes.FindIndex(responseGotSetterIndex + 1, c => c.blocks.Exists(b => b.blockType == ExceptionBlockType.BeginExceptionBlock));
            var exceptionBlockEndIndex = codes.FindIndex(exceptionBlockStartIndex + 1, c => c.blocks.Exists(b => b.blockType == ExceptionBlockType.EndExceptionBlock));

            if (responseGotSetterIndex < 0 || exceptionBlockStartIndex < 0 || exceptionBlockEndIndex < 0)
            {
                Debug.LogError("GLHF: REVERSE PATCH ERROR: COULD NOT IDENTIFY BASIC ACTION RESPONSE BLOCK FOR SKIPPING");
                return codes;
            }

            if (codes[exceptionBlockEndIndex].opcode != Nop)
            {
                codes.Insert(exceptionBlockEndIndex, new CodeInstruction(Nop).MoveBlocksFrom(codes[exceptionBlockEndIndex]));
            }

            var condLabel = ilgen.DefineLabel();
            var uncondLabel = ilgen.DefineLabel();
            codes[exceptionBlockEndIndex + 1].labels.Add(uncondLabel);
            var exceptionBlockStartCode = codes[exceptionBlockStartIndex];

            codes.InsertRange(exceptionBlockStartIndex, new CodeInstruction[]
            {
                            new CodeInstruction(Ldarg_3),
                            new CodeInstruction(Brfalse, condLabel),
                            new CodeInstruction(Ldarg_S, (byte)4),
                            new CodeInstruction(Dup),
                            new CodeInstruction(Stloc_S, (byte)19),
                            new CodeInstruction(Ldnull),
                            new CodeInstruction(Ceq),
                            new CodeInstruction(Ldc_I4_0),
                            new CodeInstruction(Ceq),
                            new CodeInstruction(Stloc_S, (byte)20),
                            new CodeInstruction(Br, uncondLabel)
            });
            exceptionBlockStartCode.labels.Add(condLabel);

            return codes;
        }

        _ = Transpiler(null, null);
        _ = controller;
        _ = deltaTime;
        _ = actingUnit;
        _ = skipBasicResponseSelect;
        _ = basicResponse;
    }

    static bool SkipAreaCard(BattlePlayingCardDataInUnitModel card, bool isDestroyed)
    {
        if (isDestroyed)
        {
            return true;
        }
        var range = card.card.GetSpec().Ranged;
        return range == LOR_DiceSystem.CardRange.FarArea || range == LOR_DiceSystem.CardRange.FarAreaEach;
    }

    

}



internal static class CodeExtensions
{
    internal static bool IsLdloc(this CodeInstruction instruction, int index)
    {
        switch (index)
        {
            case 0:
                if (instruction.opcode == OpCodes.Ldloc_0)
                {
                    return true;
                }
                break;
            case 1:
                if (instruction.opcode == OpCodes.Ldloc_1)
                {
                    return true;
                }
                break;
            case 2:
                if (instruction.opcode == OpCodes.Ldloc_2)
                {
                    return true;
                }
                break;
            case 3:
                if (instruction.opcode == OpCodes.Ldloc_3)
                {
                    return true;
                }
                break;
        }
        return (instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S) &&
            (instruction.operand is IConvertible i && i.ToInt32(null) == index || instruction.operand is LocalBuilder local && local.LocalIndex == index);
    }

    internal static bool IsStloc(this CodeInstruction instruction, int index)
    {
        switch (index)
        {
            case 0:
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    return true;
                }
                break;
            case 1:
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    return true;
                }
                break;
            case 2:
                if (instruction.opcode == OpCodes.Stloc_2)
                {
                    return true;
                }
                break;
            case 3:
                if (instruction.opcode == OpCodes.Stloc_3)
                {
                    return true;
                }
                break;
        }
        return (instruction.opcode == OpCodes.Stloc || instruction.opcode == OpCodes.Stloc_S) &&
            (instruction.operand is IConvertible i && i.ToInt32(null) == index || instruction.operand is LocalBuilder local && local.LocalIndex == index);
    }

    internal static OpCode MakeStloc(this CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldloc_0)
        {
            return OpCodes.Stloc_0;
        }
        if (instruction.opcode == OpCodes.Ldloc_1)
        {
            return OpCodes.Stloc_1;
        }
        if (instruction.opcode == OpCodes.Ldloc_2)
        {
            return OpCodes.Stloc_2;
        }
        if (instruction.opcode == OpCodes.Ldloc_3)
        {
            return OpCodes.Stloc_3;
        }
        if (instruction.opcode == OpCodes.Ldloc_S)
        {
            return OpCodes.Stloc_S;
        }
        if (instruction.opcode == OpCodes.Ldloc)
        {
            return OpCodes.Stloc;
        }
        return default;
    }

    internal static OpCode MakeLdloc(this CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Stloc_0)
        {
            return OpCodes.Ldloc_0;
        }
        if (instruction.opcode == OpCodes.Stloc_1)
        {
            return OpCodes.Ldloc_1;
        }
        if (instruction.opcode == OpCodes.Stloc_2)
        {
            return OpCodes.Ldloc_2;
        }
        if (instruction.opcode == OpCodes.Stloc_3)
        {
            return OpCodes.Ldloc_3;
        }
        if (instruction.opcode == OpCodes.Stloc_S)
        {
            return OpCodes.Ldloc_S;
        }
        if (instruction.opcode == OpCodes.Stloc)
        {
            return OpCodes.Ldloc;
        }
        return default;
    }
}