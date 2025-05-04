using LibraryOfAngela;
using LoALoader.Model;
using LOR_DiceSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using Workshop;

namespace LoALoader
{
    internal struct FileParseRequest
    {
        public string packageId;
        public string modName;
        public string path;
        public byte[] bytes;
        public FileType type;
        public TaskCompletionSource<bool> awaitSource;
    }

    struct CardWorkParse
    {
        public string packageId;
        public List<ArtworkCustomizeData> datas;
    }

    public class FileParser
    {
        static FileParser()
        {
            ThreadPool.QueueUserWorkItem(EnqueueJob);
        }

        private static ConcurrentQueue<FileParseRequest> initQueue = new ConcurrentQueue<FileParseRequest>();
        private static ConcurrentQueue<FileParseRequest> queue = new ConcurrentQueue<FileParseRequest>();
        private static AutoResetEvent queueNotifier = new AutoResetEvent(false);
        private static LoAXmlLoader loader = new LoAXmlLoader();
        internal static ConcurrentQueue<CardWorkParse> cardworks = new ConcurrentQueue<CardWorkParse>();
        private static TaskCompletionSource<bool> dataWaitSource;
        private static TaskCompletionSource<bool> cardWorkwaitSource = new TaskCompletionSource<bool>();
        private static TaskCompletionSource<bool> callInitalizerSource = new TaskCompletionSource<bool>();
        private static List<Task> cardWorkTasks = new List<Task>();
        private static bool initCheckSkip = false;
        internal static ConcurrentQueue<FileParseRequest> assembliePaths = new ConcurrentQueue<FileParseRequest>();

        internal static Task Enqueue(string packageId, string modName, string path, byte[] bytes, FileType type)
        {
            var source = new TaskCompletionSource<bool>();
            var request = new FileParseRequest { packageId = packageId, modName = modName, path = path, bytes = bytes, type = type, awaitSource = source };
            if (type == FileType.INIT)
            {
                initQueue.Enqueue(request);
            }
            else
            {
                queue.Enqueue(request);
            }
            queueNotifier.Set();
            return source.Task;
        }

        public static void InitCheckSkip()
        {
            initCheckSkip = true;
        }

        public static async Task WaitDataComplete()
        {
            Debug.Log("LoA :: Call Job End");
            dataWaitSource = new TaskCompletionSource<bool>();
            queueNotifier.Set();
            await dataWaitSource.Task;
        }

        public static async Task WaitCardWorkComplete()
        {
            await cardWorkwaitSource.Task;
        }

        public static async Task WaitCallInitializerComplete()
        {
            await callInitalizerSource.Task;
        }

        public static void CallInitializerComplete()
        {
            callInitalizerSource.SetResult(true);
        }


