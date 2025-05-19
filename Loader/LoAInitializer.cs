using HarmonyLib;
using Mod;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace LoALoader
{
    public class LoAInitializer : ModInitializer
    {
        public static LoAInitializer Instance { get; private set; }
        private static bool isInit = false;

        private Harmony harmony;
        internal LoAFastLoadObserver observer;
        public List<ModContentInfo> activatedMods;

        public static string[] whiteListDll = new string[]
        {
            "0Harmony.dll",
            "MonoMod.Common.dll",
            "MonoMod.RuntimeDetour.dll",
            "MonoMod.Utils.dll",
            "Mono.Cecil.Rocks.dll",
            "Mono.Cecil.Pdb.dll",
            "Mono.Cecil.Mdb.dll",
            "Mono.Cecil.dll",
        };

        public LoAInitializer()
        {
            try
            {
                if (Instance != null || isInit)
                {
                    UnityEngine.Debug.Log("LoALoader Already Created But Another Instance Created...??");
                    return;
                }
                Instance = this;
                isInit = true;

                var timeDurations = new List<long>();

                // 1. 초기화
                timeDurations.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds());
                var obj = UnityEngine.Object.FindObjectOfType<EntryScene>();
                var activatedMods = obj.modPopup.dataList.Where(d => d?.IsActivated == true &&
        File.Exists(Path.Combine(d.ModInfo.dirInfo.FullName, "Assemblies", "LoALoader.dll")))
                    .Select(d => d.ModInfo)
                    .ToList();

                // 2. 모드 목록 조회
                timeDurations.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds());
                ModContentInfo latestLoALoaderModContent = null;
                var latestVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var logger = new StringBuilder($"LoALoader Type Loaded, First Loaded Loader Version : {latestVersion}\n");
                foreach (var m in activatedMods)
                {
                    var path = Path.Combine(m.dirInfo.FullName, "Assemblies", "LoALoader.dll");
                    var v = FileVersionInfo.GetVersionInfo(path);
                    var version = new Version(v.FileVersion);
                    if (latestVersion < version)
                    {
                        logger.AppendLine($"- More Latest LoALoader Found From {m.invInfo.workshopInfo.title} : {version}");
                        latestLoALoaderModContent = m;
                        latestVersion = version;
                    }
                }

                // 3. 버전 조회 완료
                timeDurations.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds());

                if (latestLoALoaderModContent is null)
                {
                    logger.AppendLine("First Loaded Loader is Latest. Continue Loader Init");
                    logger.AppendLine(LogDuration(timeDurations));
                    UnityEngine.Debug.Log(logger.ToString());
                    Init(obj, activatedMods);
                }
                else
                {
                    logger.AppendLine($"First Loaded Loader is Not Latest, Load Latest LoALoader in {latestLoALoaderModContent.invInfo.workshopInfo.title}");
                    var asm = Assembly.LoadFile(Path.Combine(latestLoALoaderModContent.dirInfo.FullName, "Assemblies", "LoALoader.dll"));
                    timeDurations.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds());
                    logger.AppendLine(LogDuration(timeDurations));
                    UnityEngine.Debug.Log(logger.ToString());
                    var initializerType = asm.GetType("LoALoader.LoAInitializer")
                        .GetConstructor(new Type[] { typeof(EntryScene), typeof(List<ModContentInfo>) })
                        .Invoke(new object[] { obj, activatedMods });
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("LoALoader Load Fail");
                UnityEngine.Debug.LogError(e);
            }
        }

        private string LogDuration(List<long> durations)
        {
            var logger = new StringBuilder("- Loader Compare Durations\n");
            for (int i = 1; i < durations.Count; i++)
            {
                string key = "";
                switch (i)
                {
                    case 1:
                        key = "Mod List Search Duration";
                        break;
                    case 2:
                        key = "Loader Version Compare Duration";
                        break;
                    case 3:
                        key = "Loader Dll Load Duration";
                        break;
                }
                logger.AppendFormat("- {0} : {1}s\n", key, (durations[i] - durations[i - 1]) / 1000.0);
            }
            return logger.ToString();
        }


        public LoAInitializer(EntryScene scene, List<ModContentInfo> mods)
        {
            Instance = this;
            Init(scene, mods);
        }

        private void Init(EntryScene scene, List<ModContentInfo> mods)
        {
            UnityEngine.Debug.Log($"LoALoader Load Success : {Assembly.GetExecutingAssembly().GetName().Version}");
            EntryObserver.Create(scene);
            this.activatedMods = mods;
            observer = new LoAFastLoadObserver();
            observer.Init();
            harmony = new Harmony("LoALoader");
            harmony.PatchAll(typeof(LoAInitializer));
        }


        [HarmonyPatch(typeof(AssemblyManager), nameof(AssemblyManager.CallAllInitializer))]
        [HarmonyPrefix]
        private static void Before_CallAllInitializer()
        {
            if (Instance is null)
            {
                UnityEngine.Debug.Log($"Failed to initialize LoA Error Level 3 ... Instance is Null... What?!!");
                LoadingProgress.modLoadingProgress = 1f;
                return;
            }

            try
            {
                Instance.harmony.PatchAll(typeof(LatestClearRecord));
                LoadingProgress.Patch(Instance.harmony);
                var errorLogs = ModContentManager.Instance.GetErrorLogs();
                errorLogs.RemoveAll(d => d.Contains("The same assembly name already exists."));
                Instance.observer.IsCalledInitializer();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log($"Failed to initialize LoA Error Level 2");
                UnityEngine.Debug.LogError(e);
                LoadingProgress.modLoadingProgress = 1f;
            }

        }

        [HarmonyPatch(typeof(AssemblyManager), nameof(AssemblyManager.CallAllInitializer))]
        [HarmonyPostfix]
        private static void After_CallInitializer()
        {
            Instance.observer.CallInitializerComplete();
        }

        [HarmonyPatch(typeof(ModContentManager), nameof(ModContentManager.SaveSelectionData))]
        [HarmonyPostfix]
        private static void After_SaveSelectionData()
        {
            Instance.observer.SaveSelectionDataComplete();
        }

        [HarmonyPatch(typeof(ModContentManager), nameof(ModContentManager.AddErrorLog), argumentTypes:typeof(string))]
        [HarmonyPostfix]
        private static void After_AddErrorLog(string msg)
        {
            UnityEngine.Debug.Log(Environment.StackTrace);
        }




        [HarmonyPatch(typeof(ModContent), nameof(ModContent.Loads))]
        [HarmonyPrefix]
        private static bool Before_Loads(ModContent __instance)
        {
            try
            {
                if (Instance?.activatedMods?.Contains(__instance._modInfo) == true)
                {
                    __instance._itemUniqueId = __instance._modInfo.invInfo.workshopInfo.uniqueId;
                    Instance.observer.LoadCommonAssemblies(__instance);
                    return false;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }

            return true;
        }


        [HarmonyPatch(typeof(ModContent), nameof(ModContent.LoadAssemblies))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_LoadAssemblies(IEnumerable<CodeInstruction> instructions)
        {
            var flag = false;
            foreach (var code in instructions)
            {
                if (!flag && (code.opcode == OpCodes.Brtrue_S || code.opcode == OpCodes.Brtrue))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoAInitializer), nameof(HandleIsLoADll)));
                }
                yield return code;
            }
        }

        private static bool HandleIsLoADll(bool origin, ModContent mod, FileInfo file)
        {
            try
            {
                if (origin) return origin;
                if (whiteListDll.Contains(file.Name) || file.Name == "LoALoader.dll") return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Check Mod Is LoA Mod Logic Error...What?!");
                UnityEngine.Debug.LogError(e);
            }
            return origin;
        }
    
    }
}
