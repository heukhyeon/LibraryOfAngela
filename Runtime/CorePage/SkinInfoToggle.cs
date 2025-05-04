using GameSave;
using LibraryOfAngela.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LibraryOfAngela.CorePage
{
    internal struct SkinProperty
    {
        public const string KEY_ORIGIN_SKIN = "Use Origin SKin";
        public const string KEY_ORIGIN_SKIN_EXP = "Use Origin Skin (Experimental)";
        public const string KEY_ALWAYS_BERSERK = "Always Berserk";
        public const string KEY_ALWAYS_BURN = "Always Burn";
        public const string KEY_ARGALIA_CUSTOM_ACTION = "Custom Action Script";
        public const string KEY_PURPLE_VFX = "Apply VFX";
        public const string KEY_REVERSE_ZENA = "Apply Zena (Experimental)";
        public const string KEY_REVERSE_CLAW = "Apply Claw (Experimental)";

        public string name;
        public bool enabled;

        public SkinProperty Copy(bool enabled)
        {
            return new SkinProperty
            {
                name = name,
                enabled = enabled
            };
        }
    }

    class SkinInfoProvider : Singleton<SkinInfoProvider>
    {
        internal Dictionary<LorId, SkinProperty[]> properties;
        private Dictionary<string, string> modSkinMap;
        private Dictionary<BookModel, BookModel> matchBooks = new Dictionary<BookModel, BookModel>();
        private Regex codRegex = new Regex("[pP][0-9]$");
        public void Initialize()
        {
            if (properties == null) properties = new Dictionary<LorId, SkinProperty[]>();
            foreach (var x in BookInventoryModel.Instance.GetBookListAll())
            {
                var info = x.ClassInfo;
                if (info.Chapter < 6 || (info.Rarity != Rarity.Unique && info.Rarity != Rarity.Rare)) continue;

                if (info.id.IsBasic())
                {
                    // 묘
                    if (info.id == 250024) InsertTarget(info.id, SkinProperty.KEY_ORIGIN_SKIN, SkinProperty.KEY_ALWAYS_BERSERK);
                    // 아르갈리아
                    else if (info.id == 260005) InsertTarget(info.id, SkinProperty.KEY_ORIGIN_SKIN, SkinProperty.KEY_ARGALIA_CUSTOM_ACTION);
                    // 필립
                    else if (info.id == 260006) InsertTarget(info.id, SkinProperty.KEY_ORIGIN_SKIN, SkinProperty.KEY_ALWAYS_BURN);
                    else if (info.id == 250035) InsertTarget(info.id, SkinProperty.KEY_ORIGIN_SKIN, SkinProperty.KEY_PURPLE_VFX);
                    // 얀은 손님 얼굴이 없다
                    else if (info.id != 250051) InsertTarget(info.id, SkinProperty.KEY_ORIGIN_SKIN);
                }
                else if (info.workshopID == "HatAdjustment") InsertTarget(info.id, SkinProperty.KEY_ORIGIN_SKIN);
                else if (info.workshopID == "ReverseCardOpen")
                {
                    if (info._id == 69690806)
                    {
                        matchBooks[x] = CreateDummyBook(180001);
                        InsertTarget(info.id, SkinProperty.KEY_REVERSE_ZENA);
                        if (isPropertyEnabled(x.owner, SkinProperty.KEY_REVERSE_ZENA)) x.owner._CustomBookItem = matchBooks[x];
                    }
                    else if (info._id >= 69690808 && info._id <= 69690810)
                    {
                        matchBooks[x] = CreateDummyBook(180002);
                        InsertTarget(info.id, SkinProperty.KEY_REVERSE_CLAW);
                        if (isPropertyEnabled(x.owner, SkinProperty.KEY_REVERSE_CLAW)) x.owner._CustomBookItem = matchBooks[x];
                    }
                }
                // 모드 책장
                else
                {
                    if (modSkinMap == null) modSkinMap = new Dictionary<string, string>();
                    try
                    {
                        var skinName = x._characterSkin;
                        if (string.IsNullOrEmpty(skinName)) continue;
                        // 원래 머리 안나오는 스킨이면 무시
                        if (!CustomizingBookSkinLoader.Instance.GetWorkshopBookSkinData(x.GetBookClassInfoId().packageId, skinName).dic[ActionDetail.Default].headEnabled)
                        {
                            continue;
                        }
                        var target = FindEnemySkin(x.ClassInfo.id.packageId, skinName);
                        if (string.IsNullOrEmpty(target)) continue;
                        modSkinMap[skinName] = target;
                        InsertTarget(info.id, SkinProperty.KEY_ORIGIN_SKIN_EXP);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }

        }

        private BookModel CreateDummyBook(int id)
        {
            return new BookModel(BookXmlList.Instance.GetData(id));
        }

        private string FindEnemySkin(string packageId, string origin)
        {
            origin = origin.ToLower();
            var matchKey = "";
            var ret = "";
            var targets = CustomizingBookSkinLoader.Instance._bookSkinData[packageId].Select(x => new
            {
                key = x.dataName.ToLower(),
                name = x.dataName
            });
            // 플레이어블은 p로 끝나는 경우가 많다.
            if (origin.EndsWith("p"))
            {
                matchKey = origin.Remove(origin.Length - 1);
                ret = targets.FirstOrDefault(x => x.key == matchKey)?.name;
                if (!string.IsNullOrEmpty(ret)) return ret;
                matchKey = origin.Remove(origin.Length - 1) + "p";
                ret = targets.FirstOrDefault(x => x.key == matchKey)?.name;
                if (!string.IsNullOrEmpty(ret)) return ret;
            }
            else if (origin.EndsWith("player"))
            {
                matchKey = origin.Substring(0, origin.Length - 6);
                ret = targets.FirstOrDefault(x => x.key == matchKey || x.key == matchKey + "enemy")?.name;
                if (!string.IsNullOrEmpty(ret)) return ret;
            }
            else if (codRegex.IsMatch(origin))
            {
                matchKey = origin.Substring(0, origin.Length - 2) + origin.Last();
                ret = targets.FirstOrDefault(x => x.key == matchKey || x.key == matchKey + "enemy")?.name;
                if (!string.IsNullOrEmpty(ret)) return ret;
            }

            // 플레이어블에 원본 이름이 그대로 붙는 경우
            matchKey = origin.Remove(origin.Length - 1) + "e";
            ret = targets.FirstOrDefault(x => x.key == matchKey)?.name;
            if (!string.IsNullOrEmpty(ret)) return ret;

            return null;
        }

        private void InsertTarget(LorId id, params string[] key)
        {
            if (properties.ContainsKey(id))
            {
                properties[id] = key.Select(x =>
                {
                    var target = properties[id].FirstOrDefault(d => d.name == x);
                    return string.IsNullOrEmpty(target.name) ? new SkinProperty { name = x, enabled = false } : target;
                }).ToArray();
            }
            else properties[id] = key.Select(x => new SkinProperty { name = x, enabled = false }).ToArray();
        }

        public static string ConvertValidSkinName(string origin, UnitDataModel unit)
        {
            if (!string.IsNullOrEmpty(unit.workshopSkin))
            {
                return unit.workshopSkin;
            }

            var id = unit.CustomBookItem?._classInfo?.id;
            if (id == null)
            {
                return origin;
            }
            if (id.IsWorkshop())
            {
                var skin = unit.CustomBookItem._characterSkin;
                if (
                    Instance.isPropertyEnabled(unit, SkinProperty.KEY_ORIGIN_SKIN_EXP) &&
                    Instance.modSkinMap?.SafeGet(skin) != null
                    )
                {
                    return Instance.modSkinMap[skin];
                }
                var config = AdvancedSkinInfoPatch.Instance.configs.SafeGet(id.packageId);
                if (config is null) return origin;
                return config.ChangeCharacterSkinByUnitInfo(origin, unit) ?? origin;
            }
            else if (id == 260006)
            {
                if (Instance.isPropertyEnabled(unit, SkinProperty.KEY_ALWAYS_BURN)) return "Blue_Philip_Burn";
            }
            else if (id == 250024)
            {
                if (Instance.isPropertyEnabled(unit, SkinProperty.KEY_ALWAYS_BERSERK)) return "Myo2";
            }
            else if (id == 250006 && Instance.isPropertyEnabled(unit, SkinProperty.KEY_ORIGIN_SKIN))
            {
                return "Kalo";
            }
            else if (id == 250008 && Instance.isPropertyEnabled(unit, SkinProperty.KEY_ORIGIN_SKIN))
            {
                return "Katriel";
            }
            else if (id == 255001 && Instance.isPropertyEnabled(unit, SkinProperty.KEY_ORIGIN_SKIN))
            {
                return "CorNamed";
            }
            return origin;
        }

        public void SaveSkinProperties(SaveData data)
        {
            if (properties == null) return;

            var rootData = new SaveData();
            foreach (var packageTarget in properties.GroupBy(x => x.Key.packageId))
            {
                var packageKey = packageTarget.Key == "" ? "LoA_Vanilla" : packageTarget.Key;
                var packageData = new SaveData();
                foreach (var pair in packageTarget.Select(p => p))
                {
                    SaveData target = new SaveData();
                    bool flag = false;
                    foreach (var property in pair.Value)
                    {
                        if (property.enabled)
                        {
                            flag = true;
                            target.AddData(property.name, new SaveData(1));
                        }
                    }
                    if (target != null && flag) packageData.AddData(pair.Key.id.ToString(), target);
                }
                rootData.AddData(packageKey, packageData);
            }


            data.AddData("LoA_Skin_Properties", rootData);
        }

        public void LoadSkinProperties(SaveData data)
        {
            var rootData = data.GetData("LoA_Skin_Properties");
            if (rootData == null) return;
            if (properties == null) properties = new Dictionary<LorId, SkinProperty[]>();
            foreach (var targetData in rootData._dic)
            {
                var key = targetData.Key == "LoA_Vanilla" ? "" : targetData.Key;
                foreach (var pair in targetData.Value._dic)
                {
                    properties[new LorId(key, int.Parse(pair.Key))] = pair.Value._dic.Select(x => new SkinProperty
                    {
                        name = x.Key,
                        enabled = x.Value.GetIntSelf() == 1
                    }).ToArray();
                }
            }


        }
    
        public bool isPropertyEnabled(UnitDataModel unit, params string[] key)
        {
            if (unit == null) return false;

            try
            {
                var id = unit?.CustomBookItem?.BookId;
                if (properties == null || !properties.ContainsKey(id)) return false;
                foreach (var property in properties[id])
                {
                    foreach (var targetKey in key)
                    {
                        if (property.name == targetKey)
                        {
                            return property.enabled;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return false;
        }

        internal void OnChangeToggleState(string name, bool check)
        {
            var unit = UI.UIController.Instance.CurrentUnit;
            if (name == SkinProperty.KEY_REVERSE_ZENA)
            {
                Instance.properties[matchBooks[unit.bookItem].BookId] = Instance.properties[unit.bookItem.BookId];
                unit._CustomBookItem = check ? matchBooks[unit.bookItem] : null;
            }
            else if (name == SkinProperty.KEY_REVERSE_CLAW)
            {
                Instance.properties[matchBooks[unit.bookItem].BookId] = Instance.properties[unit.bookItem.BookId];
                unit._CustomBookItem = check ? matchBooks[unit.bookItem] : null;
            }
        }
    }

    class SkinInfoToggle : MonoBehaviour
    {
        private List<SkinInfoSwitch> toggleObjects = new List<SkinInfoSwitch>();

        public void UpdateTarget()
        {
            var bookId = UI.UIController.Instance.CurrentUnit.CustomBookItem.GetBookClassInfoId();
            if (toggleObjects.Count == 0)
            {
                var textComponent = GetComponentInChildren<TextMeshProUGUI>();
                var obj = LoAFramework.UiBundle.LoadAsset<GameObject>("Toggle");
                var parent = transform.Find("[Rect]NameTap");
                for (int i = 0; i < 2; i++)
                {
                    var toggle = Instantiate(obj, parent);
                    var component = toggle.AddComponent<SkinInfoSwitch>();
                    component.Init(i, textComponent);
                    toggleObjects.Add(component);
                }
            }
            var properties = SkinInfoProvider.Instance.properties.SafeGet(bookId);
            for (int i = 0; i < toggleObjects.Count; i++)
            {
                var visible = properties != null && i <= properties.Length - 1;
                toggleObjects[i].gameObject.SetActive(visible);
                if (visible)
                {
                    toggleObjects[i].matchId = bookId;
                    toggleObjects[i].Name = properties[i].name;
                    toggleObjects[i].Checked = properties[i].enabled;
                }
            }
        }
    }

    class SkinInfoSwitch : MonoBehaviour
    {
        public LorId matchId;
        public string _name = "";
        private TextMeshProUGUI label;
        private Image checkMark;
        private bool _checked = false;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                label.text = value;
            }
        }

        public bool Checked
        {
            get => _checked;
            set
            {
                _checked = value;
                checkMark.enabled = value;
            }
        }

        public void Init(int i, TextMeshProUGUI textComponent)
        {
            gameObject.name = "LoACustomToggle_" + (i + 1);
            gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            gameObject.transform.localPosition = new Vector3(25f, 50f * (i + 1), 0f);
            checkMark = GetComponentsInChildren<Image>().First(x => x.gameObject.name == "Checkmark");
            label = GetComponentInChildren<TextMeshProUGUI>();
            label.transform.localPosition = new Vector3(20f, -2f, 0f);
            label.font = textComponent.font;
            label.fontMaterial = textComponent.fontMaterial;
            Destroy(GetComponent<Toggle>());

            var triggercComponent = GetComponent<EventTrigger>();
            triggercComponent.triggers.Clear();

            var trigger = new EventTrigger.Entry();
            trigger.eventID = EventTriggerType.PointerClick;
            trigger.callback.AddListener((e) =>
            {
                Checked = !Checked;
                SkinInfoProvider.Instance.properties[matchId] = SkinInfoProvider.Instance.properties[matchId].Select(x =>
                {
                    if (x.name != Name) return x;
                    else return new SkinProperty
                    {
                        name = Name,
                        enabled = Checked
                    };
                }).ToArray();
                SkinInfoProvider.Instance.OnChangeToggleState(Name, Checked);
                UICustomizePopup.Instance.UpdatePreview();
            });
            triggercComponent.triggers.Add(trigger);
        }
    }
}