        private static void EnqueueJob(object param)
        {
            while (true)
            {
                if (!initCheckSkip) queueNotifier.WaitOne(300);
                while (true)
                {
                    bool flag = false;
                    if (!initCheckSkip && initQueue.TryDequeue(out FileParseRequest request))
                    {
                        //Debug.Log($"Init Request : {request.packageId}");
                        ParseInit(request.packageId, request.modName, request.path);
                        request.awaitSource.SetResult(true);
                        continue;
                    }
                    if (queue.TryDequeue(out FileParseRequest request2))
                    {
                        //Debug.Log($"Init Request 2 : {request2.path}");
                        flag = true;
                        try
                        {
                            switch (request2.type)
                            {
                                case FileType.DATA:
                                    ParseData(request2.packageId, request2.path, request2.bytes);
                                    break;
                                case FileType.SKIN:
                                    ParseSkin(request2.packageId, request2.path, request2.bytes);
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }

                        request2.awaitSource.SetResult(true);
                    }
                    // Debug.Log("LoA :: Wait 3" + flag);
                    if (!flag) break;
                }
                if (!initCheckSkip) continue;

                callInitalizerSource.Task.Wait();

                // 런타임에서 명시적으로 data wait 을 호출해주지않았다면 더 추가될수있으므로 다시 루프
                if (dataWaitSource is null) continue;

                try
                {
                    loader.Combine();
                    Debug.Log("LoA :: Data Job End, Finish");
                }
                catch (Exception e)
                {
                    Debug.Log("LoA :: Data Job Finish Fail");
                    Debug.LogError(e);
                }

                dataWaitSource.SetResult(true);
                break;
            }
            Task.WhenAll(cardWorkTasks).Wait();
            foreach (var d in cardworks)
            {
                Singleton<CustomizingCardArtworkLoader>.Instance.AddArtworkData(d.packageId, d.datas);
            }
            cardWorkwaitSource.SetResult(true);
        }

        private static void ParseData(string packageId, string path, byte[] bytes)
        {
            var name = Path.GetFileName(path);
            var target = bytes;
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
                loader.InsertStage(packageId, stages);

            }

            if (name.StartsWith("PassiveList"))
            {
                var passives = getContents<PassiveXmlRoot, PassiveXmlInfo>(target, (x) => x.list);
                passives.ForEach(x => x.workshopID = packageId);
                loader.InsertPassives(packageId, passives);
            }
            else if (name.StartsWith("EnemyUnitInfo"))
            {
                var enemys = getContents<EnemyUnitClassRoot, EnemyUnitClassInfo>(target, (x) => x.list);
                enemys.ForEach(x => x.workshopID = packageId);
                loader.InsertEnemys(packageId, enemys);
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
                loader.InsertBooks(packageId, equipPages);
            }
            else if (name.StartsWith("CardInfo"))
            {
                var cardInfos = getContents<DiceCardXmlRoot, DiceCardXmlInfo>(target, (x) => x.cardXmlList);
                cardInfos.ForEach(x => x.workshopID = packageId);
                loader.InsertCards(packageId, cardInfos);
            }
            else if (name.StartsWith("Deck"))
            {
                var deck = getContents<DeckXmlRoot, DeckXmlInfo>(target, (x) => x.deckXmlList);
                deck.ForEach(x =>
                {
                    x.workshopId = packageId;
                    LorId.InitializeLorIds<LorIdXml>(x._cardIdList, x.cardIdList, packageId);
                });
                loader.InsertDecks(packageId, deck);
            }
            else if (name.StartsWith("Dropbook"))
            {
                var dropBook = getContents<BookUseXmlRoot, DropBookXmlInfo>(target, (x) => x.bookXmlList);
                dropBook.ForEach(x =>
                {
                    x.workshopID = packageId;
                    x.InitializeDropItemList(packageId);
                });
                loader.InsertDrops(packageId, dropBook);
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
                loader.InsertCardDrops(packageId, tables);
            }
            else if (name.StartsWith("FormationInfo"))
            {
                var tables = getContents<FormationXmlRoot, FormationXmlInfo>(target, x => x.list);
                loader.InsertFormation(tables);
            }
        }

