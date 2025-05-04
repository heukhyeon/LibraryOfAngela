using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela.Buf
{
    class SceneBufImpl : MonoBehaviour
    {
        private Image image;
        private UIBufOverlay overlay;
        private SceneBuf current;
        private BattleUnitInformationUI_BuffSlot slot;

        public SceneBuf Current
        {
            get => current;
            set
            {
                if (current == null && value == null) return;
                current = value;
                gameObject.SetActive(current != null);
                if (value != null)
                {
                    if (current != null)
                    {
                        current.onDescriptionChanged = null;
                    }
       
                    overlay.SetName(Singleton<BattleEffectTextsXmlList>.Instance.GetEffectTextName(value.keywordId));
                    
                    Sprite icon;
                    if (!BattleUnitBuf._bufIconDictionary.TryGetValue(value.keywordIconId, out icon) || icon == null)
                    {
                        icon = BufPatch.ModBufIcon(value.keywordIconId, value);
                    }
                    overlay.SetIcon(icon);
                    image.sprite = icon;
                    scaleVector = Vector3.one * value.iconScale;
                    value.slot = slot;
                    value.onDescriptionChanged = () =>
                    {
                        overlay.SetDescription(value.bufActivatedText);
                    };
                    value.Init();
                    overlay.SetDescription(value.bufActivatedText);
                }
            }
        }

        public void Awake()
        {
            slot = GetComponent<BattleUnitInformationUI_BuffSlot>();
            slot._txt_stacknum.text = "";
            slot._txt_stacknum.transform.localScale = slot._txt_stacknum.transform.localScale * 0.75f;
            image = slot._img_icon;
            overlay = slot._tooltip_buff;
            if (overlay is null)
            {
                overlay = slot.GetComponent<UIBufOverlay>();
            }
            overlay.SetCamera(SingletonBehavior<BattleManagerUI>.Instance.ui_unitInformation.GetCanvas().worldCamera);
            // 

        }

        private Vector3 scaleVector = new Vector3(1.5f, 1.5f, 1.5f);

        public void Update()
        {
            image.transform.localScale = scaleVector;
        }
    }
}
