using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    // 외부 모드에 노출됨. 
    public static class Patcher
    {
        public static void Unpatch(Type targetType, string methodName, PatchType type)
        {
            ServiceLocator.Instance.CreateInstance<IPatcher>(GetCallerAssembly()).Unpatch(targetType, methodName,  type);
        }

        public static void PatchTranspiler(Type targetType, Type patchType, string methodName, string patchName = null, Type[] types = null)
        {
            ServiceLocator.Instance.CreateInstance<IPatcher>(GetCallerAssembly()).PatchTranspiler(targetType, patchType, methodName, patchName, types);
        }

        private static Assembly GetCallerAssembly()
        {
            StackTrace stacktrace = new StackTrace();
            var caller = stacktrace.GetFrame(2);
            return caller.GetMethod().DeclaringType.Assembly;
        }
    }

    public enum PatchType
    {
        PREFIX,
        POSTFIX
    }
    public enum PatchScope
    {
        FORERVER,
        INVITATION
    }
}
