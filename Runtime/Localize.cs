using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Util;
using LOR_XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace LibraryOfAngela
{
    class LocalizeData
    {
        public string packageId;
        public string type;
        public string id;
        public string name;
        public string desc;
        public List<string> descs;
        public object origin;

        public LocalizeData()
        {

        }

        public LocalizeData(BattleCardDesc desc)
        {
            type = "Card";
            name = desc.cardID.ToString();
            this.desc = desc.cardName;
            var abilityExists = !string.IsNullOrEmpty(desc.ability);

            if (abilityExists || desc.behaviourDescList != null)
            {
                origin = desc;
            }
        }

        public LocalizeData(BattleCardAbilityDesc desc)
        {
            type = "Ability";
            name = desc.id;
            this.desc = string.Join("\n", desc.desc);
        }

        public LocalizeData(BattleEffectText desc)
        {
            type = "Effect";
            id = desc.ID;
            name = desc.Name;
            this.desc = desc.Desc;
        }

        public LocalizeData(PassiveDesc desc)
        {
            type = "Passive";
            id = desc._id.ToString();
            name = desc.name;
            this.desc = desc.desc;
        }
        public LocalizeData(CharacterName desc)
        {
            type = "Stage";
            name = desc.ID.ToString();
            this.desc = desc.name;
        }

        public LocalizeData(BookDesc desc)
        {
            type = "BookStory";
            id = desc.bookID.ToString();
            name = desc.bookName;
            this.desc = "";
            descs = desc.texts ?? new List<string>();
        }

        public LocalizeData(string type, string name, string desc)
        {
            this.type = type;
            this.name = name;
            this.desc = desc;
        }
    }

    class Localize
    {
        private readonly IEnumerable<ILoALocalizeMod> mods;
        private List<LocalizeData> datas = new List<LocalizeData>();
        private List<BattleDialogCharacter> dialogs = new List<BattleDialogCharacter>();
        public bool isInjectAll { get; private set; } = false;

        public Localize(IEnumerable<ILoALocalizeMod> mods)
        {
            this.mods = mods;
        }

        public void Start(string language = null)
        {
            language = language ?? TextDataModel.CurrentLanguage;
            foreach (var m in mods)
            {
                inject(language, m.packageId, m.path);
                Logger.Log($"Localize Inject Complete : {m.packageId}");
            }
            isInjectAll = true;
        }

        public void Combine()
        {
            if (datas.Count == 0) return;

            var rootDiceCardDictionary = BattleCardAbilityDescXmlList.Instance.GetFieldValue<Dictionary<string, BattleCardAbilityDesc>>("_dictionary");
            var rootBufDictionary = BattleEffectTextsXmlList.Instance.GetFieldValue<Dictionary<string, BattleEffectText>>("_dictionary");
            var storyDic = BookDescXmlList.Instance._dictionaryWorkshop;
            BattleDialogXmlList.Instance.AddDialogByMod(dialogs);
            foreach (var data in datas)
            {
                try
                {
                    data.desc = string.Join("\n", data.desc.Split('\n').Select(x => x.Trim())).Trim();
                    switch (data.type)
                    {
                        case "ETC":
                            TextDataModel.textDic[data.name] = data.desc;
                            break;
                        case "Stage":
                            var stage = StageClassInfoList.Instance.GetData(new LorId(data.packageId, int.Parse(data.name)));
                            if (stage != null) stage.stageName = data.desc;
                            break;
                        case "BookName":
                            BookXmlList.Instance.GetData(new LorId(data.packageId, int.Parse(data.name))).InnerName = data.desc;
                            break;
                        case "DropBook":
                            DropBookXmlList.Instance.GetData(new LorId(data.packageId, int.Parse(data.name))).workshopName = data.desc;
                            break;
                        case "Effect":
                            rootBufDictionary[data.id] = new BattleEffectText
                            {
                                ID = data.id,
                                Name = data.name,
                                Desc = data.desc
                            };
                            break;
                        case "Passive":
                            var passive = PassiveXmlList.Instance.GetData(new LorId(data.packageId, int.Parse(data.id)));
                            if (passive != null)
                            {
                                passive.name = data.name;
                                passive.desc = data.desc;
                            }
                            break;
                        case "BookStory":
                            if (!storyDic.ContainsKey(data.packageId))
                            {
                                storyDic[data.packageId] = new List<BookDesc>();
                            }
                            var id = int.Parse(data.id);
                            var target = storyDic[data.packageId].Find(x => x.bookID == id);
                            if (data.descs != null)
                            {
                                if (target != null)
                                {
                                    target.bookName = data.name;
                                    target.texts = data.descs;
                                }
                                else
                                {
                                    storyDic[data.packageId].Add(new BookDesc
                                    {
                                        bookID = id,
                                        bookName = data.name,
                                        texts = data.descs,
                                    });
                                }
                                BookXmlList.Instance.GetData(new LorId(data.packageId, id)).InnerName = data.name;
                            }
                            else if (target != null)
                            {
                                target.bookName = data.name;
                                target.texts = data.desc.Split('\n').ToList();
                            }
                            else
                            {
                                storyDic[data.packageId].Add(new BookDesc
                                {
                                    bookID = id,
                                    bookName = data.name,
                                    texts = data.desc.Split('\n').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.TrimEnd('\r', '\n')).ToList(),
                                });
                            }
                            break;
                        case "Card":
                            var targetId = new LorId(data.packageId, int.Parse(data.name));
                            ItemXmlDataList.instance.GetCardItem(targetId).workshopName = data.desc;
                            if (data.origin != null)
                            {
                                BattleCardDescXmlList.Instance._dictionary[targetId] = data.origin as BattleCardDesc;
                            }
                            break;
                        case "Ability":
                            rootDiceCardDictionary[data.name] = new BattleCardAbilityDesc { id = data.name, desc = new List<string> { data.desc } };
                            break;
                        case "EnemyName":
                            EnemyUnitClassInfoList.Instance.GetData(new LorId(data.packageId, int.Parse(data.name))).name = data.desc;
                            break;
                        default:
                            break;
                    }
                }
                catch (NullReferenceException)
                {
                    Logger.Log($"Localze Error ({data.packageId}) :: Please check valid Format :: {data.type} // {data.id} // {data.name}");
                }

            }
        }

        public void inject(string language, string packageId, string path)
        {
            path = Path.Combine(path, "Localize", language);
            if (!Directory.Exists(path)) return;

            var datas = new List<LocalizeData>();

            datas.AddRange(loadRawKeyValues(Path.Combine(path, "Stage.xml"), (reader) =>
            {
                var key = reader.GetAttribute("name");
                reader.Read();
                var value = reader.Value.Trim();
                return new LocalizeData { type = "Stage", name = key, desc = value };
            }));

            datas.AddRange(loadRawKeyValues(Path.Combine(path, "BookName.xml"), (reader) =>
            {
                var key = reader.GetAttribute("name");
                var type = reader.GetAttribute("type");
                if (string.IsNullOrEmpty(type)) type = "BookName";
                reader.Read();
                var value = reader.Value.Trim();
                return new LocalizeData { type = type, name = key, desc = value };
            }));

            datas.AddRange(loadRawKeyValues(Path.Combine(path, "Effect.xml"), (reader) =>
            {
                var key = reader.GetAttribute("name");
                var title = reader.GetAttribute("title");
                reader.Read();
                var value = reader.Value.Trim();
                return new LocalizeData { type = "Effect", id = key, name = title, desc = value };
            }));

            datas.AddRange(loadRawKeyValues(Path.Combine(path, "Passive.xml"), (reader) =>
            {
                var key = reader.GetAttribute("name");
                var title = reader.GetAttribute("title");
                reader.Read();
                var value = reader.Value.Trim();
                return new LocalizeData { type = "Passive", id = key, name = title, desc = value };
            }));

            datas.AddRange(loadRawKeyValues(Path.Combine(path, "CardName.xml"), (reader) =>
            {
                var key = reader.GetAttribute("name");
                reader.Read();
                var value = reader.Value.Trim();
                return new LocalizeData { type = "Card", name = key, desc = value };
            }));

            datas.AddRange(loadRawKeyValues(Path.Combine(path, "CardAbility.xml"), (reader) =>
            {
                var key = reader.GetAttribute("name");
                reader.Read();
                var value = reader.Value.Trim();
                return new LocalizeData { type = "Ability", name = key, desc = value };
            }));

            datas.AddRange(loadRawKeyValues(Path.Combine(path, "BookStory.xml"), (reader) =>
            {
                var id = reader.GetAttribute("name");
                var key = reader.GetAttribute("title");
                reader.Read();
                var value = reader.Value.Trim();
                return new LocalizeData { type = "BookStory", id = id, name = key, desc = value };
            }));

            datas.AddRange(loadRawKeyValues(Path.Combine(path, "EnemyNames.xml"), (reader) =>
            {
                var key = reader.GetAttribute("name");
                reader.Read();
                var value = reader.Value.Trim();
                return new LocalizeData { type = "EnemyName", name = key, desc = value };
            }));

            foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file);
                if (ext == ".xml" || ext == ".txt")
                {
                    try
                    {
                        
                        datas.AddRange(load(file, packageId));
                    }
                    catch(Exception e)
                    {
                        Logger.Log("Parsing Localize Error in " + file);
                        throw e;
                    }
             
                }
            }

            datas.ForEach(x => x.packageId = packageId);

            this.datas.AddRange(datas);

        }

        /// <summary>
        /// <Key title=""></Key> 의 형태로 정의된 xml에 대해 키밸류를 읽는다.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private List<LocalizeData> loadRawKeyValues(string path, Func<XmlReader, LocalizeData> callback)
        {
            if (!File.Exists(path)) return new List<LocalizeData>();
            var result = new List<LocalizeData>();
            try
            {
                using (var reader = XmlReader.Create(path))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement() && reader.Name == "Key")
                        {
                            result.Add(callback(reader));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Localize Error in  {path}, Please Check");
                Logger.LogError(e);
            }




            return result;
        }

        private List<LocalizeData> load(string path, string packageId)
        {
            var result = new List<LocalizeData>();
            var empty = Array.Empty<LocalizeData>();
            bool flag = false;
       
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                using (var reader = XmlReader.Create(path))
                {
                    while (!reader.EOF && reader.Name?.Contains("Root") != true)
                    {
                        reader.Read();
                    }
                    var rootName = reader.Name;
                    flag = rootName == "Root";
                    if (!flag)
                    {
                        result.AddRange(ConvertXml<BattleCardDescRoot>(reader)?.cardDescList?.Select(x => new LocalizeData(x)) ?? empty);
                        result.AddRange(ConvertXml<PassiveDescRoot>(reader)?.descList?.Select(x => new LocalizeData(x)) ?? empty);
                        result.AddRange(ConvertXml<BattleCardAbilityDescRoot>(reader)?.cardDescList?.Select(x => new LocalizeData(x)) ?? empty);
                        result.AddRange(ConvertXml<BattleEffectTextRoot>(reader)?.effectTextList?.Select(x => new LocalizeData(x)) ?? empty);
                        result.AddRange(ConvertXml<CharactersNameRoot>(reader)?.nameList?.Select(x => new LocalizeData(x) { type = fileName.Contains("Stage") ? "Stage" : fileName.Contains("DropBook") ? "DropBook" : "EnemyName" }) ?? empty);
                        result.AddRange(ConvertXml<BattleCardDescRoot>(reader)?.cardDescList?.Select(x => new LocalizeData(x)) ?? empty);
                        result.AddRange(ConvertXml<BookDescRoot>(reader)?.bookDescList?.Select(x => new LocalizeData(x)) ?? empty);
                        ConvertXml<BattleDialogRoot>(reader)?.characterList?.ForEach(x =>
                        {
                            x.workshopId = packageId;
                            x.bookId = int.Parse(x.characterID);
                            dialogs.Add(x);
                        });
                    }
                }


                if (result.Count == 0)
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(File.ReadAllText(path));
                    var type = "ETC";
                    foreach (var t in new string[] { "DropBook"})
                    {
                        if (fileName.Contains(t))
                        {
                            type = t;
                            break;
                        }
                    }
                    foreach (object obj in xmlDocument.SelectNodes("localize/text"))
                    {
                        XmlNode xmlNode = (XmlNode)obj;
                        string key = string.Empty;
                        if (xmlNode.Attributes.GetNamedItem("id") != null)
                        {
                            key = xmlNode.Attributes.GetNamedItem("id").InnerText;
                        }
                        result.Add(new LocalizeData { type = type, name = key, desc = xmlNode.InnerText });
                    }
                }
            }
            catch (FormatException e)
            {
                Logger.Log($"Localize FormatException in {path}");
                Logger.LogError(e);
                flag = false;
            }


            if (flag)
            {
                var name = Path.GetFileName(path);
                if (name.StartsWith("EtcText"))
                {
                    result.AddRange(loadRawKeyValues(path, (reader) =>
                    {
                        var key = reader.GetAttribute("name");
                        reader.Read();
                        var value = reader.Value.Trim();
                        return new LocalizeData { type = "ETC", name = key, desc = value };
                    }));
                }
                else if (name.StartsWith("BookStory"))
                {
                    result.AddRange(loadRawKeyValues(path, (reader) =>
                    {
                        var id = reader.GetAttribute("name");
                        var key = reader.GetAttribute("title");
                        reader.Read();
                        var value = reader.Value.Trim();
                        return new LocalizeData { type = "BookStory", id = id, name = key, desc = value };
                    }));
                }
            }
            return result;
        }

        private T ConvertXml<T>(XmlReader reader) where T : class
        {
            try
            {
                var content = (T)new XmlSerializer(typeof(T)).Deserialize(reader);
                return content;
            }
            catch (InvalidOperationException e)
            {
                return null;
            }
            catch (Exception e)
            {
                Logger.Log("ConvertXml Fail in Type :" + typeof(T).FullName);
                throw e;
            }
        }
    }
}
