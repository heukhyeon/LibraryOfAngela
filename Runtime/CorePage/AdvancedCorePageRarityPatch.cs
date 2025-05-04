using HarmonyLib;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.CorePage
{
    class AdvancedCorePageRarityPatch
    {
         static Dictionary<LorId, Color32> corePageOutlines = new Dictionary<LorId, Color32>();
         internal static Dictionary<LorId, Color32> corePageInnerLines = new Dictionary<LorId, Color32>();
         static Dictionary<LorId, Color32> passiveModels = new Dictionary<LorId, Color32>();

        public void Initialize()
        {
            Type patchType = GetType();
            foreach (var model in AdvancedEquipBookPatch.Instance.configs.Where(x => x != null).SelectMany(x =>
            {
                var models = x?.GetRarityModels() ?? new List<RarityModel>();
                return models.Where(d =>
                {
                    if (d is null)
                    {
                        Logger.Log($"Model Raryity Null ? in {x.packageId}");
                        return false;
                    }
                    return true;
                });
            }))
            {
                UpdateRarityModel(model);
            }

            // UILibrarianInfoInCardPhase -> 층 분류에서 사서 아래에 표시되는 현재 사서 정보
            // UILibrarianInfoPanel -> 사서 정보
            // UIEquipPagePreviewPanel -> 핵심책장에 마우스 over 했을때 나오는 미리보기
            // UIInvenLeftEquipPageSlot -> 핵심책장 목록에서 좌측 (북마크 등) 에 표시되는 책장들
            // UICharacterBookSlot -> 사서 정보에서 우측에 표시되는 핵심책장 아이콘
            // UILibrarianEquipDeckPanel -> 전투책장에서 사서 덱 목록 보여주는 판넬
            // UIBattleSettingLibrarianInfoPanel -> 전투 화면에서 사서 정보 보여주는 판넬
            // UIOriginEquipPageSlot -> 핵심책장 목록의 슬롯

            Patcher.PatchTranspiler(typeof(UIEquipPageModelPreviewPanel), patchType, "SetData");
            Patcher.PatchTranspiler(typeof(UILibrarianEquipBookInfoPanel), patchType, "SetUnitData");


            Patcher.PatchTranspiler(typeof(UIEquipPagePreviewPanel), patchType, "SetData");
            Patcher.PatchTranspiler(typeof(UIPassiveSuccessionBookSlot), patchType, "SetDefaultColor", "UIPassiveSuccessionBookSlot_SetDefaultColor");


            // Patcher.PatchTranspiler(typeof(UICustomCoreBookInfoPanel), patchType, "SetBookContentData");


            Patcher.PatchTranspiler(typeof(UILibrarianInfoInCardPhase), patchType, "SetData", "UILibrarianInfoInCardPhase_SetData");
            Patcher.PatchTranspiler(typeof(UIBattleSettingLibrarianInfoPanel), patchType, "SetData", "UIBattleSettingLibrarianInfoPanel_SetData");
            Patcher.PatchTranspiler(typeof(UIBattleSettingLibrarianInfoPanel), patchType, "SetEquipPageSlotState");
            Patcher.PatchTranspiler(typeof(UILibrarianInfoInCardPhase), patchType, "OnPointerExitEquipPage");
            Patcher.PatchTranspiler(typeof(UILibrarianInfoPanel), patchType, "UpdatePanel");


            
            Patcher.PatchTranspiler(typeof(UICharacterBookSlot), patchType, "SetHighlighted");
            Patcher.PatchTranspiler(typeof(UIEquipPagePreviewPanel), patchType, "SetPassiveBookInfoPanel");
            Patcher.PatchTranspiler(typeof(UIGachaEquipSlot), patchType, "SetDefaultColor");
            Patcher.PatchTranspiler(typeof(UIOriginEquipPageSlot), patchType, "SetColorFrame");
            Patcher.PatchTranspiler(typeof(UIInvenLeftEquipPageSlot), patchType, "SetColorFrame");
            Patcher.PatchTranspiler(typeof(UISettingInvenEquipPageLeftSlot), patchType, "SetColorFrame");


            typeof(UILibrarianEquipDeckPanel).Patch("SetData", "UILibrarianEquipDeckPanel_SetData");
            typeof(UIEquipPagePreviewPanel).Patch("SetData", "UIEquipPagePreviewPanel_SetData");
            typeof(UILibrarianInfoInCardPhase).Patch("SetData", "UILibrarianInfoInCardPhase_SetData");
            
            typeof(UIOriginEquipPageSlot).Patch("SetColor");
            typeof(UICharacterBookSlot).Patch("SetHighlighted");
            typeof(UIPassiveSuccessionCenterEquipBookSlot).Patch("SetColorByRarity");

            // 패시브 색상 입히기
            typeof(UIPassiveSuccessionPreviewPassiveSlot).Patch("SetColorByRarity", "SetColorByRarity_PassiveRare", types: typeof(Rarity));
            typeof(UIPassiveSuccessionCenterPassiveSlot).Patch("SetColorByRarity", "SetColorByRarity_Passive");
            typeof(UIPassiveSuccessionSlot).Patch("SetColorByRarity", "SetColorByRarity_Passive");
            typeof(UILibrarianEquipInfoSlot).Patch("SetData");

            // 전투 설정 핵심책장 이너라인 색입히기
            typeof(UIBattleSettingLibrarianInfoPanel).Patch("SetData", patchName: "UIBattleSettingLibrarianInfoPanel_SetData");
        }

        public static void UpdateRarityModel(RarityModel model)
        {
            switch (model.type)
            {
                case RarityModel.Type.PASSIVE:
                    passiveModels[model.targetId] = model.color;
                    break;
                case RarityModel.Type.BOOK_OUTLINE:
                    corePageOutlines[model.targetId] = model.color;
                    break;
                case RarityModel.Type.BOOK_INNERLINE:
                    corePageInnerLines[model.targetId] = model.color;
                    break;
            }
        }

        // in UICharacterBookSlot
        // UIColorManager.Manager.GetEquipRarityColor(this._bookModel.Rarity))
        // ->
        // AdvancedCorePageRarityPatch.GetRarityColor(UIColorManager.Manager.GetEquipRarityColor(this._bookModel.Rarity)), this._bookModel);
        private static IEnumerable<CodeInstruction> Trans_SetHighlighted(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UICharacterBookSlot), "_bookModel"))
            });
        }

        // in UIEquipPagePreviewPanel
        // UIColorManager.Manager.GetEquipRarityColor(giveBookModel.Rarity); 래핑
        private static IEnumerable<CodeInstruction> Trans_SetPassiveBookInfoPanel(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
{
                new CodeInstruction(OpCodes.Ldloc_2),
});
        }

        // UIGachaEquipSlot
        // UIColorManager.Manager.GetEquipRarityColor(this._book.Rarity) 래핑
        private static IEnumerable<CodeInstruction> Trans_SetDefaultColor(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIGachaEquipSlot), "_book"))
            });
        }

        // in UIEquipPageModelPreviewPanel
        // UIColorManager.Manager.GetEquipRarityColor(this.unitData.bookItem.Rarity); 래핑
        private static IEnumerable<CodeInstruction> Trans_SetData(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
            });
        }

        // in UILibrarianInfoInCardPhase
        // 이름 변경
        private static IEnumerable<CodeInstruction> Trans_UIBattleSettingLibrarianInfoPanel_SetData(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
{
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIBattleSettingLibrarianInfoPanel), "unitdata")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnitDataModel), "get_bookItem"))
});
        }

        // in UILibrarianInfoInCardPhase
        // 테두리 변경
        private static IEnumerable<CodeInstruction> Trans_SetEquipPageSlotState(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIBattleSettingLibrarianInfoPanel), "unitdata")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnitDataModel), "get_bookItem"))
            });
        }

        // in UILibrarianInfoInCardPhase
        private static IEnumerable<CodeInstruction> Trans_UILibrarianInfoInCardPhase_SetData(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnitDataModel), "get_bookItem"))
            });
        }

        private static IEnumerable<CodeInstruction> Trans_OnPointerExitEquipPage(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILibrarianInfoInCardPhase), "unitdata")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnitDataModel), "get_bookItem"))
            });
        }

        // in UILibrarianEquipBookInfoPanel
        // UIColorManager.Manager.GetEquipRarityColor(this.unitData.bookItem.Rarity); 래핑
        private static IEnumerable<CodeInstruction> Trans_SetUnitData(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILibrarianEquipBookInfoPanel), "unitData")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnitDataModel), "get_bookItem"))
            });
        }

        // UIPassiveSuccessionBookSlot
        // UIColorManager.Manager.GetEquipRarityColor(this.currentbookmodel.Rarity) 래핑
        private static IEnumerable<CodeInstruction> Trans_UIPassiveSuccessionBookSlot_SetDefaultColor(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIPassiveSuccessionBookSlot), "currentbookmodel"))
            });
        }

        // UILibrarianInfoPanel
        private static IEnumerable<CodeInstruction> Trans_UpdatePanel(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
{
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILibrarianInfoPanel), "_selectedUnit")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnitDataModel), "get_bookItem"))
});
        }

        // UICustomCoreBookInfoPanel
        private static IEnumerable<CodeInstruction> Trans_SetBookContentData(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
{
                new CodeInstruction(OpCodes.Ldarg_1)
});
        }

        // UIOriginEquipPageSlot
        private static IEnumerable<CodeInstruction> Trans_SetColorFrame(IEnumerable<CodeInstruction> instructions)
        {
            return mappingBookColor(instructions, new CodeInstruction[]
{
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIOriginEquipPageSlot), "_bookDataModel")),
});
        }

        private static void After_UIEquipPagePreviewPanel_SetData(BookModel book, UIEquipPagePreviewPanel __instance)
        {
            InjectTextColorCom(book, __instance.txt_BookName);
        }

        private static void After_UIBattleSettingLibrarianInfoPanel_SetData(UnitDataModel data, UIBattleSettingLibrarianInfoPanel __instance)
        {
            InjectTextColorCom(data.bookItem, __instance.setter_bookname);
        }

        private static void After_UILibrarianInfoInCardPhase_SetData(UnitDataModel data, UILibrarianInfoInCardPhase __instance)
        {
            InjectTextColorCom(data.bookItem, __instance.txt_BookName);
        }

        private static void After_UILibrarianEquipDeckPanel_SetData(UILibrarianEquipDeckPanel __instance)
        {
            if (__instance._unitdata != null)
            {
                InjectTextColorCom(__instance._unitdata.bookItem, __instance.txt_BookName);
            }
        }

        private static void After_SetHighlighted(UICharacterBookSlot __instance)
        {
            InjectTextColorCom(__instance.BookModel, __instance.BookName);
        }

        private static void After_SetColorByRarity(UIPassiveSuccessionCenterEquipBookSlot __instance)
        {
            var id = __instance._currentbookmodel.BookId;
            if (AdvancedEquipBookPatch.Instance.infos.ContainsKey(id))
            {
                var color = GetRarityColor(__instance.img_Frame.color, __instance._currentbookmodel);
                __instance.img_Frame.color = color;
                __instance.img_IconGlow.color = color;
                __instance.setter_name.underlayColor = color;
            }
        }

        private static Color defaultColor;

        // UIOriginEquipPageSlot
        private static void After_SetColor(Color c, UIOriginEquipPageSlot __instance)
        {
            if (defaultColor == default(Color))
            {
                defaultColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
            }
            if (defaultColor == c && corePageInnerLines.ContainsKey(__instance.BookDataModel.BookId))
            {
                var color = GetRarityInnerColor(c, __instance.BookDataModel);
                __instance.Frame.color = color;
                __instance.BookName.color = color;

            }
        }

        /// <summary>
        /// <see cref="UIPassiveSuccessionPreviewPassiveSlot"/> 은 레어리티에 따라서 처리하는게 있으므로 별도 처리
        /// </summary>
        /// <param name="___passivemodel"></param>
        private static void After_SetColorByRarity_PassiveRare(PassiveModel ___passivemodel, List<Graphic> ___graphics_Rarity)
        {
            var passive = ___passivemodel?.reservedData?.currentpassive?.id;
            if (passive is null || !passiveModels.ContainsKey(passive)) return;
            var color = passiveModels[passive];
            foreach (Graphic graphic in ___graphics_Rarity)
            {
                if (graphic != null)
                {
                    graphic.color = color;
                }
            }
        }

        private static void Before_SetColorByRarity_Passive(ref Color c, PassiveModel ___passivemodel)
        {
            var passive = ___passivemodel?.reservedData?.currentpassive?.id;
            if (passive is null || !passiveModels.ContainsKey(passive)) return;
            if (c == UIColorManager.Manager.GetUIColor(UIColor.Disabled)) return;
            c = passiveModels[passive];
        }

        private static void After_SetData(UILibrarianEquipInfoSlot __instance)
        {
            var passive = __instance._currentpassive?.passive?.id;
            if (passive is null || !passiveModels.ContainsKey(passive)) return;
            var color = passiveModels[passive];
            __instance.Frame.color = color;
            __instance.txt_cost.color = color;
        }

        public static IEnumerable<CodeInstruction> mappingBookColor(IEnumerable<CodeInstruction> instructions, IEnumerable<CodeInstruction> bookTargetIL)
        {
            var target = AccessTools.Method(typeof(UIColorManager), "GetEquipRarityColor");
            var target2 = AccessTools.Method(typeof(UIColorManager), "GetUIColor");
            var codes = new List<CodeInstruction>(instructions);
            for(int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                if (code.opcode == OpCodes.Call && code.operand is MethodBase m && m.Name.Contains("Passive"))
                {
                    var param = m.GetParameters();
                    if (param.Length == 1 && param[0].ParameterType == typeof(Color))
                    {
                        foreach (var il in bookTargetIL)
                        {
                            yield return il;
                        }
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedCorePageRarityPatch), "FixDuplicatedColor"));
                    }
                }

                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    foreach (var il in bookTargetIL)
                    {
                        yield return il;
                    }
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedCorePageRarityPatch), "GetRarityColor"));
                }
                else if (code.Is(OpCodes.Callvirt, target2) && codes[i - 1].opcode == OpCodes.Ldc_I4_0)
                {
                    foreach (var il in bookTargetIL)
                    {
                        yield return il;
                    }
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedCorePageRarityPatch), "GetRarityInnerColor"));
                }
            }
        }

        private static Color GetRarityColor(Color origin, BookModel book)
        {

            if (corePageOutlines.ContainsKey(book.BookId))
            {
                return corePageOutlines[book.BookId];
            }

            return origin;
        }

        private static Color GetRarityInnerColor(Color origin, BookModel book)
        {
            if (book == null) return origin;

            if (corePageInnerLines.ContainsKey(book.BookId)) return corePageInnerLines[book.BookId];

            return origin;
        }

        private static Color FixDuplicatedColor(Color origin, BookModel book)
        {
            if (book == null) return origin;

            if (corePageInnerLines.ContainsKey(book.BookId) && corePageInnerLines[book.BookId] == origin) return UIColorManager.Manager.GetUIColor(UIColor.Default);

            return origin;
        }

        private static void InjectTextColorCom(BookModel book, MonoBehaviour target)
        {
            var exists = target.GetComponent<RairtyTextComponent>();
            if (exists != null) exists.UpdateTarget(book);
            else
            {
                target.gameObject.AddComponent<RairtyTextComponent>().UpdateTarget(book);
            }
        }
    }
}
