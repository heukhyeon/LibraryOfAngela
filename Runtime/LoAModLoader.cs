using HarmonyLib;
using HarmonyLib.Tools;
using LibraryOfAngela.Battle;
using LibraryOfAngela.BattleUI;
using LibraryOfAngela.Buf;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.Emotion;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Map;
using LibraryOfAngela.Save;
using LibraryOfAngela.SD;
using LibraryOfAngela.Story;
using LibraryOfAngela.Util;
using LoALoader;
using LOR_XML;
using Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Title;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LibraryOfAngela
{
    class LoAModLoader
    {
        private float _progress = 0f;
        public float modLoadingProgress
        {
            get => _progress;
            set
            {
                _progress = value;
                if (loaderProgressField != null)
                {
                    loaderProgressField.SetValue(null, value);
                }
               // Debug.Log($"TEST 2 :::: {value} // {loaderProgressField?.GetValue(null)}");
            }
        }

        private static bool isCompleted = false;
        private List<Assembly> assemblies;
        private FieldInfo loaderProgressField;
        private static bool isTimeout = false;

        public LoAModLoader(List<Assembly> assemblies)
        {
            this.assemblies = assemblies;
            var type = Type.GetType("LoALoader.LoadingProgress, LoALoader");
            if (type != null)
            {
                AccessTools.Field(type, "maxTimeout")?.SetValue(null, 100f);
                loaderProgressField = AccessTools.Field(type, "modLoadingProgress");
               // Debug.Log($"TEST 1 :::: {loaderProgressField != null}");
                AccessTools.Field(type, "onTimeout")?.SetValue(null, new Action(() =>
                {
                    if (!isTimeout)
                    {
                        isTimeout = true;
                        Logger.Log("Init Timeout...?");
                        Logger.Flush();
                        LoAFramework.PreloadBattleUiBundle();
                    }

                }));
            }
        }

        public void InitHarmony()
        {
            var alreadyLoadedAssembly = AssemblyManager.Instance.GetFieldValue<Dictionary<string, List<Assembly>>>("_assemblyDict");
            var target = new List<string> { "0Harmony", "MonoMod.Utils", "Mono.Cecil", "MonoMod.RuntimeDetour" };
            foreach (var assembly in alreadyLoadedAssembly.Values.SelectMany(x => x).Select(x => x.GetName().Name))
            {
                if (target.Contains(assembly))
                {
                    Logger.Log($"{assembly} is Loaded From Other Mod, Load Skip");
                    target.Remove(assembly);
                }
            }
            if (target.Count > 0)
            {
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(GetType().Assembly.Location)))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    if (target.Contains(name))
                    {
                        Assembly.LoadFile(file);
                    }
                }
            }
        }

        // GC 방지
        private static Task loaLoadingTask;
        private static long manualStartTime;

        public void Start()
        {
            modLoadingProgress = 0f;
            Debug.Log("LoA :: Initialize Start");
            modLoadingProgress += 0.1f;
            manualStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            bool isSuccess = false;
            try
            {
                typeof(UITitleController).Patch("Initialize");
                SceneManager.sceneLoaded += SceneLoaded;
                loaLoadingTask = Task.Run(LoadStart);
            }
            catch (Exception)
            {
                Logger.Log($"Attempted to load, but failed too many times. Force resume to prevent other modes from blocking.");
                modLoadingProgress = 1f;
            }
        }

        private long initTime;

        private async void LoadStart()
        {
            var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long callInitializerCompleteTime = 0L;
            initTime = time;
            try
            {
                Debug.Log("LoARuntime Init Task Start");

                var _ = Task.Run(async () =>
                {
                    while (modLoadingProgress < 1f)
                    {
                        await Task.Delay(5000);
                        Logger.CheckFlush(modLoadingProgress);
                    }
                });

                var callInitializerCompleteTask = FileParser.WaitCallInitializerComplete().ContinueWith((t) =>
                {
                    callInitializerCompleteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    Logger.Log($"When Call Initializer in Progress : {modLoadingProgress}");
                });

                LoAModCache.Instance.Initialize(assemblies);
                LogProgress("ModCache", 0.05f);
                
                assemblies = null;
                Execute("OnPreLoad", (x) => x.OnPreLoad());

                var dataTask = LoadDatas();

                var artworkTask = LoAArtworks.Instance.Initialize();
                LogProgress("Artwork", 0.05f);

                var localizeTask = LoadLocalizeFiles();
                var localizeCompleteTask = localizeTask.ContinueWith((t) =>
                {
                    dataTask.Wait();
                    foreach (var l in t.Result)
                    {
                        l.Combine();
                    }
                    BufPatch.InitLoABufEffectInfo();
                });

                var onInitializeModTask = localizeCompleteTask.ContinueWith((t) =>
                {
                    Execute("OnInitializeMod", (x) => x.OnInitializeMod());
                });

                var completeLoadTask = onInitializeModTask.ContinueWith((t) =>
                {
                    Execute("OnCompleteLoad", (x) => x.OnCompleteLoad());
                });

                AdvancedEquipBookPatch.Instance.Initialize();
                LogProgress("AdvancedEquipBook", 0.1f);
                var effectTask = AdvancedEquipBookPatch.Instance.EnqueueEffect();
                var emotionLoadTask = Task.Run(() =>
                {
                    return LoAEmotionDictionary.Instance.Initialize();
                });

                SkinPatch.Initialize();
                LogProgress("SkinPatch", 0.02f);

                EnemyOver5.Initialize();
                LogProgress("EnemyOver5", 0.02f);

                EmotionOver5.Initialize();
                LogProgress("EmotionOver5", 0.02f);

                EgoPatch.Initialize();
                LogProgress("EgoPatch", 0.06f);

                LoAAssetBundles.Instance.Initialize();
                LogProgress("AssetBundles", 0.1f);

                StoryPatch.Instance.Initialize();
                LogProgress("StoryPatch", 0.02f);

                BattleSettingUIPatch.Initialize();
                LogProgress("BattleSettingUIPatch", 0.02f);

                MultiDeckPatch.Initialize(AdvancedEquipBookPatch.Instance.configs);
                LogProgress("MultiDeckPatch", 0.02f);

                MapPatch.Instance.Initialize();
                LogProgress("MapPatch", 0.02f);

                LoAHistoryController.Instance.Initialize();
                LogProgress("HistoryController", 0.02f);

                BattleInterfaceCache.Instance.Initialize();
                LogProgress("BattleInterfaceCache", 0.02f);

                FixValidPatch.Initialize();
                LogProgress("ValidationPatches", 0.02f);

                CustomCardEffect.Initialize();
                LogProgress("CustomCardEffect", 0.02f);

                BattlePatch.Initialize();
                LogProgress("BattlePatch", 0.02f);

                ExhaustPatch.Initialize();
                LogProgress("ExhaustPatch", 0.02f);

                BattlePagePatch.Instance.Initialize();
                LogProgress("BattlePagePatch", 0.02f);

                AdvancedSuccessionPatch.Instance.Initialize();
                LogProgress("SuccessionPatch", 0.02f);

                LogProgress("AutoBattlePatch", 0.02f);

                BattleLogPatch.Instance.Initialize();
                MovePatch.Initialize();
                LogProgress("UIAndUtilPatches", 0.02f);

                await dataTask;
                LogProgress("DataLoaded", 0.02f);

                await localizeCompleteTask;
                LogProgress("LocalizationComplete", 0.02f);

                // 나중에 구겨넣어야할 놈들 리스트
                AssemblyManager.Instance._initializer.AddRange(LoAModCache.Mods.OfType<ModInitializer>());
                Patch(typeof(BufPatch));
                BufPatch.Instance.Initialize();
                var harmonyTask = PatchHarmonies();
                BookCategoryPatch.Instance.Initialize(AdvancedEquipBookPatch.Instance.configs);
                TimelinePatch.Instance.InitData();
                LogProgress("BookCategoryPatch", 0.0033f);

                AdvancedSkinInfoPatch.Instance.Initialize();
                LogProgress("SkinInfoPatch", 0.0033f);

                AdvancedEquipBookPatch.Instance.OnlyCardInit();
                LogProgress("CardInitialized", 0.0033f);

                WorkshopSkinExportPatch.Instance.Initialize(AdvancedSkinInfoPatch.Instance.infos.Values.ToList());
                MultiDeckPatch.InsertMultideckOption(AdvancedEquipBookPatch.Instance.configs);
                LogProgress("WorkshopAndDeckSetup", 0.01f);

                await effectTask;
                LogProgress("EffectsLoaded", 0.0025f);

                await artworkTask;
                LogProgress("ArtworkLoaded", 0.0025f);

                await emotionLoadTask;
                EmotionPatch.Initialize();
                LogProgress("EmotionPatch", 0.05f);

                await harmonyTask;
                LogProgress("HarmonyComplete", 0.01f);

                Logger.Log("Initialize Complete, Call Child Mode OnInitializeMod");

                await onInitializeModTask;
                LogProgress("ModsInitialized", 0.005f);

                await completeLoadTask;
                LogProgress("ModsLoaded", 0.005f);

                Logger.Log("Initialize Complete, Wait CardWorkComplete");

                await FileParser.WaitCardWorkComplete();
                Logger.Log("CardWorkLoad Complete");
                //LoAAssetBundles.Instance.LoopAsyncAssetBundleLoad();
                LogProgress("Completed", forceValue: 1f);
            }
            catch (Exception e)
            {
                Logger.Log("Unknown Excepion during LoA Initialize");
                Logger.LogError(e);
                modLoadingProgress = 1f;
            }
            var current = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var du = callInitializerCompleteTime - time;
            var final = new StringBuilder($"Initialize Logic End, Duration : {(current - manualStartTime) / 1000.0}s\n");
            final.AppendLine($"- Duration Start Called And Task Create : {(time - manualStartTime) / 1000.0}s");
            final.AppendLine($"- Duration CallInitializer : {du / 1000.0}s");
            final.AppendLine($"- Duration without CallInitializer : {(current - time - du) / 1000.0}s");
            Logger.Log(final.ToString());
        }

        private Task PatchHarmonies()
        {
            return Task.Run(() =>
            {
                var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                Patch(typeof(BookCategoryPatch));
                Patch(typeof(BattlePatch));
                Patch(typeof(BattlePhasePatch));
                Patch(typeof(AdvancedSuccessionPatch));
                Patch(typeof(AdvancedEquipBookPatch));
                Patch(typeof(MapPatch));
                Patch(typeof(AutoBattlePatch));
                Patch(typeof(BattleResultPatch));
                Patch(typeof(BattleLogPatch));
                Patch(typeof(FixValidPatch));
                Patch(typeof(EmotionPatch));
                Patch(typeof(BattlePagePatch));
                Patch(typeof(BattleInterfaceCache));
                Patch(typeof(StoryPatch));
                Patch(typeof(BattleSettingUIPatch));
                Patch(typeof(CustomCardEffect));
                Patch(typeof(FacePatch));
                Patch(typeof(SavePatch));
                Patch(typeof(SkinPatch));
                Patch(typeof(SkinRenderPatch));
                Patch(typeof(EmotionUIPatch));
                Patch(typeof(AdvancedSkinInfoPatch));
                Patch(typeof(LoAAssetBundles));
                Patch(typeof(WorkshopSkinExportPatch));
                Patch(typeof(MultiDeckPatch));
                Patch(typeof(LoAHistoryController));
                Patch(typeof(EnemyOver5));
                Patch(typeof(EmotionOver5));
                Patch(typeof(ExhaustPatch));
                Patch(typeof(SceneBufPatch));
                Patch(typeof(BattleUIPatch));
                Patch(typeof(EgoPatch));
                Patch(typeof(MovePatch));
                Patch(typeof(LoADebugger));
                Patch(typeof(AdvancedCorePageRarityPatch));
                var duration = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - time) / 1000.0;
                Logger.Log($"Harmony Patch Complete, Duration : {duration}s");
            });
        }

        private void Patch(Type targetType)
        {
            try
            {
                InternalExtension.internalPatcher.PatchAll(targetType);
            }
            catch (Exception e)
            {
                Logger.Log($"Harmony Patch Fail in {targetType.Name}");
                Logger.LogError(e);
            }
        }

        private void LogProgress(string stepName, float delta = 0.02f, float forceValue = -1f)
        {
            // 진행률 업데이트
            if (isTimeout)
            {
                throw new TimeoutException("LoA Load Process Already Timeout");
            }

            if (forceValue != -1f)
            {
                modLoadingProgress = forceValue;
            }
            else
            {
                modLoadingProgress += delta;
            }

            // 로그 출력
            var percent = (modLoadingProgress * 100f).ToString("F1");
            var duration = DateTimeOffset.Now.ToUnixTimeMilliseconds() - initTime;
            Logger.Log($"[{percent}%] {stepName} - {duration / 1000.0}s");
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            InitFinalize(true);
        }

        private static void After_Initialize()
        {
            try
            {
                if (!isCompleted)
                {
                    InitFinalize(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                PatcherImpl.FlushLogs();
            }
        }



        private static void InitFinalize(bool fromSceneLoaded)
        {
            if (isCompleted) return;
            var resultBuilder = new StringBuilder("LoA :: Call InitFinalize\n");
            resultBuilder.AppendLine($"- Task Exist : {loaLoadingTask != null}");
            resultBuilder.AppendLine($"- Task Complete : {loaLoadingTask?.IsCompleted} // {loaLoadingTask?.IsCanceled} // {loaLoadingTask?.IsFaulted}");
            resultBuilder.AppendLine($"- From SceneLoaded : {fromSceneLoaded}");
            Debug.Log(resultBuilder.ToString());
            isCompleted = true;
            LoAAssetBundles.Instance.LoopAsyncAssetBundleLoad();
            new AdvancedCorePageRarityPatch().Initialize();
            Logger.Log("Init Complete");
            Logger.Flush();
            LoAFramework.PreloadBattleUiBundle();
        }

        private void Execute(string name, Action<ILoAMod> action)
        {
            Logger.Log($"Mod {name} Invoke Start");
            foreach (var mod in LoAModCache.Mods)
            {
                try
                {
                    var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    action(mod);
                    var d = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - time) * 0.001f;
                    Logger.Log($"Mod {mod.packageId} -> {name} Excecute : {d.ToString("F1")}s");
                }
                catch (Exception e)
                {
                    Logger.Log($"Mod {name} Invoke Error (From {mod.packageId} ({mod.path}))");
                    Logger.LogError(e);
                }
            }
            Logger.Log($"Mod {name} Invoke Complete");
        }

        private async Task LoadDatas()
        {
            var dataMods = LoAModCache.Mods.OfType<ILoACustomDataMod>().ToList();
            if (dataMods.Count == 0)
            {
                Logger.Log($"Mod Data Interface Zero. Skip");
            }
            else
            {
                Logger.Log($"Mod Data Interface Initialize Start");

                var tasks = new List<Task>();
                foreach (var mod in dataMods)
                {
                    var path = Path.Combine(mod.path, mod.customDataPath ?? "Data");
                    tasks.Add(FileLoader.LoadData(mod.packageId, path));
                }

                await Task.WhenAll(tasks);
            }


            await FileParser.WaitDataComplete();

            Logger.Log($"Mod Data Interface Initialize Complete");
        }

        private Task<Localize[]> LoadLocalizeFiles()
        {
            var dataMods = LoAModCache.Mods.OfType<ILoALocalizeMod>().ToList();
            if (dataMods.Count == 0)
            {
                Logger.Log($"Mod Localize Interface Zero. Skip");
                return Task.FromResult(new Localize[] { });
            }

            var tasks = new List<Task<Localize>>();

            foreach (var f in Enumerable.Range(0, dataMods.Count).GroupBy(x => x % 10))
            {
                var localize = new Localize(f.Select(index => dataMods[index]));
                tasks.Add(Task.Run(() =>
                {
                    localize.Start();
                    return localize;
                }));
            }

            return Task.WhenAll(tasks);
        }

        private void LoadLocalize()
        {

            var dataMods = LoAModCache.Mods.OfType<ILoALocalizeMod>().ToList();
            if (dataMods.Count == 0)
            {
                Logger.Log($"Mod Localize Interface Zero. Skip");
                return;
            }

            Logger.Log($"Mod Localize Interface Initialize Start");


            var runners = Enumerable.Range(0, dataMods.Count).GroupBy(x => x % 10).Select(x =>
            {
                return new Localize(x.Select(index => dataMods[index]));
            }).ToList();

            Parallel.ForEach(runners, x => x.Start());

            Logger.Log($"Mod Localize Interface Initialize Combine");

            foreach (var runner in runners) runner.Combine();

            Logger.Log($"Mod Localize Interface Initialize Complete");
        }

    }
}
