using LoALoader.Model;
using LOR_DiceSystem;
using LOR_XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using Workshop;

namespace LibraryOfAngela
{
    class LoAXmlLoader
    {
        private Dictionary<string, List<StageClassInfo>> modStages = new Dictionary<string, List<StageClassInfo>>();
        private Dictionary<string, List<PassiveXmlInfo>> modPassives = new Dictionary<string, List<PassiveXmlInfo>>();
        private Dictionary<string, List<EnemyUnitClassInfo>> modEnemys = new Dictionary<string, List<EnemyUnitClassInfo>>();

        private Dictionary<string, List<BookXmlInfo>> modBooks = new Dictionary<string, List<BookXmlInfo>>();

        private Dictionary<string, List<DiceCardXmlInfo>> modCards = new Dictionary<string, List<DiceCardXmlInfo>>();

        private Dictionary<string, List<DeckXmlInfo>> modDeck = new Dictionary<string, List<DeckXmlInfo>>();

        private Dictionary<string, List<DropBookXmlInfo>> modDrops = new Dictionary<string, List<DropBookXmlInfo>>();

        private Dictionary<string, List<CardDropTableXmlInfo>> modCardDrops = new Dictionary<string, List<CardDropTableXmlInfo>>();

        private List<FormationXmlInfo> formations = new List<FormationXmlInfo>();

        private Dictionary<string, List<WorkshopSkinData>> modSkins = new Dictionary<string, List<WorkshopSkinData>>();

        private Dictionary<string, List<BookDesc>> modStories = new Dictionary<string, List<BookDesc>>();

        public void Combine()
        {
            foreach (var pair in modStages)
            {
                StageClassInfoList.Instance.AddStageByMod(pair.Key, pair.Value);
            }
            foreach (var pair in modPassives)
            {
                PassiveXmlList.Instance.AddPassivesByMod(pair.Value);
            }
            foreach (var pair in modEnemys)
            {
                pair.Value.ForEach(d =>
                {
                    if (d.bookId is null || d.bookId.Count == 0)
                    {
                        d.bookId = new List<int> { d._id };
                    }
                    if (d.nameId > 0)
                    {
                        d.name = CharactersNameXmlList.Instance.GetName(d.nameId);
                    }
                });

    /*            if (EnemyUnitClassInfoList.Instance._workshopEnemyDict.ContainsKey(pair.Key))
                {
                    pair.Value.AddRange(EnemyUnitClassInfoList.Instance._workshopEnemyDict[pair.Key]);
                    EnemyUnitClassInfoList.Instance._workshopEnemyDict.Remove(pair.Key);
                }*/
                EnemyUnitClassInfoList.Instance.AddEnemyUnitByMod(pair.Key, pair.Value);
            }
            foreach (var pair in modBooks)
            {
                BookXmlList.Instance._workshopBookDict.Remove(pair.Key);
                pair.Value.ForEach(d =>
                {
                    if (d.TextId > 0)
                    {
                        d.InnerName = Singleton<BookDescXmlList>.Instance.GetBookName(new LorId(d.TextId));
                    }
                });
                BookXmlList.Instance.AddEquipPageByMod(pair.Key, pair.Value);
            }
            foreach (var pair in modCards)
            {
                pair.Value.ForEach(d =>
                {
                    if (d._textId > 0)
                    {
                        d.workshopName = Singleton<BattleCardDescXmlList>.Instance.GetCardDesc(new LorId(d._textId)).cardName;
                    }
                });
                ItemXmlDataList.instance.AddCardInfoByMod(pair.Key, pair.Value);
            }
            foreach(var pair in modDeck)
            {
                DeckXmlList.Instance.AddDeckByMod(pair.Value);
            }
            foreach (var pair in modCardDrops)
            {
                CardDropTableXmlList.Instance.AddCardDropTableByMod(pair.Key, pair.Value);
            }
            foreach (var pair in modDrops)
            { 
                DropBookXmlList.Instance.SetDropTableByMod(pair.Value);
                DropBookXmlList.Instance.AddBookByMod(pair.Key, pair.Value);
            }

            FormationXmlList.Instance._list.AddRange(formations);

            //Debug.Log($"LoA :: Job End Finish Detect 444 : {modSkins.Count}");
            foreach (var pair in modSkins)
            {
                // Debug.Log($"LoA :: Job End Finish Detect 555 : {pair.Key} // {pair.Value.Count}");
                CustomizingBookSkinLoader.Instance._bookSkinData.Remove(pair.Key);
                Singleton<CustomizingBookSkinLoader>.Instance.AddBookSkinData(pair.Key, pair.Value);
            }

            foreach (var pair in modStories)
            {
                BookDescXmlList.Instance.AddBookTextByMod(pair.Key, pair.Value);
            }

            modStages.Clear();
            modPassives.Clear();
            modEnemys.Clear();
            modBooks.Clear();
            modCards.Clear();
            modCardDrops.Clear();
            modDeck.Clear();
        }

        public void InsertStage(string packageId, List<StageClassInfo> stages)
        {
            InsertOrUpdate(packageId, stages, modStages);
        }

        public void InsertPassives(string packageId, List<PassiveXmlInfo> passives)
        {
            InsertOrUpdate(packageId, passives, modPassives);
        }

        public void InsertEnemys(string packageId, List<EnemyUnitClassInfo> enemys)
        {
            InsertOrUpdate(packageId, enemys, modEnemys);
        }

        public void InsertBooks(string packageId, List<BookXmlInfo> books)
        {
            InsertOrUpdate(packageId, books, modBooks);
        }

        public void InsertCards(string packageId, List<DiceCardXmlInfo> items)
        {
            InsertOrUpdate(packageId, items, modCards);
        }

        public void InsertDecks(string packageId, List<DeckXmlInfo> decks)
        {
            InsertOrUpdate(packageId, decks, modDeck);
        }

        public void InsertDrops(string packageId, List<DropBookXmlInfo> dropBooks)
        {
            InsertOrUpdate(packageId, dropBooks, modDrops);
        }

        public void InsertCardDrops(string packageId, List<CardDropTableXmlInfo> cardDrops)
        {
            InsertOrUpdate(packageId, cardDrops, modCardDrops);
        }

        // 원어에 한해서는 Localize를 안두고 직접 data에 적는 경우가 있으므로 파싱에서도 유지
        public void InsertBookStories(string packageId, List<BookDesc> stories)
        {
            InsertOrUpdate(packageId, stories, modStories);
        }

        public void InsertFormation(List<FormationXmlInfo> formations)
        {
            this.formations.AddRange(formations);
        }

        public void InsertSkins(string packageId, LoAWorkshopAppearanceInfo skin)
        {
            var s = new LoAWorkshopSkinData
            {
                contentFolderIdx = packageId,
                id = 0,
                dic = skin.clothCustomInfo,
                prefab = skin.prefab,
                dataName = skin.bookName
            };

            if (modSkins.ContainsKey(packageId)) modSkins[packageId].Add(s);
            else modSkins[packageId] = new List<WorkshopSkinData> { s };
        }


        private void InsertOrUpdate<T>(string packageId, List<T> list, Dictionary<string, List<T>> dic)
        {
            if (dic.ContainsKey(packageId)) dic[packageId].AddRange(list);
            else dic[packageId] = list;
        }
    }
}
