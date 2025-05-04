using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Util
{
    internal static class HarmonyExtension
    {
        public static int GetIndex(this CodeInstruction code, OpCode opCode)
        {
            if (code.opcode != opCode) return -1;
            var operand = code.operand as LocalBuilder;
            return operand?.LocalIndex ?? -1;
        }
    }
}
