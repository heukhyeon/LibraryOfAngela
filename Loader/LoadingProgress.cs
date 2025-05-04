using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoALoader
{
    public class LoadingProgress
    {

        public static float modLoadingProgress = 0f;
        public static int loadState = 0;
        public static float elepsed = 0f;
        public static float maxTimeout = 20f;
        public static Action onTimeout = null;

        public static void Patch(Harmony harmony)
        {
            var type = typeof(EntryScene).GetNestedTypes(AccessTools.all).FirstOrDefault(x => x.Name.Contains("LoadingProgress"));
            var method = AccessTools.Method(type, "MoveNext");
            var patchMethod = AccessTools.Method(typeof(LoadingProgress), nameof(LoadingProgress.Trans_LoadingProgress));
            harmony.Patch(method, transpiler: new HarmonyMethod(patchMethod));
        }


        private static IEnumerable<CodeInstruction> Trans_LoadingProgress(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var type = original.DeclaringType;
            var target1 = AccessTools.Method(typeof(Mathf), "Lerp");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Call, target1))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoadingProgress), nameof(HandleCustomLoading)));
                }
            }
        }

        private static float HandleCustomLoading(float origin)
        {
            if (origin < 0.8f) return origin;
            if (modLoadingProgress >= 1f) return origin;
            elepsed += Time.deltaTime;
            if (elepsed >= maxTimeout)
            {
                var logger = new StringBuilder("LoA :: Loading timeout. The loading process probably didn't start properly, so we'll force it to resume. However, the functionality of other LoA modes may not work properly.\n");
                logger.AppendLine($"- LoadStateLevel : {loadState}");
                logger.AppendLine($"- LoadProgress : {modLoadingProgress}");
                Debug.Log(logger.ToString());
                try
                {
                    onTimeout?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.Log("LoA :: Framework OnTimeout Exception");
                    Debug.LogError(e);
                }
                modLoadingProgress = 1f;
                return origin;
            }

            return Mathf.Lerp(0.8f, 0.88f, modLoadingProgress);
        }
    }
}
