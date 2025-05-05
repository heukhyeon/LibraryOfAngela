using GameSave;
using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.CorePage
{
    class MultiDeckPatch
    {
        private static Dictionary<UIEquipDeckCardList, MultiDeckComponent> dictionary = new Dictionary<UIEquipDeckCardList, MultiDeckComponent>();
        internal static Dictionary<LorId, MultiDeckInfo> infos = new Dictionary<LorId, MultiDeckInfo>();
        internal static Dictionary<MultiDeckInfo.DeckInfo, List<DiceCardItemModel>> cards = new Dictionary<MultiDeckInfo.DeckInfo, List<DiceCardItemModel>>();
        internal static Dictionary<DeckModel, BookModel> deckOwners = new Dictionary<DeckModel, BookModel>();
        internal static Dictionary<LorId, BattlePageInfo> fixedCountCards = new Dictionary<LorId, BattlePageInfo>();
        internal static Dictionary<LorId, List<BattlePageInfo>> visibleConditionCards = new Dictionary<LorId, List<BattlePageInfo>>();
        internal static HashSet<LorId> multideckSwitchRefreshTargets = new HashSet<LorId>();

        public static void Initialize(List<CorePageConfig> mods)
        {
            foreach (var x in LoAModCache.BattlePageConfigs)
            {
                foreach (var d in x.GetBattlePageInfos())
                {
                    if (d.fixedCount > 0)
                    {
                        fixedCountCards[new LorId(x.packageId, d.id)] = d;
                    }
                    if (d.visibleCondition != null)
                    {
                        var m = d;
                        m.packageId = x.packageId;
                        var key = new LorId(x.packageId, d.id);
                        if (!visibleConditionCards.ContainsKey(key))
                        {
                            visibleConditionCards[key] = new List<BattlePageInfo>();
                        }
                        if (d.visibleCondition is BattlePageInfo.VisibleCondition.MultiDeck m2)
                        {
                            m2.packageId = m2.packageId ?? x.packageId;
                            multideckSwitchRefreshTargets.Add(new LorId(x.packageId, m2.corePageId));
                        }
                        visibleConditionCards[key].Add(m);
                    }
                }
            }

            InternalExtension.SetRange(typeof(MultiDeckPatch));
        }

        // 데이터 초기화 이후에 되야함
        public static void InsertMultideckOption(List<CorePageConfig> mods)
        {
            foreach (var x in mods)
            {
                var settings = FrameworkExtension.GetSafeAction(() => x.JustSettingCards);
                if (settings != null)
                {
                    settings.ForEach(d =>
                    {
                        if (!fixedCountCards.ContainsKey(d))
                        {
                            fixedCountCards[d] = new BattlePageInfo(d.id)
                            {
                                packageId = x.packageId,
                                fixedCount = ItemXmlDataList.instance.GetCardItem(d).Rarity == Rarity.Unique ? 1 : 3,
                            };
                        }
                    });
                }
                var multideck = x.MultiDecks;
                if (multideck == null || multideck.Count == 0) continue;
                multideck.ForEach(d =>
                {
                    infos[d.targetId] = d;
                    var targetBook = BookXmlList.Instance.GetData(d.targetId);
                    if (!targetBook.optionList.Contains(BookOption.MultiDeck))
                    {
                        targetBook.optionList.Add(BookOption.MultiDeck);
                    }
                });
            }
        }

        [HarmonyPatch(typeof(InventoryModel), "RemoveCard")]
        [HarmonyPrefix]
        private static bool Before_RemoveCard(LorId cardId, ref bool __result)
        {
            if (fixedCountCards.ContainsKey(cardId))
            {
                __result = true;
                return false;
            }
            return true;
        }
        private static MultiDeckInfo currentMultideckTarget;
        private static List<LorId> currentVisibleCards;

        [HarmonyPatch(typeof(UIInvenCardListScroll), "GetCardsByGradeFilterUI")]
        [HarmonyPrefix]
        private static bool Before_GetCardsByGradeFilterUI(UIInvenCardListScroll __instance, ref List<DiceCardItemModel> __result)
        {
            var bookId = __instance?._unitdata?.bookItem?.BookId;
            if (bookId == null) return true;

            var targetInfo = infos.SafeGet(bookId);
            if (targetInfo == null) return true;
            var deck = __instance._unitdata.bookItem._deck;
            var index = __instance._unitdata.bookItem._deckList.IndexOf(deck);
            var matchInfo = targetInfo.infos[index];
            var deckCards = FrameworkExtension.GetSafeAction(() => matchInfo.visibleCards?.Invoke(deck.GetCardList_nocopy()));
            if (deckCards == null || deckCards.Count == 0) return true;
            ResetDeckCards(matchInfo, deckCards);
            __result = cards[matchInfo];
            return false;
        }

        [HarmonyPatch(typeof(DeckModel), "MoveCardToInventory")]
        [HarmonyPrefix]
        private static bool Before_MoveCardToInventory(ref bool __result, DeckModel __instance, LorId cardId)
        {
            try
            {
                var owner = deckOwners.SafeGet(__instance);
                if (owner is null) return true;
                var info = infos.SafeGet(owner.BookId);
                if (info is null) return true;
                var matchInfo = info.infos[owner._deckList.IndexOf(__instance)];
                var result = matchInfo.onCardRemove?.Invoke(owner.owner, __instance, cardId);
                if (result is null)
                {
                    return true;
                }
                if (result is MultiDeckRemoveState.Rejected)
                {
                    __result = false;
                    return false;
                }
                if (result is MultiDeckRemoveState.RemovedWithoutInventory)
                {
                    __instance._deck.Remove(ItemXmlDataList.instance.GetCardItem(cardId));
                    __result = true;
                    return false;
                }
                if (result is MultiDeckRemoveState.CustomHandle h)
                {
                    __result = h.isRemoved;
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return true;
        }

        private static void ResetDeckCards(MultiDeckInfo.DeckInfo matchInfo, List<LorId> deckCards)
        {
            bool flag = cards.ContainsKey(matchInfo) && cards[matchInfo].Count == deckCards.Count;
            if (flag)
            {
                for (int i = 0; i < deckCards.Count; i++)
                {
                    if (cards[matchInfo][i].GetID() != deckCards[i])
                    {
                        flag = false;
                        break;
                    }
                }
            }
            if (!flag)
            {
                cards[matchInfo] = deckCards.Select(x =>
                {
                    var c = new DiceCardItemModel(ItemXmlDataList.instance.GetCardItem(x));
                    c.num = c.GetRarity() == Rarity.Unique ? 1 : 3;
                    return c;
                }).ToList();
            }
        }

        // InventoryModel.AddCard
        // 설정용 카드는 들어가지 않게 한다.
        [HarmonyPatch(typeof(InventoryModel), "AddCard", new Type[] { typeof(LorId), typeof(int) })]
        [HarmonyPrefix]
        private static bool Before_AddCard(LorId cardId)
        {
            try
            {
                if (fixedCountCards.ContainsKey(cardId))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return true;
        }

        [HarmonyPatch(typeof(BookModel), nameof(BookModel.AddCardFromInventoryToCurrentDeck))]
        [HarmonyPostfix]
        private static void After_AddCardFromInventoryToCurrentDeck(BookModel __instance, LorId cardId, ref CardEquipState __result)
        {
            try
            {
                var bookId = __instance.GetBookClassInfoId();
                if (!infos.ContainsKey(bookId)) return;
                var index = __instance._deckList.IndexOf(__instance._deck);
                if (index == -1)
                {
                    Logger.Log($"Insert Detected, But Deck Index -1....? {bookId}");
                    return;
                }
                var deckInfo = infos[bookId].infos[index];
                var res = deckInfo.onCardInsert?.Invoke(__instance.owner, __instance._deck, cardId, __result);
                if (res != null)
                {
                    __result = res.Value;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(BookModel), "LoadFromSaveData")]
        [HarmonyPostfix]
        private static void After_LoadFromSaveData(BookModel __instance)
        {
            var id = __instance.BookId;
            var info = infos.SafeGet(id);
            if (info != null) InjectDeckOwner(__instance, info);
        }

        [HarmonyPatch(typeof(BookInventoryModel), "CreateBook", new Type[] { typeof(BookXmlInfo) })]
        [HarmonyPostfix]
        private static void After_CreateBook(BookXmlInfo bookClassInfo, BookModel __result)
        {
            if (__result is null || bookClassInfo.id.IsBasic()) return;

            var id = bookClassInfo.id;
            var info = infos.SafeGet(id);
            if (info != null) InjectDeckOwner(__result, info);
            try
            {
                LoAModCache.Instance[bookClassInfo.workshopID]?.CorePageConfig?.OnCreateCorePage(__result);

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(UIEquipDeckCardList), "OnChangeDeckTab")]
        [HarmonyPostfix]
        private static void After_OnChangeDeckTab(UIEquipDeckCardList __instance)
        {
            try
            {
                var bookId = __instance.currentunit?.bookItem?.BookId;
                if (bookId == null) return;
                if (currentMultideckTarget != null)
                {
                    var index = __instance.deckTabsController.GetCurrentIndex();
                    if (index >= 0 && index < currentMultideckTarget.infos.Count)
                    {
                        var deck = __instance.currentunit.bookItem._deck.GetCardList_nocopy();
                        currentVisibleCards = FrameworkExtension.GetSafeAction(() => currentMultideckTarget.infos[index].visibleCards?.Invoke(deck));
                    }
                }
                if (multideckSwitchRefreshTargets.Contains(bookId))
                {
                    GetCurrentInvenCardList().ApplyFilterAll();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static UIEquipDeckCardList GetCurrentEquipDeckCardList()
        {
            var controller = UI.UIController.Instance;
            if (controller.CurrentUIPhase == UIPhase.BattleSetting)
            {
                // BattleSetting 상태일 때
                var battlePanel = controller.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel;
                return battlePanel.EditPanel.BattleCardPanel.EquipInfoDeckPanel.EquipDeckPanel;
            }
            else
            {
                // 일반 상태일 때
                var cardPanel = controller.GetUIPanel(UIPanelType.Page) as UICardPanel;
                return cardPanel.EquipInfoDeckPanel.EquipDeckPanel;
            }
        }

        private static UIInvenCardListScroll GetCurrentInvenCardList()
        {
            if (UI.UIController.Instance.CurrentUIPhase != UIPhase.BattleSetting)
            {
                return (UI.UIController.Instance.GetUIPanel(UIPanelType.Page) as UICardPanel).InvenCardList;
            }
            return (UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel).EditPanel.BattleCardPanel.InvenCardList;
        }

        [HarmonyPatch(typeof(UIEquipDeckCardList), "RefreshAll")]
        [HarmonyPostfix]
        private static void After_RefreshAll(UIEquipDeckCardList __instance)
        {
            try
            {
                var bookId = __instance?.currentunit?.bookItem?.BookId;
                if (bookId == null) return;

                var targetInfo = infos.SafeGet(bookId);
                if (targetInfo == null) return;
                var index = __instance.deckTabsController.GetCurrentIndex();
                if (index < 0 || targetInfo.infos.Count <= index)
                {
                    Logger.Log($"Current Deck Index Overflow... What? Target Book :{bookId} // index :: {index} // DeckInfo Count :: {targetInfo.infos.Count}");
                    return;
                }
                var matchInfo = targetInfo.infos[index];
                var currentDeck = __instance.currentunit.bookItem._deckList[index].GetCardList_nocopy();
                var visibleCards = FrameworkExtension.GetSafeAction(() => matchInfo.visibleCards?.Invoke(currentDeck));
                if (visibleCards != null && visibleCards.Count > 0)
                {
                    __instance.rootPanel.button_OpenDeckListButton.interactable = false;
                    __instance.rootPanel.button_SaveDeckButton.interactable = false;
                }
                else
                {
                    __instance.rootPanel.button_OpenDeckListButton.interactable = true;
                    __instance.rootPanel.button_SaveDeckButton.interactable = true;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(UILibrarianEquipDeckPanel), "SetData")]
        [HarmonyPostfix]
        private static void After_SetData(UILibrarianEquipDeckPanel __instance)
        {
            After_RefreshAll(__instance.EquipDeckPanel);
        }

        private static void InjectDeckOwner(BookModel target, MultiDeckInfo info)
        {
            for (int i = 0; i < info.infos.Count; i++)
            {
                var deck = target._deckList[i];
                var targetInfo = info.infos[i];
                deckOwners[deck] = target;
                if (targetInfo.visibleCards != null)
                {
                    int j = 0;
                    while (j < deck._deck.Count)
                    {
                        var c = deck._deck[j];
                        if (c.isError)
                        {
                            deck._deck.Remove(c);
                            continue;
                        }
                        j++;
                    }
                }
                try
                {
                    if (targetInfo.onCardChange != null)
                    {
                        var decks = targetInfo.onCardChange(target.owner, deck, deck.GetCardList_nocopy());
                        if (decks != null)
                        {
                            Logger.Log($"Invalid Deck Save, Reset : {target.BookId} // Deck {i}");
                            deck._deck = decks;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }

            }
        }

        [HarmonyPatch(typeof(UIInvenCardListScroll), "GetCardBySearchFilterUI")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_GetCardBySearchFilterUI(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(List<DiceCardItemModel>), "FindAll");
            foreach (var code in instructions)
            {
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MultiDeckPatch), nameof(FilterByMultiDeckOnlyPage)));
                }
                yield return code;
            }
        }

        /// <summary>
        /// CardEquipState currentState = unit.AddCardFromInventory(list2[i].GetID());
        /// ->
        /// CardEquipState currentState = DisableByMultiDeckOnlyPage(unit.AddCardFromInventory(list2[i].GetID()), list2[i].GetID()));
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UIDeckCardList), "SetDeckCheck")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetDeckCheck(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(UnitDataModel), "AddCardFromInventory");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MultiDeckPatch), nameof(DisableByMultiDeckOnlyPage)));
                }

            }
        }

        /// <summary>
        /// this.currentunit.GetDeck().Count >= this.currentunit.GetDeckSize()
        /// ->
        /// this.currentunit.GetDeck().Count >= CheckForceInsert(this.currentunit.GetDeckSize(), this)
        /// 
        /// 강제 삽입이 있다면 어차피 여기서 처리하므로 생략한다.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UIEquipDeckCardList), nameof(UIEquipDeckCardList.InsertCardSlot))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_InsertCardSlot(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(UnitDataModel), nameof(UnitDataModel.GetDeckSize));
            bool fired = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target) && !fired)
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MultiDeckPatch), nameof(MultiDeckPatch.CheckForceInsert)));
                }
            }
        }

        private static int CheckForceInsert(int count, UIEquipDeckCardList __instance)
        {
            try
            {
                var bookItem = __instance.currentunit.bookItem;
                if (bookItem is null || !infos.ContainsKey(bookItem.GetBookClassInfoId())) return count;
                var deckIndex = bookItem._deckList.IndexOf(bookItem._deck);
                if (deckIndex == -1) return count;
                if (infos[bookItem.GetBookClassInfoId()].infos[deckIndex].onCardInsert is null) return count;
                return 100;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return count;
            }
        }

        private static Predicate<DiceCardItemModel> FilterByMultiDeckOnlyPage(Predicate<DiceCardItemModel> origin)
        {
            return new Predicate<DiceCardItemModel>((x) =>
            {
                if (!origin(x)) return false;
                try
                {
                    var id = x.GetID();
                    if (id.IsBasic() || !visibleConditionCards.ContainsKey(id))
                    {
                        if (currentVisibleCards?.Contains(id) == false)
                        {
                            return false;
                        }
                        return true;
                    }
                    int blockFlag = -1;
                    foreach (var info in visibleConditionCards[id])
                    {
                        var condition = info.visibleCondition;
                        if (condition is BattlePageInfo.VisibleCondition.MultiDeck m)
                        {
                            if (blockFlag == -1) blockFlag = 0;
                            var book = UI.UIController.Instance.CurrentUnit.bookItem.BookId;
                            if (book.packageId == m.packageId || book.id == m.corePageId)
                            {
                                var cardList = GetCurrentEquipDeckCardList();
                                if (cardList.deckTabsController.GetCurrentIndex() == m.deckIndex)
                                {
                                    blockFlag = 1;
                                }
                            }
                        }
                    }
                    if (blockFlag == 0)
                    {
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                    return true;
                }

            });
        }

        private static CardEquipState DisableByMultiDeckOnlyPage(CardEquipState origin, List<DiceCardItemModel> cards, int index)
        {
            if (origin != CardEquipState.Equippable) return origin;
            if (cards is null || index < 0 || cards.Count >= index) return origin;
            var id = cards[index].GetID();
            try
            {
                if (id.IsBasic() || !visibleConditionCards.ContainsKey(id))
                {
                    if (currentVisibleCards?.Contains(id) == false)
                    {
                        return CardEquipState.OnlyPageLimit;
                    }
                    return origin;
                }
                int blockFlag = -1;
                foreach (var info in visibleConditionCards[id])
                {
                    var condition = info.visibleCondition;
                    if (condition is BattlePageInfo.VisibleCondition.MultiDeck m)
                    {
                        if (blockFlag == -1) blockFlag = 0;
                        var book = UI.UIController.Instance.CurrentUnit.bookItem.BookId;
                        if (book.packageId == m.packageId || book.id == m.corePageId)
                        {
                            var cardList = GetCurrentEquipDeckCardList();
                            if (cardList.deckTabsController.GetCurrentIndex() == m.deckIndex)
                            {
                                blockFlag = 1;
                            }
                        }
                    }
                }
                if (blockFlag == 0)
                {
                    return CardEquipState.OnlyPageLimit;
                }
                return origin;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return origin;
            }
        }

        [HarmonyPatch(typeof(UIEquipDeckCardList), "SetDeckLayout")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetDeckLayout(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(BookModel), "IsMultiDeck");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MultiDeckPatch), "IsLoAMultiDeck"));
                }
            }
        }

        private static bool IsLoAMultiDeck(bool origin, UIEquipDeckCardList __instance)
        {
            if (dictionary.ContainsKey(__instance))
            {
                dictionary.SafeGet(__instance).UpdateLayout(__instance.currentunit);

                return origin;
            }
            else
            {
                var com = __instance.gameObject.AddComponent<MultiDeckComponent>();
                dictionary[__instance] = com;
                com.parent = __instance;
                com.UpdateLayout(__instance.currentunit);
                return origin;
            }
        }

        [HarmonyPatch(typeof(UIInvenCardListScroll), "SetData")]
        [HarmonyPrefix]
        private static void Before_SetInvenData(UIInvenCardListScroll __instance, List<DiceCardItemModel> cards, UnitDataModel unitData)
        {
            try
            {
                currentMultideckTarget = null;
                currentVisibleCards = null;
                var bookId = unitData?.bookItem?.BookId;
                if (bookId == null) return;

                var targetInfo = infos.SafeGet(bookId);
                if (targetInfo == null) return;

                currentMultideckTarget = targetInfo;
                var unitDeck = unitData.bookItem._deck;
                for (int i = 0; i < targetInfo.infos.Count; i++)
                {
                    var matchInfo = targetInfo.infos[i];
                    var currentDeck = unitDeck._deck;

                    var deckCards = FrameworkExtension.GetSafeAction(() => matchInfo.visibleCards?.Invoke(currentDeck));
                    if (deckCards != null)
                    {
                        // 전용 덱 카드 준비
                        ResetDeckCards(matchInfo, deckCards);
                        var specialCards = MultiDeckPatch.cards.SafeGet(matchInfo);
                        if (specialCards != null)
                        {
                            // 임시로 전용 카드들을 인벤토리에 추가
                            cards.AddRange(specialCards);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }

    class MultiDeckComponent : MonoBehaviour
    {
        public UIEquipDeckCardList parent;
        private HorizontalLayoutGroup linearLayout;
        private bool init = false;
        private bool reduced = false;
        private LorId lastBookId = null;
        private string[] lastTextNames = null;
        private bool checkFlag = false;
        private MultiDeckInfo currentInfo;
        private MultiDeckButton[] buttons;

        public void UpdateLayout(UnitDataModel unit)
        {
            var bookId = unit.bookItem.BookId;
            // 같은 책이 또 보여진 경우는 처리 없이 무시
            if (bookId == lastBookId) return;
            lastBookId = bookId;
            var info = MultiDeckPatch.infos.SafeGet(bookId);

            // 대응하는 멀티덱 정보가 잇는경우
            if (info != null)
            {
                if (!init) Init();
                currentInfo = info;
                var count = Math.Min(info.infos.Count, 4);
                linearLayout.childAlignment = count <= 4 ? TextAnchor.LowerCenter : TextAnchor.LowerLeft;
                for (int i = 0; i < 4; i++)
                {
                    if (!reduced)
                    {
                        buttons[i].LastTextName = parent.deckTabsController.CustomTabs[i].TabName.text;
                    }
                    buttons[i].Visible = i < count;
                }
                reduced = true;
                checkFlag = false;
            }
            // 대응하는 멀티덱 정보가 없고, 이전에 개별 멀티덱용으로 줄인경우
            else if (reduced)
            {
                linearLayout.childAlignment = TextAnchor.LowerLeft;
                for (int i = 0; i < 4; i++)
                {
                    buttons[i].tabText.text = buttons[i].LastTextName;
                    buttons[i].Visible = true;
                }
                reduced = false;
                checkFlag = true;
            }

            return;
        }

        private void Update()
        {
            if (reduced && !checkFlag)
            {
                var count = Math.Min(currentInfo.infos.Count, 4);
                for (int i = 0; i < count; i++)
                {
                    buttons[i].tabText.text = currentInfo.infos[i].name;
                }
            }
        }

        private void OnEnable()
        {
            if (parent != null)
            {
                checkFlag = false;
                UpdateLayout(UI.UIController.Instance.CurrentUnit);
            }
        }

        private void Init()
        {
            init = true;
            parent.multiDeckLayout.gameObject.SetActive(true);
            linearLayout = GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "CustomTabs");
            lastTextNames = new string[4];
            buttons = parent.deckTabsController.CustomTabs.Select(x =>
            {
                var com = x.gameObject.AddComponent<MultiDeckButton>();
                com.tabText = x.TabName;
                return com;
            }).ToArray();
        }
    }

    class MultiDeckButton : MonoBehaviour
    {
        public bool _visible = true;
        private string _lastTextName;
        public string LastTextName
        {
            get => _lastTextName;
            set
            {
                _lastTextName = value;
                tabText.text = value;
            }
        }

        public TextMeshProUGUI tabText;


        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                gameObject.SetActive(value);
            }
        }

        public void OnEnable()
        {
            if (!Visible)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
