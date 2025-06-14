using HarmonyLib;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;
using LibraryOfAngela.SD;
using LoALoader.Model;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using Workshop;
using static LibraryOfAngela.Extension.Framework.FrameworkExtension;

namespace LibraryOfAngela.EquipBook
{
    class RenderCacheData
    {
        public SkinComponentKey key;
        public string packageId;
        public string skinName;
        public string targetName;
        public ClothCustomizeData data;
        public ActionDetail motion;
        public Sprite realSprite;
        public Sprite frontRealSprite;
        public Sprite skinSprite;
        public Sprite frontSkinSprite;
        public bool isAssetBundle;
        public bool hasSkinSprite;
    }

    class BattleAssetBundle
    {
        public bool isLoaded;
        public bool isRecyclable;
        public AssetBundleInfo info;
        public string skin;
    }

    struct SkinComponentKey
    {
        public string packageId;
        public string skinName;


        public override string ToString()
        {
            return $"LoASkinComponentKey :({packageId} // {skinName})";
        }
    }


    class SkinRenderPatch : Singleton<SkinRenderPatch>
    {


        private Dictionary<string, List<BattleAssetBundle>> recyclableBundles;
        private Dictionary<string, Dictionary<ActionDetail, string>> motionSfxReplaceDic = new Dictionary<string, Dictionary<ActionDetail, string>>();
        private Dictionary<SkinComponentKey, string> prefabs = new Dictionary<SkinComponentKey, string>();
        private HashSet<SkinComponentKey> skinSets;

        public void Initialize() {
            var str = new StringBuilder("Skin Render Target\n");
            int cnt = 0;
            var totalDic = new Dictionary<SkinComponentKey, List<RenderCacheData>>();
            foreach (var c in LoAModCache.Instance.Where(x => x.ArtworkConfig != null))
            {
                var config = c.ArtworkConfig;
                foreach (var bundleInfo in c?.AssetBundleConfig?.GetAssetBundleInfos() ?? new List<AssetBundleInfo>())
                {
                    var count = bundleInfo.types?.Length ?? 0;
                    for (int i = 0; i < count + 1; i++)
                    {
                        var target = i == 0 ? bundleInfo.type : bundleInfo.types[i - 1];
                        var sd = target as AssetBundleType.Sd;
                        if (sd is null) continue;

                        if (recyclableBundles is null) recyclableBundles = new Dictionary<string, List<BattleAssetBundle>>();
                        if (!recyclableBundles.ContainsKey(sd.skin)) recyclableBundles[sd.skin] = new List<BattleAssetBundle>();
                        recyclableBundles[sd.skin].Add(new BattleAssetBundle { info = bundleInfo, isLoaded = false, isRecyclable = sd.isOnlyBattle, skin = sd.skin });
                    }
                }

                if (!CustomizingBookSkinLoader.Instance._bookSkinData.ContainsKey(c.packageId)) continue;

                var originSkins = CustomizingBookSkinLoader.Instance._bookSkinData[c.packageId];
                
                for (int i = 0; i < originSkins.Count; i++)
                {
                    if (skinSets is null) skinSets = new HashSet<SkinComponentKey>();

                    var skin = originSkins[i];
                    var key = new SkinComponentKey { packageId = skin.contentFolderIdx, skinName = skin.dataName };

                    if (skin is LoAWorkshopSkinData d && !string.IsNullOrEmpty(d.prefab))
                    {
                        Logger.Log($"Prefab Detect :: {key} // {d.prefab}");
                        prefabs[key] = d.prefab;
                        originSkins.Remove(skin);
                        i--;
                        continue;
                    }

                    var hasSkinSprite = AdvancedSkinInfoPatch.Instance.infos.SafeGet(skin.dataName)?.hasSkinSprite ?? false;
                    var targetInfo = AdvancedSkinInfoPatch.Instance.infos.SafeGet(skin.dataName);
      
                    var items = new List<RenderCacheData>();
                    skinSets.Add(key);
                    str.Append($"- {skin.contentFolderIdx} : {skin.dataName}");
                    var hasWorkshopSkin = GetSafeAction(() => targetInfo?.exportWorkshopSkinMatchedId) != null;
                    var workshopName = hasWorkshopSkin ? WorkshopSkinExportPatch.GetModSkinDataName(targetInfo.packageId, targetInfo.skinName) : string.Empty;

                    if (GetSafeAction(() => targetInfo?.exportWorkshopSkinMatchedId) != null)
                    {
                        str.Append("(Include WorkshopSkin)");
                    }
                    str.Append("\n");
                    cnt++;

                    if (targetInfo?.audioReplace != null)
                    {
                        motionSfxReplaceDic[skin.dataName] = new Dictionary<ActionDetail, string>();
                    }

                    foreach (var motion in skin.dic)
                    {
                        var target = GetSafeAction(() => config.ConvertMotionSprite(skin.dataName, motion.Key));
                        if (target == null)
                        {
                            target = GetSafeAction(() => config.ConvertMotionSprite(skin.dataName, ActionDetail.Default));
                            if (target == null) continue;
                        }
                        var originSpritePath = string.IsNullOrEmpty(motion.Value.spritePath) ? motion.Value.frontSpritePath : motion.Value.spritePath;
                        items.Add(new RenderCacheData
                        {
                            key = new SkinComponentKey { packageId = skin.contentFolderIdx, skinName = skin.dataName },
                            packageId = c.packageId,
                            data = motion.Value,
                            isAssetBundle = target.isAssetBundle,
                            hasSkinSprite = hasSkinSprite,
                            skinName = skin.dataName,
                            motion = motion.Key,
                            targetName = target.isAssetBundle ? target.skinName : originSpritePath
                        });
                        motion.Value.spritePath = "";
                        motion.Value.frontSpritePath = "";
                        motion.Value.hasSpriteFile = false;
                        motion.Value.hasFrontSpriteFile = false;
                        var sfxTarget = targetInfo?.audioReplace?.Invoke(motion.Key);
                        if (sfxTarget != null) motionSfxReplaceDic[skin.dataName][motion.Key] = sfxTarget;
                    }

                    totalDic[key] = items;
                }
            }


            LoASDTarget.skinSet = totalDic;
            if (totalDic.Count > 0)
            {
                InternalExtension.SetRange(GetType());
            }

            if (cnt > 0 && LoAFramework.DEBUG)
            {
                Logger.Log(str.ToString());
            }
        }

