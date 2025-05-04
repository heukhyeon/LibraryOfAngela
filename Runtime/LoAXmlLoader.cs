using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace LibraryOfAngela
{
    class LoAXmlLoader
    {
        private readonly IEnumerable<ILoACustomDataMod> mods;
        private Dictionary<string, List<StageClassInfo>> modStages = new Dictionary<string, List<StageClassInfo>>();
        private Dictionary<string, List<PassiveXmlInfo>> modPassives = new Dictionary<string, List<PassiveXmlInfo>>();
        private Dictionary<string, List<EnemyUnitClassInfo>> modEnemys = new Dictionary<string, List<EnemyUnitClassInfo>>();

        private Dictionary<string, List<BookXmlInfo>> modBooks = new Dictionary<string, List<BookXmlInfo>>();

        private Dictionary<string, List<DiceCardXmlInfo>> modCards = new Dictionary<string, List<DiceCardXmlInfo>>();

        private Dictionary<string, List<DeckXmlInfo>> modDeck = new Dictionary<string, List<DeckXmlInfo>>();

        private Dictionary<string, List<DropBookXmlInfo>> modDrops = new Dictionary<string, List<DropBookXmlInfo>>();

        private Dictionary<string, List<CardDropTableXmlInfo>> modCardDrops = new Dictionary<string, List<CardDropTableXmlInfo>>();

        private List<FormationXmlInfo> formations = new List<FormationXmlInfo>();

        public LoAXmlLoader(IEnumerable<ILoACustomDataMod> mods)
        {
            this.mods = mods;
        }

        public void Start()
        {
            foreach (var mod in mods) inject(mod);
        }

        public void Combine()
        {
            foreach(var pair in modStages)
            {
                StageClassInfoList.Instance.AddStageByMod(pair.Key, pair.Value);
            }
            foreach (var pair in modPassives)
            {
                PassiveXmlList.Instance.AddPassivesByMod(pair.Value);
            }
            foreach (var pair in modEnemys)
            {
                EnemyUnitClassInfoList.Instance.AddEnemyUnitByMod(pair.Key, pair.Value);
            }
            foreach (var pair in modBooks)
            {
                BookXmlList.Instance.AddEquipPageByMod(pair.Key, pair.Value);
            }
            foreach (var pair in modCards)
            {
                ItemXmlDataList.instance.AddCardInfoByMod(pair.Key, pair.Value);
            }
            foreach(var pair in modDeck)
            {
                DeckXmlList.Instance.AddDeckByMod(pair.Value);
            }
            foreach (var pair in modDrops)
            {
                DropBookXmlList.Instance.AddBookByMod(pair.Key, pair.Value);
            }
            FormationXmlList.Instance._list.AddRange(formations);
 
            modStages.Clear();
            modPassives.Clear();
            modEnemys.Clear();
            modBooks.Clear();
            modCards.Clear();
            modDeck.Clear();
            foreach (var m in mods)
            {
                try
                {
                    m.OnXmlDataLoadComplete();
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        public void inject(ILoACustomDataMod targetMod)
        {
            var path = Path.Combine(targetMod.path, targetMod.customDataPath ?? "Data");
            var packageId = targetMod.packageId;

            foreach(var target in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(target);
                if (name.StartsWith("StageInfo"))
                {
                    var stages = getContents<StageXmlRoot, StageClassInfo>(target, (x) => x.list);
                    stages.ForEach(x =>
                    {
                        x.workshopID = packageId;
                        x.InitializeIds(packageId);
                        x.storyList.ForEach(story =>
                        {
                            story.packageId = packageId;
                            story.valid = true;
                        });
                    });
                    InsertOrUpdate(packageId, stages, modStages);
                }

                if (name.StartsWith("PassiveList"))
                {
                    var passives = getContents<PassiveXmlRoot, PassiveXmlInfo>(target, (x) => x.list);
                    passives.ForEach(x => x.workshopID = packageId);
                    InsertOrUpdate(packageId, passives, modPassives);
                }
                else if (name.StartsWith("EnemyUnitInfo"))
                {
                    var enemys = getContents<EnemyUnitClassRoot, EnemyUnitClassInfo>(target, (x) => x.list);
                    enemys.ForEach(x => x.workshopID = packageId);
                    InsertOrUpdate(packageId, enemys, modEnemys);
                }
                else if (name.StartsWith("EquipPage"))
                {
                    var equipPages = getContents<BookXmlRoot, BookXmlInfo>(target, (x) => x.bookXmlList);
                    equipPages.ForEach(x =>
                    {
                        x.workshopID = packageId;
                        x.motionSoundList.Clear();
                        LorId.InitializeLorIds<LorIdXml>(x.EquipEffect._PassiveList, x.EquipEffect.PassiveList, x.workshopID);
                    });
                    InsertOrUpdate(packageId, equipPages, modBooks);
                }
                else if (name.StartsWith("CardInfo"))
                {
                    var cardInfos = getContents<DiceCardXmlRoot, DiceCardXmlInfo>(target, (x) => x.cardXmlList);
                    cardInfos.ForEach(x => x.workshopID = packageId);
                    InsertOrUpdate(packageId, cardInfos, modCards);
                }
                else if (name.StartsWith("Deck"))
                {
                    var deck = getContents<DeckXmlRoot, DeckXmlInfo>(target, (x) => x.deckXmlList);
                    deck.ForEach(x =>
                    {
                        x.workshopId = packageId;
                        LorId.InitializeLorIds<LorIdXml>(x._cardIdList, x.cardIdList, packageId);
                    });
                    InsertOrUpdate(packageId, deck, modDeck);
                }
                else if (name.StartsWith("Dropbook"))
                {
                    var dropBook = getContents<BookUseXmlRoot, DropBookXmlInfo>(target, (x) => x.bookXmlList);
                    dropBook.ForEach(x =>
                    {
                        x.workshopID = packageId;
                        x.InitializeDropItemList(packageId);
                    });
                    InsertOrUpdate(packageId, dropBook, modDrops);
                }
                else if (name.StartsWith("CardDropTable"))
                {
                    var tables = getContents<CardDropTableXmlRoot, CardDropTableXmlInfo>(target, x => x.dropTableXmlList);
                    foreach (var x in tables)
                    {
                        x.workshopId = packageId;
                        x.cardIdList.Clear();
                        LorId.InitializeLorIds<LorIdXml>(x._cardIdList, x.cardIdList, packageId);
                    }
                    InsertOrUpdate(packageId, tables, modCardDrops);
                }
                else if (name.StartsWith("FormationInfo"))
                {
                    var tables = getContents<FormationXmlRoot, FormationXmlInfo>(target, x => x.list);
                    formations.AddRange(tables);
                }
            }
        }

        public static List<R> getContents<T, R>(string path, Func<T, List<R>> targetFindCallback)
        {
            if (!File.Exists(path)) return new List<R>();

            try
            {
                using (StringReader stringReader3 = new StringReader(File.ReadAllText(path)))
                {
                    T charactersNameRoot = (T)new XmlSerializer(typeof(T)).Deserialize(stringReader3);
                    var list = targetFindCallback(charactersNameRoot);
                    if (list != null) return list;
                    return new List<R>();
                }
            }
            catch (Exception e)
            {
                Logger.Log("Xml Parse Fail, Please Check :" + path);
                Logger.LogError(e);
                return new List<R>();
            }

        }

        private void InsertOrUpdate<T>(string packageId, List<T> list, Dictionary<string, List<T>> dic)
        {
            if (dic.ContainsKey(packageId)) dic[packageId].AddRange(list);
            else dic[packageId] = list;
        }
    }
}
