using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Save;
using LibraryOfAngela.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UI;
using UnityEngine;

namespace LibraryOfAngela.Implement
{
    class PatcherImpl : IPatcher
    {
        private string packageId;
        private string HarmonyId
        {
            get => "LoA::" + packageId;
        }
        internal HarmonyLib.Harmony harmony;
        internal static List<PatcherImpl> patchers = new List<PatcherImpl>();

        private class PatchInfo
        {
            public Type type;
            public string methodName;
            public MethodBase targetMethod;
            public PatchScope patchScope;
            public MethodBase prefix;
            public MethodBase postfix;
            public MethodBase transpiler;
            public MethodBase finalize;

            public override bool Equals(object obj)
            {
                if (obj is PatchInfo i)
                {
                    return i.type == type && i.methodName == methodName && i.targetMethod == targetMethod &&
                        i.prefix == prefix && i.postfix == postfix && i.finalize == finalize && i.transpiler == transpiler;
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        private List<PatchInfo> patchCache = new List<PatchInfo>();
        public static List<string> patchLogBeforeLoad = new List<string>();

        public PatcherImpl(object key)
        {
            Logger.Log($"Harmony Create Request : {key}");
            var mod = LoAModCache.FromAssembly(key);
            packageId = mod?.packageId;
            if (mod != null) Logger.Log($"LoA Patcher Created, Harmony Id is ( {HarmonyId} )");
            else throw new Exception("Patcher Create Request Without LoAMod!!!");
            patchers.Add(this);
            harmony = new Harmony(HarmonyId);
        }

        public PatcherImpl()
        {
            Logger.Log($"LoA Framework Patcher Created");
            packageId = "LibraryOfAngelaFramework";
            patchers.Add(this);
            harmony = new Harmony(HarmonyId);
        }

        public static void FlushLogs()
        {
            if (patchLogBeforeLoad != null && patchLogBeforeLoad.Count > 0)
            {
                var builder = new StringBuilder("Harmony Patch Log\n");
                patchLogBeforeLoad.ForEach(x => builder.AppendLine(x));
                if (LoAFramework.DEBUG)
                {
                    Logger.Log(builder.ToString());
                }
                patchLogBeforeLoad.Clear();
                patchLogBeforeLoad = null;
            }
        }

        public void PatchTranspiler(Type targetType, Type patchType, string methodName, string patchName, Type[] paramTypes)
        {
            MethodBase targetMethod;

            if (methodName == ".ctor")
            {
                targetMethod = targetType.GetConstructors().First();
            }
            else if (paramTypes is null)
            {
                targetMethod = targetType.GetMethod(methodName, AccessTools.all);
            }
            else
            {
                targetMethod = targetType.GetMethod(methodName, CommonExtension.allFlag, null, paramTypes, null);
            }


            if (targetMethod == null)
            {
                throw new Exception(
                 "LoA :: Target Method Info Not Found ! Please Check !!"
                 + $"Target Info : {targetType.FullName}.{methodName}"
                 + $"Patch Info : {patchType.FullName}.{methodName}");
            }
            var name = $"Trans_{patchName ?? (methodName == ".ctor" ? "Constructor" : methodName)}";
            var methodInfo = patchType.GetMethod(name, AccessTools.all);
            if (methodInfo == null)
            {
                throw new Exception(
                              "LoA :: Patch Transpiler Method Info Not Found ! Please Check !!"
                    + $"Target Info : {targetType.FullName}.{methodName}"
                    + $"Patch Info : {patchType.FullName}.{name}");
            }

            if (Logger.Enabled)
            {
                Logger.Log($"Patch Transpiler ! {targetType.Name}.{targetMethod.Name} / {patchType.Name}.{methodInfo.Name}");
            }
            else
            {
                var builder = new StringBuilder("");
                builder.Append($"- {targetType?.FullName}.{methodName} / {patchType.FullName}.{name} (Transpiler)");
                patchLogBeforeLoad.Add(builder.ToString());
            }
   
            harmony.Patch(targetMethod, transpiler: new HarmonyMethod(methodInfo));
        }

        private void UnPatch(PatchInfo info)
        {
            harmony.Unpatch(info.targetMethod, HarmonyPatchType.All, harmony.Id);
            patchCache.Remove(info);
        }

        public void Unpatch(Type targetType, string methodName, PatchType type)
        {
            var targets = patchCache.Where(x => x.type == targetType && x.methodName == methodName).ToList();
            targets.ForEach(x =>
            {
                UnPatch(x);
            });
        }

        public void Patch(Type targetType, string name, string patchName, Type[] paramTypes, PatchScope scope)
        {
            MethodBase targetMethod;
            if (paramTypes?.Length > 0)
            {
                targetMethod = targetType.GetMethod(name, CommonExtension.allFlag, null, paramTypes, null);
            }
            else
            {
                try
                {
                    targetMethod = targetType.GetMethod(name, CommonExtension.allFlag);
                }
                catch (AmbiguousMatchException e)
                {
                    Logger.Log($"AmbiguousMatchException in {targetType.Name}.{name}");
                    throw e;
                }
                
            }
            if (targetMethod == null && name == ".ctor")
            {
                targetMethod = targetType.GetConstructors(CommonExtension.allFlag).First();
                Logger.Log($"Constructor Check : {targetMethod != null}");
            }
            if (targetMethod == null)
            {
                throw new Exception($"Target Method is Not Exist !! Please Check it : {targetType.Name}.{name}");
            }

            var stack = new StackTrace();
            var caller = stack.GetFrame(1);
            if (caller.GetMethod().DeclaringType != typeof(PatchTextension))
            {
                throw new Exception("Patch(Type, string, Type[]) is Only Supported in Type.Patch Extension!!!");
            }   
            var realCaller = stack.GetFrame(2).GetMethod().DeclaringType;
            if (name == ".ctor") patchName = "Constructor";
            patchName = string.IsNullOrEmpty(patchName) ? name : patchName;
            var prefixMethod =  realCaller.GetMethod("Before_" + patchName, CommonExtension.allFlag);
            var postfixMethod = realCaller.GetMethod("After_" + patchName, CommonExtension.allFlag);
            var finalizeMethod = realCaller.GetMethod("Finalize_" + patchName, CommonExtension.allFlag);
            if (prefixMethod == null && postfixMethod == null && finalizeMethod == null)
            {
                var message = new StringBuilder("Patch Method Not Found!! Please Check it Any Exists :\n");
                message.AppendLine($"- {realCaller.Name}.Before_{patchName}");
                message.AppendLine($"- {realCaller.Name}.After_{patchName}");
                message.AppendLine($"- {realCaller.Name}.Finalize_{patchName}");
                throw new Exception(message.ToString());
            }

            Patch(targetMethod, prefixMethod, postfixMethod, null, finalizeMethod, scope);
        }

        public void PatchInternal(Type targetType, Type callerType, string name, string patchName, Type[] paramTypes, PatchInternalFlag flag)
        {
            MethodBase targetMethod;
            if (paramTypes?.Length > 0)
            {
                targetMethod = targetType.GetMethod(name, CommonExtension.allFlag, null, paramTypes, null);
            }
            else
            {
                try
                {
                    targetMethod = targetType.GetMethod(name, CommonExtension.allFlag);
                }
                catch (AmbiguousMatchException e)
                {
                    Logger.Log($"AmbiguousMatchException in {targetType.Name}.{name}");
                    throw e;
                }

            }
            if (targetMethod == null && name == ".ctor")
            {
                targetMethod = targetType.GetConstructors(CommonExtension.allFlag).First();
                Logger.Log($"Constructor Check : {targetMethod != null}");
            }
            if (targetMethod == null)
            {
                throw new Exception($"Target Method is Not Exist !! Please Check it : {targetType.Name}.{name}");
            }

            if (name == ".ctor") patchName = "Constructor";
            patchName = string.IsNullOrEmpty(patchName) ? name : patchName;
            var prefixMethod = (flag & PatchInternalFlag.PREFIX) == PatchInternalFlag.PREFIX ? callerType.GetMethod("Before_" + patchName, CommonExtension.allFlag) : null;
            var postfixMethod = (flag & PatchInternalFlag.POSTFIX) == PatchInternalFlag.POSTFIX ? callerType.GetMethod("After_" + patchName, CommonExtension.allFlag) : null;
            var transpilerMethod = (flag & PatchInternalFlag.TRANSPILER) == PatchInternalFlag.TRANSPILER ? callerType.GetMethod("Trans_" + patchName, CommonExtension.allFlag) : null;
            var finalizeMethod = (flag & PatchInternalFlag.FINALIZER) == PatchInternalFlag.FINALIZER ? callerType.GetMethod("Finalize_" + patchName, CommonExtension.allFlag) : null;
            if (prefixMethod == null && postfixMethod == null && transpilerMethod == null && finalizeMethod == null)
            {
                var message = new StringBuilder("Patch Method Not Found!! Please Check it Any Exists :\n");
                message.AppendLine($"Target : {targetType.Name}.{name}");
                message.AppendLine($"Flag Status : {(flag & PatchInternalFlag.PREFIX) == PatchInternalFlag.PREFIX} // {(flag & PatchInternalFlag.POSTFIX) == PatchInternalFlag.POSTFIX} // {(flag & PatchInternalFlag.TRANSPILER) == PatchInternalFlag.TRANSPILER} // {(flag & PatchInternalFlag.FINALIZER) == PatchInternalFlag.FINALIZER}");
                message.AppendLine($"- {callerType.Name}.Before_{patchName}");
                message.AppendLine($"- {callerType.Name}.After_{patchName}");
                message.AppendLine($"- {callerType.Name}.Finalize_{patchName}");
                throw new Exception(message.ToString());
            }

            Patch(targetMethod, prefixMethod, postfixMethod, transpilerMethod, finalizeMethod, PatchScope.FORERVER);
        }

        public void ClearInvitationPatch()
        {
            int i = 0;
            while (i < patchCache.Count)
            {
                if (patchCache[i].patchScope == PatchScope.INVITATION) UnPatch(patchCache[i]);
                else i++;
            }
        }

/*        private MethodInfo CreateDynamic(MethodInfo origin)
        {
            if (origin == null) return null;
            var p = origin.GetParameters().Select(x => x.ParameterType).ToArray();
            var m = new DynamicMethod(origin.Name, origin.ReturnType, p);
            var gen = m.GetILGenerator();
            gen.Emit(OpCodes.Ldstr, $"LoA Harmony Called :" + origin.Name);
            gen.Emit(OpCodes.Call, typeof(UnityEngine.Debug).GetMethod("Log", AccessTools.all, null, new Type[] { typeof(string) }, null));
            for (int i = 0; i < p.Length; i++)
            {
                gen.Emit(OpCodes.Ldarg_S, i);
            }
            gen.Emit(OpCodes.Call, origin);
            gen.Emit(OpCodes.Ret);
            return m;
        }*/

        public void Patch(MethodBase target, MethodInfo prefixMethod = null, MethodInfo postfixMethod = null, MethodInfo transpilerMethod = null, MethodInfo finalizeMethod = null, PatchScope scope = PatchScope.FORERVER)
        {
            var info = new PatchInfo
            {
                methodName = target.Name,
                patchScope = scope,
                type = target.DeclaringType,
                targetMethod = target,
                prefix = prefixMethod,
                postfix = postfixMethod,
                transpiler = transpilerMethod,
                finalize = finalizeMethod
            };
            if (patchCache.Contains(info))
            {
                Logger.Log($"{target.DeclaringType}.{target.Name} ({scope}) is Already Patched");
                return;
            }
            patchCache.Add(info);

            if (Logger.Enabled)
            {
                var builder = new StringBuilder("Patch !");
                builder.Append($" {target.DeclaringType?.Name}.{target?.Name}");
                if (prefixMethod != null) builder.Append($" / {prefixMethod.DeclaringType.Name}.{prefixMethod.Name}");
                if (postfixMethod != null) builder.Append($" / {postfixMethod.DeclaringType.Name}.{postfixMethod.Name}");
                builder.Append($" / {scope}");
                Logger.Log(builder.ToString(), false);
            }
            else
            {
                var builder = new StringBuilder("- ");
                builder.Append($"{target.DeclaringType?.Name}.{target?.Name}");
                if (prefixMethod != null) builder.Append($" / {prefixMethod.DeclaringType.Name}.{prefixMethod.Name}");
                if (postfixMethod != null) builder.Append($" / {postfixMethod.DeclaringType.Name}.{postfixMethod.Name}");

                patchLogBeforeLoad.Add(builder.ToString());
            }

            harmony.Patch(target, prefix: prefixMethod.ToHarmonyMethod(), postfix: postfixMethod.ToHarmonyMethod(), transpiler: transpilerMethod.ToHarmonyMethod(), finalizer: finalizeMethod.ToHarmonyMethod(), ilmanipulator: null);
        }

        public void PatchAll(Type targetType)
        {
            harmony.PatchAll(targetType);
        }

        void IPatcher.Patch(MethodBase target, MethodInfo prefixMethod, MethodInfo postfixMethod, MethodInfo finalizeMethod, PatchScope scope)
        {
            Patch(target, prefixMethod: prefixMethod, postfixMethod: postfixMethod, finalizeMethod: finalizeMethod, scope: scope);
        }
    }

    enum PatchInternalFlag
    {
        PREFIX = 1,
        POSTFIX = 2,
        FINALIZER = 4,
        TRANSPILER = 8,
    }

    static class HarmonyExtension
    {
        // private static Stopwatch st = new Stopwatch();
        public static HarmonyMethod ToHarmonyMethod(this MethodInfo method)
        {
            if (method == null) return null;
            //if (method.Name == "After_GetBufIcon") return null;
            //if (method.Name == "After_SetCard") return null;
            return new HarmonyMethod(method);
        }

        
   /*     private static void HarmonyLogPre(MethodBase __originalMethod)
        {
            // if (StageController.Instance._phase == StageController.StagePhase.ApplyLibrarianCardPhase)
            // {
            //Logger.Log($"LoA Harmony Call : {__originalMethod.DeclaringType}.{__originalMethod.Name}");
            st.Restart();
           // }
           // else __state = 0;
        }

        private static void HarmonyLogPost(MethodBase __originalMethod, int __state)
        {
            // if (StageController.Instance._phase == StageController.StagePhase.ApplyLibrarianCardPhase)
            //   {
            st.Stop();
            var elep = st.ElapsedMilliseconds;
            if (elep > 1)
            {
                Logger.Log($"LoA Harmony Call Over : {__originalMethod.DeclaringType}.{__originalMethod.Name} // {elep}");
            }
                
          //  }
        }*/
    }
}
