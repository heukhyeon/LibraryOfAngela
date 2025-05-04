using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using LibraryOfAngela.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.Story
{
    class BookCategoryPatch : Singleton<BookCategoryPatch>
    {
        private Dictionary<string, CustomEquipBookCategory> categories = new Dictionary<string, CustomEquipBookCategory>();
        private Dictionary<LorId, string> keys = new Dictionary<LorId, string>();

        public void Initialize(List<CorePageConfig> mods)
        {
            mods.ForEach(x =>
            {
               
                var category = FrameworkExtension.GetSafeAction(() => x.GetEquipBookCategories());
                if (category?.Count > 0)
                {
                    foreach (var c in category)
                    {
                        if (string.IsNullOrEmpty(c.uniqueId))
                        {
                            c.uniqueId = x.packageId + c.visibleName;
                        }
                        foreach (var b in c.matchedBookIds)
                        {
                            keys[b] = c.uniqueId;
                        }
                        var origin = categories.SafeGet(c.uniqueId);
                        if (origin != null)
                        {
                            origin.matchedBookIds.AddRange(c.matchedBookIds);
                        }
                        else
                        {
                            categories[c.uniqueId] = c;
                        }
                    }
                }
            });
            if (categories.Count > 0)
            {
                InternalExtension.SetRange(GetType());
                foreach (var t in typeof(UIEquipPageScrollList).GetNestedTypes(AccessTools.all))
                {
                    foreach (var d in t.GetMethods(AccessTools.all))
                    {
                        if (d.Name.Contains("<SetData>b__1"))
                        {
                            t.PatchInternal(d.Name, PatchInternalFlag.POSTFIX, patchName: "FindCategory");
                            break;
                        }
                    }
                }

                foreach (var t in typeof(UISettingEquipPageScrollList).GetNestedTypes(AccessTools.all))
                {
                    foreach (var d in t.GetMethods(AccessTools.all))
                    {
                        if (d.Name.Contains("<SetData>b__1"))
                        {
                            t.PatchInternal(d.Name, PatchInternalFlag.POSTFIX, patchName: "FindCategory");
                            break;
                        }
                    }
                }
            }
        }

        /**
         * UIBookStoryChapterSlot.SetEpisodeSlots 에서
         * if (this.panel.panel.GetChapterBooksData(this.chapter).Count > 0) 를
         * 
         * BookCategoryPatch.InflateCustomEpisodeSlots(chapter, this, ref j)
         * if (this.panel.panel.GetChapterBooksData(this.chapter).Count > 0)
         * 
         * 로 위에 코드를 추가한다.
         */
        [HarmonyPatch(typeof(UIBookStoryChapterSlot), "SetEpisodeSlots")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetEpisodeSlots(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var fired = false;
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];

                if (!fired && i >= 1 && codes[i].opcode == OpCodes.Blt && codes[i-1].opcode == OpCodes.Add)
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIBookStoryChapterSlot), "chapter"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BookCategoryPatch), "InflateCustomEpisodeSlots"));
                }
            }
        }

        /// <summary>
        /// uistoryKeyData = new UIStoryKeyData(b.ClassInfo.Chapter, b.ClassInfo.id.packageId);
        /// ->
        /// uistoryKeyData = WrappingLoAStoryKeyData(new UIStoryKeyData(b.ClassInfo.Chapter, b.ClassInfo.id.packageId), b);
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UISettingEquipPageScrollList), "SetData")]
        [HarmonyPatch(typeof(UIEquipPageScrollList), "SetData")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetData(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Constructor(typeof(UIStoryKeyData), parameters: new Type[] { typeof(int), typeof(string) });
            var fired = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!fired && code.Is(OpCodes.Newobj, target))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(List<BookModel>.Enumerator), "get_Current"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BookCategoryPatch), nameof(WrappingLoAStoryKeyData)));
                }
            }
        }

        private static UIStoryKeyData WrappingLoAStoryKeyData(UIStoryKeyData origin, BookModel book)
        {
            if (book.ClassInfo.id.IsBasic()) return origin;
            var key = Instance.keys.SafeGet(book.GetBookClassInfoId());
            if (key is null) return origin;
            var match = Instance.categories.SafeGet(key);
            if (match is null)
            {
                return origin;
            }
            return new LoAUIStoryKeyData(match);
        }

        [HarmonyPatch(typeof(UIBookStoryPanel), "OnSelectEpisodeSlot")]
        [HarmonyPostfix]
        private static void After_OnSelectEpisodeSlot(UIBookStoryEpisodeSlot slot, TextMeshProUGUI ___selectedEpisodeText)
        {
            if (slot?.books == null || slot.books.Count == 0) return;
            var id = slot.books[0].id;
            var key = Instance.keys.SafeGet(id);
            if (key is null) return;
            var match = Instance.categories.SafeGet(key);
            if (match is null) return;
            ___selectedEpisodeText.text = match.visibleName;
        }

        /// <summary>
        /// <see cref="ILoACustomStoryInvitationMod"/> 구현한 모드의 책장이 서고 별도 슬롯을 차지하는경우 "기타" 항목에서 해당 모드 책장을 지운다.
        /// 이후 "기타" 항목이 빈 경우 기타 항목을 지운다.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="data"></param>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(UIBookStoryEpisodeSlot), "Init", new Type[] { typeof(int), typeof(List<BookXmlInfo>), typeof(UIBookStoryChapterSlot) })]
        [HarmonyPostfix]
        private static void After_Init(int chapter, List<BookXmlInfo> data, UIBookStoryEpisodeSlot __instance)
        {
            if (data.Count == 0) return;
            data.RemoveAll(x =>
            {
                var id = x.id;
                return Instance.keys.ContainsKey(id);
            });
            if (data.Count == 0) __instance.Deactive();
        }

        [HarmonyPatch(typeof(UISettingInvenEquipPageListSlot), "SetBooksData")]
        [HarmonyPatch(typeof(UIInvenEquipPageListSlot), "SetBooksData")]
        [HarmonyPostfix]
        private static void After_SetBooksData(Image ___img_Icon, Image ___img_IconGlow, TextMeshProUGUI ___txt_StoryName, List<BookModel> books, UIStoryKeyData storyKey)
        {
            if (books.Count > 0 && storyKey is LoAUIStoryKeyData data)
            {
                var category = data.category;
                var sprite = UISpriteDataManager.instance.GetStoryIcon(category.artwork);
                ___img_Icon.sprite = sprite.icon;
                ___img_IconGlow.sprite = sprite.iconGlow;
                ___txt_StoryName.text = category.visibleName;
            }
        }

        private static void After_FindCategory(ref bool __result, UIStoryKeyData x, BookModel ___b)
        {
            var id = ___b.GetBookClassInfoId();
            if (id.IsBasic()) return;
            var key = Instance.keys.SafeGet(id);
            if (key is null) return;
            var match = Instance.categories.SafeGet(key);
            if (match is null) return;
            __result = x is LoAUIStoryKeyData data && data.category == match;
        }

        private static void InflateCustomEpisodeSlots(int chapter, UIBookStoryChapterSlot slot, ref int index)
        {
            var targetCategories = Instance.categories.Where(x => x.Value.level == chapter);
            if (targetCategories.Count() == 0) return;
            var slots = slot.EpisodeSlots;
            foreach (var c in targetCategories)
            {
                try
                {
                    var category = c.Value;
                    var targetBooks = category.matchedBookIds
    .Where(x => BookInventoryModel.Instance._bookList.Exists(d => d.GetBookClassInfoId() == x))
    .Select(x => BookXmlList.Instance.GetData(x)).ToList();

                    if (targetBooks.Count == 0) continue;


                    var sprite = UISpriteDataManager.instance.GetStoryIcon(category.artwork);
                    var cnt = slots.Count;
                    while (cnt <= index)
                    {
                        slot.InstatiateAdditionalSlot();
                        var afterCnt = slots.Count;
                        // 뭔가의 이유로 슬롯 확장을 실패한 경우
                        if (cnt == afterCnt)
                        {
                            Logger.Log("Try InstatiateAdditionalSlot But Not Increased, Maybe Other Mod Conflict ?");
                            // 이 시점에선 어차피 제대로 못보여주므로 그냥 이후 동작을 스킵시킨다.
                            return;
                        }
                        else if (afterCnt > index)
                        {
                            break;
                        }
                        cnt = afterCnt;
                    }

                    slots[index].Init(targetBooks, slot);
                    slots[index].episodeText.text = category.visibleName;
                    slots[index].episodeIcon.sprite = sprite.icon;
                    slots[index].episodeIconGlow.sprite = sprite.iconGlow;
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }

                index++;
            }
        }

    }


    class LoAUIStoryKeyData : UIStoryKeyData
    {
        public CustomEquipBookCategory category;

        public LoAUIStoryKeyData(CustomEquipBookCategory category) : base(category.level, category.matchedBookIds[0].packageId)
        {
            this.category = category;
        }
    }
}
