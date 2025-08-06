using HarmonyLib;
using LibraryOfAngela.Implement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela
{
    public static class InternalExtension
    {
        public static int GetIndex(this CodeInstruction owner)
        {
            if (owner == null) return -1;
            if (owner.opcode != OpCodes.Ldloc_S) return -1;
            return (owner.operand as LocalBuilder)?.LocalIndex ?? -1;
        }

        private static Type targetType = null;
        internal static PatcherImpl internalPatcher = new PatcherImpl();

        public static void SetRange(this Type type)
        {
            targetType = type;
        }

        internal static Type PatchInternal(this Type type, string name, PatchInternalFlag flag, string patchName = "", params Type[] paramTypes)
        {
            try
            {
                internalPatcher.PatchInternal(type, targetType, name, patchName, paramTypes, flag);
                return type;
            }
            catch (Exception e)
            {
                Logger.Log($"Exception in PatchInternal, Target Patch : {type.Name}.{name} (patchName : {patchName})");
                Logger.LogError(e);
                return type;
            }

        }

        public static void PatchEnd()
        {
            targetType = null;
        }
    }
}
