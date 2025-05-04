using LibraryOfAngela;
using LibraryOfAngela.Buf;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using LibraryOfAngela.Util;
using LOR_XML;
using Mod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    class LoAModCache
    {
        private List<LoAConfigs> configs = new List<LoAConfigs>();
        private Dictionary<string, LoAConfigs> dics = new Dictionary<string, LoAConfigs>();
        private Dictionary<Assembly, LoAConfigs> assemblyDics = new Dictionary<Assembly, LoAConfigs>();
        private List<Type> externalBufTypes = new List<Type>();

        public void Initialize(List<Assembly> assemblies)
        {
            configs = assemblies.Select(d => LoadTypesAndGetDescFromAssembly(d)).Select(LoAConfigs.Create).Where(x => x != null).ToList();
            foreach (var config in configs)
            {
                var path = Path.GetDirectoryName(config.Assembly.Location);
                while (Path.GetFileNameWithoutExtension(path) != "Assemblies")
                {
                    path = Directory.GetParent(path).FullName;
                }
                var parentPath = Directory.GetParent(path).FullName;
                var matchMod = ModContentManager.Instance.GetAllMods().FirstOrDefault(x => x.dirInfo.FullName == parentPath);

                config.mod.path = path;
                config.mod.packageId = matchMod.invInfo.workshopInfo.uniqueId;

                dics[config.packageId] = config;
                assemblyDics[config.Assembly] = config;

                config.Init(config.ArtworkConfig);
                config.Init(config.AssetBundleConfig);
                config.Init(config.CorePageConfig);
                config.Init(config.BattlePageConfig);
                config.Init(config.MapConfig);
                config.Init(config.StoryConfig);
                config.Init(config.EmotionConfig);
                config.Init(config.SuccessionConfig);
  
            }
            BufPatch.Instance.InitExternalBufTypes(externalBufTypes);
            externalBufTypes.Clear();
        }


        private ILoAMod LoadTypesAndGetDescFromAssembly(Assembly assembly)
        {
            var manager = AssemblyManager.Instance;
            var dic = BattleCardAbilityDescXmlList.Instance._dictionary;
            ILoAMod mod = null;
            string step = "";
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    string name = type.Name;
                    string key = null;
                    bool res = true;
                    try
                    {
                        if (type.IsSubclassOf(typeof(DiceCardSelfAbilityBase)) && name.StartsWith("DiceCardSelfAbility_"))
                        {
                            key = name.Substring("DiceCardSelfAbility_".Length);
                            SafeAdd(manager._diceCardSelfAbilityDict, key, type, assembly, "DiceCardSelfAbilityBase");
                        }
                        else if (type.IsSubclassOf(typeof(DiceCardAbilityBase)) && name.StartsWith("DiceCardAbility_"))
                        {
                            key = name.Substring("DiceCardAbility_".Length);
                            SafeAdd(manager._diceCardAbilityDict, key, type, assembly, "DiceCardAbilityBase");
                        }
                        else if (type.IsSubclassOf(typeof(BehaviourActionBase)) && name.StartsWith("BehaviourAction_"))
                        {
                            // BehaviourAction은 key가 필요 없으므로 바로 추가
                            SafeAdd(manager._behaviourActionDict, name.Substring("BehaviourAction_".Length), type, assembly, "BehaviourActionBase");
                        }
                        else if (type.IsSubclassOf(typeof(PassiveAbilityBase)) && name.StartsWith("PassiveAbility_"))
                        {
                            // PassiveAbility는 key가 필요 없으므로 바로 추가
                            SafeAdd(manager._passiveAbilityDict, name.Substring("PassiveAbility_".Length), type, assembly, "PassiveAbilityBase");
                        }
                        else if (type.IsSubclassOf(typeof(DiceCardPriorityBase)) && name.StartsWith("DiceCardPriority_"))
                        {
                            // DiceCardPriority는 key가 필요 없으므로 바로 추가
                            SafeAdd(manager._diceCardPriorityDict, name.Substring("DiceCardPriority_".Length), type, assembly, "DiceCardPriorityBase");
                        }
                        else if (type.IsSubclassOf(typeof(EnemyUnitAggroSetter)) && name.StartsWith("EnemyUnitAggroSetter_"))
                        {
                            // EnemyUnitAggroSetter는 key가 필요 없으므로 바로 추가
                            SafeAdd(manager._enemyUnitAggroSetterDict, name.Substring("EnemyUnitAggroSetter_".Length), type, assembly, "EnemyUnitAggroSetter");
                        }
                        else if (type.IsSubclassOf(typeof(EnemyTeamStageManager)) && name.StartsWith("EnemyTeamStageManager_"))
                        {
                            // EnemyTeamStageManager는 key가 필요 없으므로 바로 추가
                            SafeAdd(manager._enemyTeamStageManagerDict, name.Substring("EnemyTeamStageManager_".Length), type, assembly, "EnemyTeamStageManager");
                        }
                        else if (type.IsSubclassOf(typeof(EnemyUnitTargetSetter)) && name.StartsWith("EnemyUnitTargetSetter_"))
                        {
                            // EnemyUnitTargetSetter는 key가 필요 없으므로 바로 추가
                            SafeAdd(manager._enemyUnitTargetSetterDict, name.Substring("EnemyUnitTargetSetter_".Length), type, assembly, "EnemyUnitTargetSetter");
                        }
                        else if (typeof(ILoAKeywordBufDic).IsAssignableFrom(type))
                        {
                            step = "ILoAKeywordBufDic";
                            externalBufTypes.Add(type);
                        }
                        else if (type.IsSubclassOf(typeof(ModInitializer)))
                        {
                            var initializer = Activator.CreateInstance(type) as ModInitializer;
                            //manager._initializer.Add(initializer);
                            if (initializer is ILoAMod m)
                            {
                                mod = m;
                            }
                        }
                        if (key != null)
                        {
                            FieldInfo field = type.GetField("Desc");
                            //step = "Desc";
                            if (field != null)
                            {
                                string text3 = field.GetValue(null) as string;
                                if (!string.IsNullOrEmpty(text3) && !dic.ContainsKey(key))
                                {
                                    dic.Add(key, new BattleCardAbilityDesc
                                    {
                                        id = key,
                                        desc = new List<string> { text3 }
                                    });
                                }
                            }
                        }
                        if (!res)
                        {
                            Logger.Log($"Maybe Duplicate in {assembly.GetName()} : {step} ");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"TypeDesc Parse Error in {assembly.GetName()}, Target : {step}");
                        Logger.LogError(e);
                    }
                }
            }
            catch (ReflectionTypeLoadException e)
            {
                Logger.Log($"Type Load Error in {assembly.FullName}");
                foreach (var ex in e.LoaderExceptions)
                {
                    Logger.LogError(ex);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"TypeDesc Parse Error Unknown, Target : {step}");
                Logger.LogError(e);
            }

            return mod;
        }

        private void SafeAdd<T>(AssemblyManager.TypeDictionary<T> target, string key, Type type, Assembly assembly, string step) where T : class
        {
            if (target._dict.ContainsKey(key))
            {
                var logger = new StringBuilder($"Type Duplicate Detect Step : {step}, Key : {key}\n");
                logger.AppendLine($"- Previous Type : {target._dict[key]?.FullName} in {target._dict[key]?.Assembly?.FullName}");
                logger.AppendLine($"- Anyway, Override to {type.FullName} in {assembly.FullName}");
                Logger.Log(logger.ToString());
            }
            target._dict[key] = type;

        }


        public LoAConfigs this[string packageId]
        {
            get
            {
                return dics.SafeGet(packageId);
            }
        }

        public static LoAModCache Instance { get; } = new LoAModCache();

        public static IEnumerable<EmotionConfig> EmotionConfigs => Instance.Select(x => x.EmotionConfig).Where(x => x != null);

        public static IEnumerable<BattlePageConfig> BattlePageConfigs => Instance.Select(x => x.BattlePageConfig).Where(x => x != null);

        public static IEnumerable<StoryConfig> StoryConfigs => Instance.Select(x => x.StoryConfig).Where(x => x != null);

        public static IEnumerable<CorePageConfig> CorePageConfigs => Instance.Select(x => x.CorePageConfig).Where(x => x != null);

        public static IEnumerable<ILoAMod> Mods => Instance.Select(x => x.mod);

        public IEnumerable<LoAConfigs> Where(Func<LoAConfigs, bool> func)
        {
            return configs.Where(func);
        }

        public IEnumerable<T> Select<T> (Func<LoAConfigs, T> func)
        {
            return configs.Select(func).Where(x => x != null);
        }

        public static LoAConfigs FromAssembly(object target)
        {
            var assembly = target is Assembly t ? t : target.GetType().Assembly;
            if (Instance.assemblyDics.ContainsKey(assembly))
            {
                return Instance.assemblyDics[assembly];
            }
            return null;
        }

    }
}
