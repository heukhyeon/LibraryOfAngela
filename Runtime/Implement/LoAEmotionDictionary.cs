using HarmonyLib;
using LibraryOfAngela.Battle;
using LibraryOfAngela.Emotion;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;
using LibraryOfAngela.Util;
using LOR_XML;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UI;
using UnityEngine;
using UnityEngine.UI;
using static LibraryOfAngela.Battle.EgoPatch;
using static LibraryOfAngela.Extension.Framework.FrameworkExtension;

namespace LibraryOfAngela.Implement
{
    class LoAEmotionDictionary : Singleton<LoAEmotionDictionary>, ILoAEmotionDictionary
    {
        // 모드별 모드 환상체 모음
        private Dictionary<string, List<LoAEmotionInfo>> infos = new Dictionary<string, List<LoAEmotionInfo>>();
        private Dictionary<string, List<AbnormalityCard>> descs = new Dictionary<string, List<AbnormalityCard>>();
        public Dictionary<EmotionCardXmlInfo, string> infoPackageIdDictionary = new Dictionary<EmotionCardXmlInfo, string>();
        public Dictionary<string, LoAEmotionDescInfo> cardIdAbnormalityCardDictionary = new Dictionary<string, LoAEmotionDescInfo>();

        public bool Initialize()
        {
            foreach (var config in LoAModCache.EmotionConfigs)
            {
                var key = config.packageId;
                var configDescDir = Path.GetDirectoryName(config.emotionPath);
                var configDescFile = Path.GetFileNameWithoutExtension(config.emotionPath);
                configDescFile = $"{configDescFile}.xml";

                var realPath = PathProvider.ConvertValidPath(config.packageId, Path.Combine(configDescDir, configDescFile));
                Logger.Log($"Emotion Xml Load in ({key}) :: {realPath}");
                infos[key] = LoAXmlLoader.getContents<EmotionCardXmlRoot, LoAEmotionInfo>(realPath, (x) => x.emotionCardXmlList.Select(c => new LoAEmotionInfo(config.packageId, c)).ToList()); ;
          

                configDescDir = Path.GetDirectoryName(config.descPath);
                configDescFile = Path.GetFileNameWithoutExtension(config.descPath);
                var prefix = TextDataModel.CurrentLanguage + "_";
                configDescFile = $"{prefix}{configDescFile}.xml";
                descs[key] = LoAXmlLoader.getContents<AbnormalityCardsRoot, AbnormalityCard>(
                    PathProvider.ConvertValidPath(config.packageId, Path.Combine(configDescDir, configDescFile)),
                    (x) => x.sephirahList.SelectMany(e => e.list).ToList()
                );
                infos[key].ForEach(x => infoPackageIdDictionary[x] = key);
                descs[key].ForEach(x =>
                {
                    x.abilityDesc = string.Join("\n", x.abilityDesc.Split('\n').Select(d => d.Trim())).Trim();
                    cardIdAbnormalityCardDictionary[x.id] = new LoAEmotionDescInfo
                    {
                        card = x,
                        cardId = x.id,
                        packageId = key
                    };
                });
                Logger.Log($"Load - {key} : Emotion Card Count : {infos[key].Count} / {descs[key].Count}");
            }


            return infos.Count > 0;
        }

        public List<AbnormalityCard> GetEmotionCardDescListByMod(ILoACustomEmotionMod mod) => descs.SafeGet(mod.packageId);

        public List<EmotionCardXmlInfo> GetEmotionCardListByMod(ILoACustomEmotionMod mod, Predicate<EmotionCardXmlInfo> condition)
        {
            try
            {
                if (mod?.packageId is null)
                {
                    Logger.Log($"Mod Is Null...What? {mod?.GetType()?.Name} // {mod is null}");
                    return new List<EmotionCardXmlInfo>();
                }

                if (infos is null)
                {
                    Logger.Log($"EmotionInfo Is Null...What?");
                    return new List<EmotionCardXmlInfo>();
                }

                var emotionInfo = infos.SafeGet(mod.packageId);
                var result = new List<EmotionCardXmlInfo>();
                if (emotionInfo is null) return result;

                if (condition is null)
                {
                    result.AddRange(emotionInfo);
                }
                else
                {
                    foreach (var emotion in emotionInfo)
                    {
                        if (condition(emotion))
                        {
                            result.Add(emotion);
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Logger.Log("Unknown Error during Find GetEmotionCardListByMod");
                Logger.LogError(e);
                return new List<EmotionCardXmlInfo>();
            }
        }

        public bool IsModCard(ILoACustomEmotionMod mod, EmotionCardXmlInfo card)
        {
            if (card is null) return false;
            return infoPackageIdDictionary.SafeGet(card) == mod.packageId;
        }

        public void ShowEmotionSelectUI(EmotionPannelInfo info)
        {
            EmotionPatch.Instance.currentPanelInfo = info;
            SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(true);
            SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.Init(info.cards.Count, info.cards.ToList());
        }

        public EmotionCardXmlInfo FindEmotionCard(string packageId, int id)
        {
            var emotionInfo = infos.SafeGet(packageId);
            if (emotionInfo is null) return null;
            return emotionInfo.Find(d => d.id == id);
        }



    }
}
