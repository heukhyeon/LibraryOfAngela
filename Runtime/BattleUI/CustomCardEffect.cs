using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using Workshop;

namespace LibraryOfAngela.BattleUI
{
    class CustomCardEffect : Singleton<CustomCardEffect>
    {
        private bool isInit = false;
        private Dictionary<BattleDiceCardUI, LoACustomCardHandEffect> handEffects = new Dictionary<BattleDiceCardUI, LoACustomCardHandEffect>();
        private List<CustomCardHandEffect> currentEffects;
        private EffectCanvasUI effectUI;
        private bool effectExists = false;

        public static void Initialize()
        {
            InternalExtension.SetRange(typeof(CustomCardEffect));
            foreach (var x in LoAModCache.BattlePageConfigs)
            {
                var cards = x.GetCardHandEffects();
                if (cards.Count > 0)
                {
                    if (!Instance.isInit)
                    {
                        Instance.isInit = true;
                    }
                }
            }
        }

        public void ClearThisRoundCardEffects()
        {
            foreach (var d in handEffects)
            {
                d.Value.UpdateTarget(null);
            }
        }

        [HarmonyPatch(typeof(StageController), "StartBattle")]
        [HarmonyPostfix]
        private static void After_StartBattle()
        {
            Instance.effectExists = false;
            Instance.currentEffects = new List<CustomCardHandEffect>();
            foreach (var x in LoAModCache.BattlePageConfigs)
            {
                foreach (var e in x.GetCardHandEffects())
                {
                    if (e.effectExistable())
                    {
                        e.packageId = x.packageId;
                        Instance.effectExists = true;
                        Instance.currentEffects.Add(e);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StageController), "ClearResources")]
        [HarmonyPostfix]
        private static void After_ClearResources()
        {
            foreach (var d in Instance.handEffects)
            {
                UnityEngine.Object.Destroy(d.Value);
            }
            Instance.handEffects.Clear();
            EffectCanvasUI.effectRef = 0;
        }

        [HarmonyPatch(typeof(BattleDiceCardUI), "SetCard")]
        [HarmonyPostfix]
        private static void After_SetCard(BattleDiceCardModel cardModel, BattleDiceCardUI __instance, Image[] ___img_Frames)
        {
            if (!Instance.effectExists) return;

            var effetorParent = ___img_Frames[0];

            var unitModel = SingletonBehavior<BattleManagerUI>.Instance.ui_unitCardsInHand.SelectedModel;
            if (Instance.effectUI is null)
            {
                Logger.Log("Create LoA Effect Canvas UI");
                var targetCamera = GameObject.Find("[Camera]Overlay_UI").GetComponent<Camera>();
                var targetCanvas = GameObject.Find("[Canvas]UnitCardsInHandUI").GetComponent<Canvas>();
                Instance.effectUI = EffectCanvasUI.Create(targetCanvas, targetCamera);
            }
            var target = Instance.currentEffects.Find(x => x.isValidCard(cardModel, unitModel));
            if (!Instance.handEffects.ContainsKey(__instance))
            {
                if (target == null) return;
                Instance.handEffects[__instance] = __instance.gameObject.AddComponent<LoACustomCardHandEffect>();
            }
            var effector = Instance.handEffects[__instance];
            effector.InjectParent(__instance.gameObject, Instance.effectUI.canvas.transform, effetorParent.transform);
            effector.UpdateTarget(target);
        }

        [HarmonyPatch(typeof(CustomizingCardArtworkLoader), "GetSpecificArtworkSprite")]
        [HarmonyPostfix]
        private static void After_GetSpecificArtworkSprite(string id, string name, ref Sprite __result, Dictionary<string, List<ArtworkCustomizeData>> ____artworkData)
        {
            if (!(__result is null)) return;
            var key = FrameworkExtension.GetSafeAction(() => LoAModCache.Instance[id]?.ArtworkConfig?.ConvertValidCombatPagePackage(name));

            if (key == null) return;
            else if (key == "")
            {
                __result = Singleton<AssetBundleManagerRemake>.Instance.LoadCardSprite(name);
            }
            else if (____artworkData.ContainsKey(key))
            {
                __result = ____artworkData[key].Find(x => x.name == name).sprite;
            }
            if (!(__result is null))
            {
                var data = new ArtworkCustomizeData
                {
                    name = name,
                    spritePath = ""
                };
                data._sprite = __result;
                if (!____artworkData.ContainsKey(id))
                {
                    ____artworkData[id] = new List<ArtworkCustomizeData>();
                }
                ____artworkData[id].Add(data);
            }
        }

        [HarmonyPatch(typeof(BattleCardAbilityDescXmlList), "GetAbilityKeywords_byScript")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_GetAbilityKeywords_byScript(IEnumerable<CodeInstruction> instructions)
        {
            var fired = false;
            foreach (var code in instructions)
            {
                if (!fired && code.opcode == OpCodes.Stloc_2)
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomCardEffect), "ConvertModSelfAbilityKeyword"));
                }
                yield return code;
            }
        }

        private static Type ConvertModSelfAbilityKeyword(Type origin, string scriptName)
        {
            if (origin != null) return origin;
            Type type;
            if (AssemblyManager.Instance._diceCardSelfAbilityDict.TryGetValue(scriptName.Trim(), out type)) return type;
            return origin;
        }
    }
}
