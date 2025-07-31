using HarmonyLib;
using LibraryOfAngela.Battle;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Buf
{
    class BufPatch : Singleton<BufPatch>
    {
        struct KeywordWrapper
        {
            public PropertyInfo info;
            public Type targetBufType;
            public KeywordBuf realBuf;
        }

        private Dictionary<Type, KeywordWrapper> bufMatch = new Dictionary<Type, KeywordWrapper>();
        private int diff = 0;
        private bool isKeywordBufHandled = false;

        public void Initialize()
        {
            InternalExtension.SetRange(typeof(BufPatch));
            InitLoABufInject();
            var extenderType = Type.GetType("EnumExtenderV2.EnumExtenderInternals, 10EnumExtender");
            var runtimeKeywordBufType = typeof(KeywordBuf).GetType();
            var method = AccessTools.Method(typeof(Enum), "GetCachedValuesAndNames");
            if (extenderType != null)
            {
                Logger.Log("Find EnumExtenderV2, Preload GetCachedValuesAndNames");
                method.Invoke(null, new object[] { typeof(KeywordBuf), true });
                //AccessTools.Method(extenderType, "SafetyUncache").Invoke(null, new Type[] { runtimeKeywordBufType });
            }

            var field = AccessTools.Field(runtimeKeywordBufType, "GenericCache");

            var currentCache = field.GetValue(runtimeKeywordBufType);
            if (currentCache == null)
            {
                typeof(Enum).PatchInternal("GetCachedValuesAndNames", flag: PatchInternalFlag.PREFIX | PatchInternalFlag.POSTFIX);
                method.Invoke(null, new object[] { typeof(KeywordBuf), true });
            }
            else
            {
                field.SetValue(runtimeKeywordBufType, HandleKeywordBufExtension(currentCache, runtimeKeywordBufType));
            }


            if (!Instance.isKeywordBufHandled)
            {
                Logger.Log("KeywordBuf Not Generated...Maybe Other Mod Inject ? Try Manually");
                currentCache = field.GetValue(runtimeKeywordBufType);
                if (currentCache is null)
                {
                    Logger.Log("KeywordBuf Not Generated But Current Cache is Same Null... What?");
                }
                else
                {
                    field.SetValue(runtimeKeywordBufType, HandleKeywordBufExtension(currentCache, runtimeKeywordBufType));
                    Logger.Log($"KeywordBuf Not Generated And Inject Manually : " + Instance.isKeywordBufHandled);
                }
            }

            typeof(BattleUnitBufListDetail).PatchInternal("AddNewKeywordBufInList", PatchInternalFlag.TRANSPILER);
        }

        public KeywordBuf GetKeywordBuf(Type type)
        {
            if (bufMatch.ContainsKey(type))
            {
                return bufMatch[type].realBuf;
            }
            Logger.Log($"Find Request Keyword {type.Name} But Null... What...?");
            return KeywordBuf.None;
        }

        public void InitExternalBufTypes(List<Type> types)
        {
            var wrappers = types.SelectMany(d => d.GetProperties(AccessTools.all))
                .Select(d =>
                {
                    var attribute = d.GetCustomAttribute<LoAKeywordBufProperty>();
                    if (attribute is null) return default(KeywordWrapper);
                    return new KeywordWrapper
                    {
                        info = d,
                        targetBufType = attribute.type
                    };
                });

            foreach (var b in wrappers)
            {
                if (b.targetBufType is null) continue;
                bufMatch[b.targetBufType] = b;
            }
        }

        private static void Before_GetCachedValuesAndNames(Type enumType, out bool __state)
        {
            if (Instance.isKeywordBufHandled)
            {
                __state = false;
                return;
            }
            if (enumType == typeof(KeywordBuf))
            {
                Logger.Log("Detect KeywordBuf Generate");
                var runtimeType = enumType.GetType();
                var field = AccessTools.Field(runtimeType, "GenericCache");
                __state = field.GetValue(runtimeType) == null;
                Logger.Log($"Detect KeywordBuf Inject Required : {__state}");
            }
            else
            {
                __state = false;
            }
        }

        private static void After_GetCachedValuesAndNames(object __result, bool __state)
        {
            if (!__state) return;
            HandleKeywordBufExtension(__result, typeof(KeywordBuf));
            // EnumExtenderV2 Conflict
        }

        private static IEnumerable<CodeInstruction> Trans_AddNewKeywordBufInList(IEnumerable<CodeInstruction> __instruction, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(__instruction);
            var lastBrFalse = codes.Last(d => d.opcode == OpCodes.Brfalse_S || d.opcode == OpCodes.Brfalse);
            int i = 0;
            while (true)
            {
                if (i >= codes.Count) break;
                var code = codes[i];
                if (code.opcode == OpCodes.Switch)
                {
                    var bufValues = Instance.bufMatch.ToList();
                    var targets = code.operand as Label[];
                    CodeInstruction br = null;
                    Type[] emptyArr = new Type[] { };
                    var childCodes = new List<CodeInstruction>();
                    var labels = new List<Label>();

                    if (br is null)
                    {
                        for (int j = i; j < codes.Count; j++)
                        {
                            if (codes[j].opcode == OpCodes.Br)
                            {
                                br = codes[j];
                                break;
                            }
                        }
                    }
                    for (int z = 0; z < Instance.diff; z++)
                    {
                        var label = generator.DefineLabel();
                        labels.Add(label);
                        childCodes.Add(new CodeInstruction(OpCodes.Br, br.operand));
                        childCodes.Add(new CodeInstruction(OpCodes.Ldnull).WithLabels(label));
                        childCodes.Add(new CodeInstruction(OpCodes.Stloc_2));
                    }

                    foreach (var c in bufValues)
                    {
                        var label = generator.DefineLabel();
                        labels.Add(label);
                        childCodes.Add(new CodeInstruction(OpCodes.Br, br.operand));
                        childCodes.Add(new CodeInstruction(OpCodes.Newobj, c.Key.GetConstructor(emptyArr)).WithLabels(label));
                        childCodes.Add(new CodeInstruction(OpCodes.Stloc_2));
                    }
                    yield return new CodeInstruction(OpCodes.Switch, targets.Concat(labels).ToArray());
                    i++;
                    while (true)
                    {
                        code = codes[i];
                        if (code.opcode != OpCodes.Ldloc_2)
                        {
                            yield return code;
                            i++;
                            continue;
                        }
                        int v = 0;
                        foreach (var c in childCodes)
                        {
                            v++;
                            yield return c;
                        }
                        yield return code;
                        i++;
                        break;
                    }
                }
                else if (code == lastBrFalse)
                {
                   
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BufPatch), nameof(BufPatch.HandleAddNewKeywordBufInList)));
                    yield return code;
                    i++;
                }
                else
                {
                    yield return code;
                    i++;
                }
            }
        }

        private static BattleUnitBuf HandleAddNewKeywordBufInList(BattleUnitBuf current, BattleUnitBufListDetail owner, BufReadyType readyType, KeywordBuf bufType, ref BattleUnitBuf replace)
        {
            try
            {
                foreach (var b in BattleInterfaceCache.Of<IHandleAddNewKeywordBufInList>(owner._self))
                {
                    var next = b.OnAddNewKeywordBufInList(bufType, current, readyType);
                    if (current != next)
                    {
                        current = next;
                    }
                }
                if (current != replace)
                {
                    replace = current;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return current;
        }

        private static object HandleKeywordBufExtension(object origin, Type type)
        {
            if (Instance.isKeywordBufHandled) return origin;
            if (type != typeof(KeywordBuf)) return origin;
            Instance.isKeywordBufHandled = true;
            var valueField = AccessTools.Field(origin.GetType(), "Values");
            var nameField = AccessTools.Field(origin.GetType(), "Names");
            var values = new List<ulong>(valueField.GetValue(origin) as ulong[]);
            var names = new List<string>(nameField.GetValue(origin) as string[]);
            var lastValue = values.Max();
            var lastName = names[values.IndexOf(lastValue)];
            var logger = new StringBuilder($"KeywordBuf Patch, Latest KeywordBuf is {lastName} ({lastValue})\n");
            var expectedLastValue = (ulong)(KeywordBuf.CB_BigBadWolf_Scar);
            if (lastValue != expectedLastValue)
            {
                Instance.diff += (int)(lastValue - expectedLastValue);
                Logger.Log($"Another KeywordBuf Inject Detect. Maybe EnumExtenderV2 ...? Move Right -> {Instance.diff}");
            }
            foreach (var t in Instance.bufMatch.Keys.ToList())
            {
                var value = ++lastValue;
                var name = "LoA_" + t.Name;
                values.Add(value);
                names.Add(name);
                var b = (KeywordBuf)value;
                var wrapper = Instance.bufMatch[t];
                wrapper.realBuf = b;
                Instance.bufMatch[t] = wrapper;
                wrapper.info.SetValue(null, b);
                logger.AppendLine($"- Keyword Add : {name} = {value}");
            }
            valueField.SetValue(origin, values.ToArray());
            nameField.SetValue(origin, names.ToArray());
            Logger.Log(logger.ToString());

            return origin;
        }

        [HarmonyPatch(typeof(BattleUnitBuf), "GetBufIcon")]
        [HarmonyPostfix]
        public static void After_GetBufIcon(BattleUnitBuf __instance, ref Sprite __result, ref Sprite ____bufIcon)
        {
            try
            {
                if (!(__result is null)) return;
                if (__instance.Hide) return;
                var key = __instance.keywordIconId;
                if (string.IsNullOrEmpty(key)) return;
                var art = LoABufIcon(key);
                if (art is null) art = ModBufIcon(key, __instance);
                if (!(art is null))
                {
                    __result = art;
                    BattleUnitBuf._bufIconDictionary[key] = __result;
                    ____bufIcon = __result;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }


        [HarmonyPatch(typeof(BattleDiceCardBuf), "GetBufIcon")]
        [HarmonyPostfix]
        public static void After_BattleDiceCardBuf_GetBufIcon(BattleDiceCardBuf __instance, ref Sprite __result, ref Sprite ____bufIcon)
        {
            if (!(__result is null)) return;
            var key = __instance.keywordIconId;
            if (string.IsNullOrEmpty(key)) return;
            var art = ModBufIcon(key, __instance);
            if (!(art is null))
            {
                ____bufIcon = art;
                __result = art;
            }
        }

        internal static Sprite LoABufIcon(string key)
        {
            switch (key)
            {
                case "loa_tremor_icon":
                case "loa_sinking_icon":
                case "loa_rupture_icon":
                case "loa_poise_icon":
                case "loa_dimension_rift_icon":
                    return BufAssetLoader.LoadImage(key);
                default:
                    return null;
            }
        }

        internal static Sprite ModBufIcon(string keyword, object target)
        {
            var artwork = LoAModCache.FromAssembly(target)?.Artworks;
            if (artwork == null) return null;
            return artwork[keyword];
        }

        internal static void InitLoABufInject()
        {
            Instance.bufMatch[typeof(BattleUnitBuf_loaTremor)] = new KeywordWrapper
            {
                info = AccessTools.Property(typeof(LoAKeywordBuf), "Tremor"),
                targetBufType = typeof(BattleUnitBuf_loaTremor)
            };

            Instance.bufMatch[typeof(BattleUnitBuf_loaSinking)] = new KeywordWrapper
            {
                info = AccessTools.Property(typeof(LoAKeywordBuf), "Sinking"),
                targetBufType = typeof(BattleUnitBuf_loaSinking)
            };

            Instance.bufMatch[typeof(BattleUnitBuf_loaPoise)] = new KeywordWrapper
            {
                info = AccessTools.Property(typeof(LoAKeywordBuf), "Poise"),
                targetBufType = typeof(BattleUnitBuf_loaPoise)
            };

            Instance.bufMatch[typeof(BattleUnitBuf_loaRupture)] = new KeywordWrapper
            {
                info = AccessTools.Property(typeof(LoAKeywordBuf), "Rupture"),
                targetBufType = typeof(BattleUnitBuf_loaRupture)
            };

            Instance.bufMatch[typeof(BattleUnitBuf_loaDimensionRift)] = new KeywordWrapper
            {
                info = AccessTools.Property(typeof(LoAKeywordBuf), "DimensionRift"),
                targetBufType = typeof(BattleUnitBuf_loaDimensionRift)
            };

            Instance.bufMatch[typeof(BattleUnitBuf_loaShield)] = new KeywordWrapper
            {
                info = AccessTools.Property(typeof(LoAKeywordBuf), "Barrier"),
                targetBufType = typeof(BattleUnitBuf_loaShield)
            };
        }

        internal static void InitLoABufEffectInfo()
        {
            foreach (var b in new List<BufControllerImpl> { 
                new TremorControllerImpl(),
                new SinkingControllerImpl(),
                new RuptureControllerImpl(),
                new DimensionRiftControllerImpl(),
            })
            {
                BattleEffectTextsXmlList.Instance._dictionary[b.keywordId] = new LOR_XML.BattleEffectText
                {
                    ID = b.keywordId,
                    Name = b.GetBufName(),
                    Desc = b.GetBufActivatedText()
                };

                BattleEffectTextsXmlList.Instance._dictionary[b.keywordId + "_Keyword"] = new LOR_XML.BattleEffectText
                {
                    ID = b.keywordId + "_Keyword",
                    Name = b.GetBufName() + " X",
                    Desc = string.Format(b.GetBufActivatedText(), "X")
                };

                b.AddAdditionalKeywordDesc();
            }
        }
    }
}
