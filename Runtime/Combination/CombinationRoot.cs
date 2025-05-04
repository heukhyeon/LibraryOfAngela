using LibraryOfAngela.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.Combination
{
    class CombinationRoot : MonoBehaviour
    {
        bool isSave = false;
        public TextMeshProUGUI targetText;
        private static UIEquipPageScrollList saveComponent;
        private string text;

        void Awake()
        {
            targetText = GetComponentInChildren<TextMeshProUGUI>();
            transform.localPosition = new Vector3(isSave ? -135f : 150f, 0f, 0f);
            transform.localScale = Vector3.one;
            text = isSave ? "Combination Save" : "Combination Load";
            if (isSave)
            {
                var target = FindObjectOfType<UIEquipPageScrollList>();
                var list = Instantiate(target.gameObject, transform.parent);
                list.transform.localPosition = new Vector3(-750f, 250f);
                list.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
                saveComponent = list.GetComponent<UIEquipPageScrollList>();
                typeof(UIEquipPageScrollList).Patch("FilterBookModels");
                saveComponent.SetScrollBar();
                saveComponent.Initialized();
                saveComponent.OpenInit();
                var keys = Enumerable.Range(0, 3).Select(x => new UIStoryKeyData(8, "LoA-Save" + (x + 1))).ToList();
                saveComponent.SetData(Enumerable.Range(0, 15).Select(x => {
                    var xml = BookXmlList.Instance.GetData(1);
                    xml.workshopID = keys[x / 5].workshopId;
                    xml.InnerName = "Test Book " + ((x % 5) + 1);
                    return new BookModel(xml);
                }).ToList(), null);
            }

        }

        void Update()
        {
            if (targetText.text != text) targetText.text = text;
        }

        public static void Initialize()
        {
            typeof(UILibrarianEquipDeckPanel).Patch("Initialized");
        }

        public static void After_Initialized(UILibrarianEquipDeckPanel __instance)
        {
            Patcher.Unpatch(typeof(UILibrarianEquipDeckPanel), "Initialized", PatchType.POSTFIX);
            var targetButton = __instance.button_SaveDeckButton;
            var floor = UI.UIController.Instance.GetUIPanel(UIPanelType.FloorInfo) as UIFloorPanel;


            var newButton1 = Instantiate(targetButton, floor.txt_Level.transform.parent);
            var newButton2 = Instantiate(targetButton, floor.txt_Level.transform.parent);

            newButton1.gameObject.AddComponent<CombinationRoot>().isSave = true;
            newButton2.gameObject.AddComponent<CombinationRoot>().isSave = false;
        }

        public static void After_FilterBookModels(List<BookModel> list, ref List<BookModel> __result, UIEquipPageScrollList __instance)
        {
            if (__instance == saveComponent) __result = list;
        }
    }
}
