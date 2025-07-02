using HarmonyLib;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.SD;
using LibraryOfAngela.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using Workshop;

namespace LibraryOfAngela.EquipBook
{
    class SkinPatch
    {
        public static void Initialize()
        {
        }

        /// <summary>
        /// 1.
        /// unit.bookItem -> unit.CustomBookItem
        /// 
        /// 2.
        /// unitAppearance2.InitCustomData(unit.customizeData, unit.defaultBook.GetBookClassInfoId());
        /// ->
        /// FacePatchComponent.InjectFacePatchComponent(unitApperance2, unit).InitCustomData(unit.customizeData, unit.defaultBook.GetBookClassInfoId());
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(SdCharacterUtil), "CreateSkin")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CreateSkin(IEnumerable<CodeInstruction> instructions)
        {
            var getItemProperty = AccessTools.Method(typeof(UnitDataModel), "get_bookItem");
            var getCustomBookItem = AccessTools.Method(typeof(UnitDataModel), "get_CustomBookItem");
            var codes = new List<CodeInstruction>(instructions);
            var skins = AccessTools.Field(typeof(BookXmlInfo), "CharacterSkin");
            var getItem = AccessTools.Method(typeof(List<string>), "get_Item");
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.Is(OpCodes.Callvirt, getItemProperty))
                {
                    yield return new CodeInstruction(OpCodes.Callvirt, getCustomBookItem);
                }
                else if (code.opcode == OpCodes.Stloc_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FacePatch), nameof(FacePatch.HandleLoAFaceBase)));
                    yield return code;
                }
                else if (code.Is(OpCodes.Ldfld, skins))
                {
                    yield return code;
                    while (i < codes.Count)
                    {
                        i++;
                        code = codes[i];
                        if (code.Is(OpCodes.Callvirt, getItemProperty))
                        {
                            yield return new CodeInstruction(OpCodes.Callvirt, getCustomBookItem);
                        }
                        else
                        {
                            yield return code;
                        }

                        if (code.Is(OpCodes.Callvirt, getItem))
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkinInfoProvider), "ConvertValidSkinName"));
                            break;
                        }
         
                    }
                }
                else yield return code;
            }
        }
        [HarmonyPatch(typeof(SdCharacterUtil), "LoadAppearance")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_LoadAppearance(IEnumerable<CodeInstruction> instructions)
        {
            var getItemProperty = AccessTools.Method(typeof(UnitDataModel), "get_bookItem");
            var getCustomBookItem = AccessTools.Method(typeof(UnitDataModel), "get_CustomBookItem");
            var codes = new List<CodeInstruction>(instructions);
            var callName = AccessTools.Method(typeof(BookModel), "GetOriginalCharcterName");
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.Is(OpCodes.Callvirt, getItemProperty))
                {
                    yield return new CodeInstruction(OpCodes.Callvirt, getCustomBookItem);
                }
                else if (code.Is(OpCodes.Callvirt, callName))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkinInfoProvider), "ConvertValidSkinName"));
                }
                else yield return code;
            }
        }

        [HarmonyPatch(typeof(UICharacterRenderer), "SetCharacter")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetCharacter(IEnumerable<CodeInstruction> instructions)
        {
            var getItemProperty = AccessTools.Method(typeof(UnitDataModel), "get_bookItem");
            var getCustomBookItem = AccessTools.Method(typeof(UnitDataModel), "get_CustomBookItem");
            var codes = new List<CodeInstruction>(instructions);
            var target = AccessTools.Method(typeof(CharacterAppearance), "Initialize");
            var target3 = AccessTools.Method(typeof(BookModel), "get_IsWorkshop");
            var target4 = AccessTools.Method(typeof(AssetBundleManagerRemake), "LoadCharacterPrefab_DefaultMotion");
            var target2 = AccessTools.Field(typeof(UI.UICharacter), "unitAppearance");
            var bookModel = AccessTools.Method(typeof(BookModel), "GetCharacterName");
            var customData = AccessTools.Method(typeof(UnitDataModel), "get_customizeData");
            var getCharacterName = AccessTools.Method(typeof(BookXmlInfo), "GetCharacterSkin");

            bool fire = false;

            for (int i = 0; i < codes.Count; i++)
            {            
                var code = codes[i];
                if (code.Is(OpCodes.Callvirt, bookModel))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(SkinInfoProvider), nameof(SkinInfoProvider.ConvertValidSkinName)));
                }
                else if (code.Is(OpCodes.Callvirt, customData))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FacePatch), nameof(FacePatch.HandleLoAFaceBase)));
                }
                else if (code.Is(OpCodes.Callvirt, getItemProperty))
                {
                    yield return new CodeInstruction(OpCodes.Callvirt, getCustomBookItem);
                }
                else if (code.Is(OpCodes.Callvirt, getCharacterName))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkinInfoProvider), nameof(SkinInfoProvider.ConvertValidSkinName)));
                }
                else if (code.Is(OpCodes.Callvirt, target4))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 5);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkinPatch), nameof(HandleRenderLoAPrefab)));
                }
                else if (!fire && code.Is(OpCodes.Callvirt, target3))
                {
                    fire = true;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkinPatch), nameof(HandleRenderWorkshopSkin)));
                }
                else yield return code;
            }
        }

        [HarmonyPatch(typeof(CharacterAppearance), "Initialize")]
        [HarmonyFinalizer]
        private static Exception Finalize_Initialize(CharacterAppearance __instance, Exception __exception)
        {
            if (__exception is NullReferenceException)
            {
                var logBuilder = new StringBuilder("CharacterApperance.Initialize NPE Detected, Maybe Other Mods Conflict\n");
                logBuilder.AppendLine($"- motionList Null : {__instance._motionList == null}");
                logBuilder.AppendLine($"- dictionary Null : {__instance._characterMotionDic == null}");
                if (__instance._motionList != null)
                {
                    for (int i = 0; i < __instance._motionList.Count; i++)
                    {
                        logBuilder.AppendLine($"- motion Items Not Null ({i}) : {__instance._motionList[i] != null}");
                    }
                    __instance._motionList.RemoveAll(x => x == null);
                    try
                    {
                        foreach (CharacterMotion characterMotion in __instance._motionList)
                        {
                            if (!__instance._characterMotionDic.ContainsKey(characterMotion.actionDetail))
                            {
                                __instance._characterMotionDic.Add(characterMotion.actionDetail, characterMotion);
                            }
                            characterMotion.gameObject.SetActive(false);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log("CharacterApperance.Initialize Finalize Internal Error");
                        Logger.LogError(e);
                    }
                }
                Logger.Log(logBuilder.ToString());
                return null;
            }
            return __exception;
        } 
    
        private static bool HandleRenderWorkshopSkin(bool origin, UnitDataModel unit)
        {
            if (!origin) return origin;
            return !SkinRenderPatch.IsLoAPrefabCharacter(unit);
        }

        private static GameObject HandleRenderLoAPrefab(GameObject origin, UnitDataModel unit, ref string text, int num)
        {
            if (!string.IsNullOrEmpty(text))
            {
                return origin;
            }


            var prefab = SkinRenderPatch.LoadLoAPrefab(unit, true);
            if (!(prefab is null))
            {
                text = unit.CustomBookItem.GetCharacterName();
            }
            return prefab;

        }
    }
}