        private static void ParseSkin(string packageId, string path, byte[] bytes)
        {
            LoAWorkshopAppearanceInfo __result = new LoAWorkshopAppearanceInfo();

            XmlSerializer serializer = new XmlSerializer(typeof(Model.ModInfo));
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                Model.ModInfo modInfo = (Model.ModInfo)serializer.Deserialize(memoryStream);
                ClothInfo clothInfo = modInfo.ClothInfo;
                __result.isClothCustom = true;
                __result.uniqueId = packageId;
                __result.path = Path.GetDirectoryName(path);
                __result.bookName = Directory.GetParent(path).Name;
 /*               if (!string.IsNullOrEmpty(clothInfo.Name))
                {
                    __result.bookName = clothInfo.Name;
                }*/
                var hasPrefab = !string.IsNullOrEmpty(clothInfo.Prefab);
                __result.prefab = clothInfo.Prefab;

                path = Path.Combine(__result.path, "ClothCustom");
                // 각 ActionDetail에 대해 CallSomething 호출
                InjectMotion(packageId, path, ActionDetail.Default, clothInfo.Default, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Guard, clothInfo.Guard, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Evade, clothInfo.Evade, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Damaged, clothInfo.Damaged, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Slash, clothInfo.Slash, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Penetrate, clothInfo.Penetrate, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Hit, clothInfo.Hit, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Move, clothInfo.Move, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Standing, clothInfo.Standing, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.NONE, clothInfo.NONE, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Fire, clothInfo.Fire, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Aim, clothInfo.Aim, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Special, clothInfo.Special, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S1, clothInfo.S1, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S2, clothInfo.S2, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S3, clothInfo.S3, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S4, clothInfo.S4, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S5, clothInfo.S5, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Slash2, clothInfo.Slash2, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Penetrate2, clothInfo.Penetrate2, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.Hit2, clothInfo.Hit2, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S6, clothInfo.S6, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S7, clothInfo.S7, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S8, clothInfo.S8, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S9, clothInfo.S9, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S10, clothInfo.S10, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S11, clothInfo.S11, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S12, clothInfo.S12, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S13, clothInfo.S13, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S14, clothInfo.S14, hasPrefab, __result);
                InjectMotion(packageId, path, ActionDetail.S15, clothInfo.S15, hasPrefab, __result);

                loader.InsertSkins(packageId, __result);
            }
        }

        private static void ParseInit(string packageId, string modName, string path)
        {
            FileLoader.LoadAll(packageId, modName, Path.Combine(path, "Data"), FileType.DATA, true);
            FileLoader.LoadAll(packageId, modName, Path.Combine(path, "Resource", "CharacterSkin"), FileType.SKIN, true);
            cardWorkTasks.Add(FileLoader.LoadAll(packageId, modName, Path.Combine(path, "Resource", "CombatPageArtwork"), FileType.CARDWORK, false));
        }
       

        public static List<R> getContents<T, R>(byte[] data, Func<T, List<R>> targetFindCallback)
        {
            if (data == null || data.Length == 0) return new List<R>();

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    T charactersNameRoot = (T)new XmlSerializer(typeof(T)).Deserialize(memoryStream);
                    var list = targetFindCallback(charactersNameRoot);
                    if (list != null) return list;
                    return new List<R>();
                }
            }
            catch (Exception e)
            {
                Debug.Log("Xml Parse Fail, Please Check the provided data.");
                Debug.LogError(e);
                return new List<R>();
            }
        }

        private static void InjectMotion(string packageId, string path, ActionDetail action, ActionDetailInfo info, bool hasPrefab, LoAWorkshopAppearanceInfo result)
        {
            if (info is null) return;

            Vector2 pivotPos = new Vector2((info.Pivot.PivotX + 512f) / 1024f, (info.Pivot.PivotY + 512f) / 1024f);
            Vector2 headPos = new Vector2(info.Head.HeadX / 100f, info.Head.HeadY / 100f);

            string spritePath = null;
            string frontSpritePath = null;

            var spriteExists = false;
            var frontSpriteExists = false;

            if (!hasPrefab)
            {
                spritePath = Path.Combine(path, action.ToString() + ".png");
                frontSpritePath = Path.Combine(path, action.ToString() + "_front.png");
                spriteExists = File.Exists(spritePath);
                frontSpriteExists = File.Exists(frontSpritePath);
            }

            ClothCustomizeData value = new ClothCustomizeData
            {
                spritePath = spritePath,
                frontSpritePath = frontSpritePath,
                hasFrontSprite = frontSpriteExists,
                pivotPos = pivotPos,
                headPos = headPos,
                headRotation = info.Head.Rotation,
                direction = info.Direction == "Side" ? CharacterMotion.MotionDirection.SideView : CharacterMotion.MotionDirection.FrontView,
                headEnabled = info.Head.HeadEnable,
                hasFrontSpriteFile = frontSpriteExists,
                hasSpriteFile = spriteExists
            };

            result.clothCustomInfo[action] = value;
        }
    }
}