        public static void RemoveCheck(string skinName) {
            var bundleInfo = Instance.recyclableBundles?.SafeGet(skinName);
            if (bundleInfo != null)
            {
                int index = 0;
                var isBattle = Singleton<StageController>.Instance.State == StageController.StageState.Battle && GameSceneManager.Instance.battleScene.gameObject.activeSelf;
                while (true)
                {
                    if (index >= bundleInfo.Count) break;
                    var current = bundleInfo[index];
                    if (current.isLoaded || (current.isRecyclable && !isBattle))
                    {
                        index++;
                        continue;
                    }
                    Logger.Log($"Skin Try Load :: {current.skin} // {current.info.path} // {isBattle} // {current.isRecyclable}");
                    LoAAssetBundles.Instance.LoadAssetBundle(new AssetBundleType.Sd(current.skin) { isOnlyBattle = current.isRecyclable });
                    current.isLoaded = true;
                    if (!current.isRecyclable)
                    {
                        bundleInfo.Remove(current);
                    }
                    else
                    {
                        index++;
                    }
                }
                if (bundleInfo.Count == 0) Instance.recyclableBundles.Remove(skinName);
            }
        }

        [HarmonyPatch(typeof(WorkshopSkinDataSetter), "SetData", new Type[] { typeof(WorkshopSkinData) })]
        [HarmonyPrefix]
        public static void Before_SetData(WorkshopSkinDataSetter __instance, WorkshopSkinData data, out LoASDTarget __state) 
        {
            try
            {
                if (LoASdTargetDictionary.Instance[__instance] != null)
                {
                    __state = null;
                    return;
                }
                var target = LoASDTarget.Create(__instance, data);
                __state = target;

                if (target is null) return;
                LoASdTargetDictionary.Instance.Add(target);
                target.InitSMotionObj();
                target.InitSfxTargets(Instance.motionSfxReplaceDic.SafeGet(target.skinName));
                return;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            __state = null;
        }

        [HarmonyPatch(typeof(WorkshopSkinDataSetter), "SetData", new Type[] { typeof(WorkshopSkinData) })]
        [HarmonyPostfix]
        public static void After_SetData(LoASDTarget __state) {

            if (__state is null) return;

            RemoveCheck(__state.skinName);
            __state.LateInit();
            if (!LoASpriteLoader.isAsyncLoad)
            {
                LoASpriteLoader.LoadSpriteAsync();
            }

        }

        public static bool IsLoAPrefabCharacter(UnitDataModel unit)
        {
            var bookItem = unit.CustomBookItem;
            if (!bookItem.IsWorkshop || unit.bookItem.ClassInfo.skinType != "Custom")
            {
                return false;
            }
            var key = new SkinComponentKey { packageId = bookItem.ClassInfo.workshopID, skinName = bookItem.GetOriginalCharcterName() };
            var prefab = Instance.prefabs.SafeGet(key);
            if (string.IsNullOrEmpty(prefab))
            {
                return false;
            }
            return true;
        }

        public static GameObject LoadLoAPrefab(UnitDataModel unit, bool ready)
        {
            if (!IsLoAPrefabCharacter(unit))
            {
                return null;
            }
            var bookItem = unit.CustomBookItem;
            var key = new SkinComponentKey { packageId = bookItem.ClassInfo.workshopID, skinName = bookItem.GetOriginalCharcterName() };
            RemoveCheck(key.skinName);
            var obj = LoAAssetBundles.Instance.LoadAsset<GameObject>(key.packageId, Instance.prefabs[key] + (ready ? "_Ready" : ""), false);
            if (obj is null || ready) return obj;
            var component = AdvancedSkinInfoPatch.Instance.skinComponentTypes.SafeGet(key);
            if (component is null) return obj;
            obj.AddComponent(component);
            return obj;
        }

        [HarmonyPatch(typeof(SdCharacterUtil), "LoadAppearance")]
        [HarmonyPrefix]
        private static bool Before_LoadAppearance(UnitDataModel unit, out string resourceName, ref GameObject __result)
        {
            try
            {
                var p = LoadLoAPrefab(unit, false);
                if (p is null)
                {
                    resourceName = "";
                    return true;
                }
                resourceName = unit.CustomBookItem.GetOriginalCharcterName();
                __result = p;
                return false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            resourceName = "";
            return true;
        }

        [HarmonyPatch(typeof(CharacterSound), "PlaySound")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_PlaySound(IEnumerable<CodeInstruction> instructions) {
            var target = typeof(Dictionary<MotionDetail, CharacterSound.Sound>).GetMethod("ContainsKey", AccessTools.all);

            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkinRenderPatch), nameof(FixValidSound)));
                }
            }
        }

        public static void RestoreRecycleableBundles() {
            if (Instance.recyclableBundles != null)
            {
                foreach (var pair in Instance.recyclableBundles.Values.SelectMany(x => x))
                {
                    pair.isLoaded = false;
                }
            }
            LoASdTargetDictionary.Instance.Clear();
        }

        [HarmonyPatch(typeof(CharacterAppearance), "ChangeMotion")]
        [HarmonyPrefix]
        private static void Before_ChangeMotion(ref ActionDetail detail, CharacterAppearance __instance, out LoASDTarget __state)
        {
            try
            {
                __state = LoASdTargetDictionary.Instance[__instance];
                if (__state is null) return;
                var d = __state.ConvertMotion(detail);
                detail = d;
            }
            catch (Exception e)
            {
                __state = null;
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(CharacterAppearance), "ChangeMotion")]
        [HarmonyPostfix]
        private static void After_ChangeMotion(ActionDetail detail, LoASDTarget __state)
        {
            __state?.ChangeMotion(detail);
        }

        [HarmonyPatch(typeof(CharacterAppearance), "ChangeLayer")]
        [HarmonyPostfix]
        private static void After_ChangeLayer(string layerName, CharacterAppearance __instance) {
            LoASdTargetDictionary.Instance[__instance]?.OnLayerChanged(layerName);
            try
            {
                if (__instance is LoACharacterApperance p)
                {
                    p.UpdateLayer(layerName);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static bool FixValidSound(bool origin, MotionDetail motion, CharacterSound instance) {
            if (origin) return origin;
            try
            {
                var target = LoASdTargetDictionary.Instance[instance];
                if (target is null) return origin;
                var m = target.ConvertMotion(MotionConverter.MotionToAction(motion));
                var targetSfx = Instance.motionSfxReplaceDic?.SafeGet(target.key.skinName)?.SafeGet(m);
                if (targetSfx is null) return origin;
                var clip = LoAModCache.Instance[target.key.packageId].AssetBundles.LoadManullay<AudioClip>(targetSfx, true);
                instance._dic.Add(motion, new CharacterSound.Sound
                {
                    motion = motion,
                    winSound = clip
                });
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            return origin;

        }


    }
}
