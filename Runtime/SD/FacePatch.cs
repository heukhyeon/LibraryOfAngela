using HarmonyLib;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;

namespace LibraryOfAngela.SD
{
    class FacePatch
    {
        private static Dictionary<string, AdvancedSkinInfo> faceTargets = new Dictionary<string, AdvancedSkinInfo>();

        public static void Initialize(List<AdvancedSkinInfo> infos)
        {
            var targets = infos.Where(x => (x.overrideFaceSprite != null && x.overrideFaceSprite.Length > 0) || (x.overrideFace != null));
            if (targets.Count() > 0)
            {
                faceTargets = new Dictionary<string, AdvancedSkinInfo>();
                foreach(var t in targets)
                {
                    faceTargets[t.skinName] = t;
                }
            }
        }

        /// <summary>
        /// if (info.unit.isSephirah)
        /// 
        /// ->
        /// 
        /// if (!FacePatch.IsLoACustomFace(this, info)) {
        ///   if (info.unit.isSephirah)
        /// }
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UICardEquipInfoSlot), "SetSlotActive")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetSlotActive(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(TMP_Text), "set_text");
            var endTarget = AccessTools.Method(typeof(FaceEditor), "Init");
            List<Label> endLabel = null;
            var codes = new List<CodeInstruction>(instructions);
            // FaceEditor.Init 다음으로 보내기 위해 해당 IL 블록 다음의 라벨을 추출한다.
            for (int i = 0; i< codes.Count; i++)
            {

                if (codes[i].Is(OpCodes.Callvirt, endTarget))
                {
                    endLabel = codes[i + 1].labels;
                    Logger.Log($"LoA UICardEquipInfoSlot If Condition Label Find :: {(i + 1)} /// {codes[i + 1].opcode}");
                    break;
                }
            }
            if (endLabel == null)
            {
                Logger.Log("LoA UICardEquipInfoSlot If Condition Label Not Found");
                foreach (var code in instructions)
                {
                    yield return code;
                }
                yield break;
            }

            bool fired = false;
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (!fired && codes[i].Is(OpCodes.Callvirt, target))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FacePatch), "IsLoACustomFace"));
                    yield return new CodeInstruction(OpCodes.Brtrue_S, endLabel[0]); // target line label
                }
            }
        }

        private static bool IsLoACustomFace(UICardEquipInfoSlot slot, CardOwnResult info)
        {
            try
            {
                var key = info.unit.CustomBookItem._characterSkin;
                if (faceTargets.ContainsKey(key))
                {
                    slot.faceEditor.InitBySephirah(new LorId(16));
                    var artwork = LoAModCache.Instance[faceTargets[key].packageId].Artworks;
                    var sp = artwork.GetNullable(faceTargets[key].overrideFaceSprite + "_setting");
                    if (sp == null)
                    {
                        sp = artwork.GetNullable(faceTargets[key].overrideFace.Invoke(key, key, info.unit).GetSettingFaceArtwork());
                    }
                    if (sp == null) return false;
                    slot.faceEditor.head.sprite = sp;
                    slot.faceEditor.head.enabled = true;
                    slot.faceEditor.head.color = new UnityEngine.Color(1f, 1f, 1f, 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return false;
        }

        public static UnitCustomizingData HandleLoAFaceBase(UnitCustomizingData origin, UnitDataModel owner, string currentSkinName)
        {
            try
            {
                var skin = owner.workshopSkin;
                if (string.IsNullOrEmpty(skin)) skin = owner.CustomBookItem.GetCharacterName();
                if (string.IsNullOrEmpty(currentSkinName)) currentSkinName = skin;
                currentSkinName = SkinInfoProvider.ConvertValidSkinName(currentSkinName, owner);
                var corePage = owner.bookItem.BookId;
                var skinInfo = AdvancedSkinInfoPatch.Instance.infos.SafeGet(skin);
                var faceBySkin = skinInfo?.overrideFace?.Invoke(currentSkinName, skin, owner);
                if (faceBySkin == null)
                {
                    faceBySkin = AdvancedSkinInfoPatch.Instance.infos.SafeGet(currentSkinName)?.overrideFace?.Invoke(currentSkinName, skin, owner);
                }

                var faceByCorePage = AdvancedEquipBookPatch.Instance.infos.SafeGet(corePage)?.overrideFace?.Invoke(currentSkinName, skin, owner);
                if (faceBySkin != null)
                {
                    faceBySkin.packageId = skinInfo.packageId;
                    faceBySkin._bUseCustomData = faceBySkin.IsDestroyOriginalFace(skin);
                    return faceBySkin;
                }

                if (faceByCorePage != null)
                {
                    faceByCorePage.packageId = corePage.packageId;
                    faceByCorePage._bUseCustomData = faceByCorePage.IsDestroyOriginalFace(skin);
                    return faceByCorePage;
                }
                if (skinInfo?.overrideFaceSprite != null)
                {
                    var f = new LegacyLoAFaceData(skinInfo);
                    f._bUseCustomData = f.IsDestroyOriginalFace(skin);
                    return f;
                }
                if (SkinInfoProvider.Instance.isPropertyEnabled(owner, SkinProperty.KEY_ORIGIN_SKIN))
                {
                    var f = new OriginKeepFaceData();
                    f._bUseCustomData = false;
                    return f;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return origin;
        }


        // 계승창에 얼굴 커스터마이즈
        [HarmonyPatch(typeof(UIPassiveSuccessionPopup), "SetData")]
        [HarmonyPostfix]
        private static void After_SetData(UnitDataModel unit, FaceEditor ___faceEdit)
        {
            try
            {
                var key = unit.CustomBookItem?._characterSkin;
                if (key != null && faceTargets.ContainsKey(key))
                {
                    var artwork = LoAModCache.Instance[faceTargets[key].packageId].Artworks;
                    var sp = artwork.GetNullable(faceTargets[key].overrideFaceSprite + "_setting");
                    if (sp == null)
                    {
                        sp = artwork.GetNullable(faceTargets[key].overrideFace.Invoke(key, key, unit).GetSettingFaceArtwork());
                    }
                    if (sp == null) return;
                    ___faceEdit.InitBySephirah(new LorId(16));
                    ___faceEdit.head.sprite = sp;
                    ___faceEdit.head.enabled = true;
                    ___faceEdit.head.color = new UnityEngine.Color(1f, 1f, 1f, 1f);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

        }

        private static SpecialCustomizedAppearance CreateApperance(LoACustomFaceData data, Transform parent)
        {
            if (!string.IsNullOrEmpty(data.PrefabKey))
            {
                var obj = LoAAssetBundles.Instance.LoadAsset<GameObject>(data.packageId, data.PrefabKey, false);
                var real = UnityEngine.Object.Instantiate(obj, parent).GetComponent<SpecialCustomizedAppearance>();
                return real;
            }
            var gameObject = new GameObject("LoA_Face");
            var emptyList = new List<SpriteRenderer>();
            SpecialCustomizedAppearance specialCustomizedAppearance = gameObject.AddComponent<SpecialCustomizedAppearance>();
            specialCustomizedAppearance.allSpriteList = new List<SpriteRenderer>();
            specialCustomizedAppearance.list = new List<SpecialCustomHead>();
            specialCustomizedAppearance.sephirahID = 1;
            specialCustomizedAppearance.face = new SpriteRenderer[0];

            var motions = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            var artwork = LoAModCache.Instance[data.packageId].Artworks;
            foreach (var i in motions)
            {
                var actionDetail = (ActionDetail)i;
                var key = data.GetFrontFaceArtwork(actionDetail);
                if (string.IsNullOrEmpty(key)) continue;
           
                var child = new GameObject($"LoA_Face_{actionDetail}");
                child.transform.SetParent(gameObject.transform);

                var renderer = child.AddComponent<SpriteRenderer>();
                var rearChild = new GameObject($"LoA_Face_Rear");
                rearChild.transform.SetParent(renderer.transform);
                var rearRenderer = rearChild.AddComponent<SpriteRenderer>();
                var lazyCom = child.AddComponent<LazyLoAFaceComponent>();
                lazyCom.key = key;
                lazyCom.rearKey = data.GetRearFaceArtwork(actionDetail);
                lazyCom.action = actionDetail;
                lazyCom.renderer = renderer;
                lazyCom.rearRenderer = rearRenderer;
                lazyCom.artwork = artwork;

                specialCustomizedAppearance.list.Add(new SpecialCustomHead
                {
                    headRenderer = null,
                    detail = actionDetail,
                    faceRenderer = null,
                    additionalFrontHair = emptyList,
                    frontHairRenderer = renderer,
                    motionDirection = actionDetail != ActionDetail.Penetrate ? CharacterMotion.MotionDirection.FrontView : CharacterMotion.MotionDirection.SideView,
                    additionalRearHair = emptyList,
                    rootObject = child,
                    additionalFace = emptyList,
                    rearHairRenderer = rearRenderer
                });
            }
            specialCustomizedAppearance.Init();
            return specialCustomizedAppearance;
        }
    
        [HarmonyPatch(typeof(CustomizingResourceLoader), "CreateCustomizedAppearance")]
        [HarmonyPrefix]
        private static bool Before_CreateCustomizedAppearance(UnitCustomizingData customData, Transform parent, ref CustomizedAppearance __result)
        {
            try
            {
                if (customData is LoACustomFaceData d && customData.specialCustomID?.id == 0)
                {
                    var f = CreateApperance(d, parent);
                    if (f != null)
                    {
                        __result = f;
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return true;
        }
    }
}
