using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LOR_BattleUnit_UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using HarmonyLib;

namespace LibraryOfAngela.Buf
{
    class SpeedDiceBufPatch : Singleton<SpeedDiceBufPatch>
    {

        public void Initialize()
        {
            InternalExtension.SetRange(typeof(SpeedDiceBufPatch));
        }

        [HarmonyPatch(typeof(SpeedDiceBindingBuf), "Init")]
        [HarmonyPostfix]
        public static void After_Init(SpeedDiceBindingBuf __instance)
        {
            var target = __instance._owner?.view?.speedDiceSetterUI?.gameObject;
            (target.GetComponent<SpeedDiceBufController>() ?? target.AddComponent<SpeedDiceBufController>()).Refresh();
        }
    }

    class SpeedDiceBufController : MonoBehaviour
    {
        private SpeedDiceSetter setter;
        private int speedDiceCount = -1;
        void Awake()
        {
            setter = GetComponent<SpeedDiceSetter>();
        }

        void LateUpdate()
        {
            if (speedDiceCount != setter._actiavedSpeedDicesCount)
            {
                speedDiceCount = setter._actiavedSpeedDicesCount;
                foreach(var buf in setter._view.model.bufListDetail.GetActivatedBufList().OfType<SpeedDiceBindingBuf>())
                {
                    var index = buf.TargetSpeedDiceIndex;
                    if (index == -1)
                    {
                        Logger.Log($"SpeedDice Index is negative. ui effect ignored. Please Check This Buf : {buf.GetType().FullName}");
                        continue;
                    }
                    try
                    {
                        if (index >= setter._speedDices.Count)
                        {
                            buf.OnCheckSpeedDiceNotExists();
                            index = buf.TargetSpeedDiceIndex;
                            if (index >= setter._speedDices.Count) continue;
                        }
                        var targetDice = setter._speedDices[index];
                        var com = targetDice.GetComponent<SpeedDiceBufBehaviour>() ?? targetDice.gameObject.AddComponent<SpeedDiceBufBehaviour>();
                        com.Buf = buf;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Logger.Log($"Count Reselect Error.... What? : {index} // {setter._speedDices.Count}");
                    }
                }
            }
        }

        public void Refresh()
        {
            speedDiceCount = -1;
        }
    }

    class SpeedDiceBufBehaviour : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SpeedDiceBindingBuf _buf;
        private SpeedDiceUI dice;
        private ILoAArtworkCache artworkCache;
        bool originSet;

        private Sprite sp;
        private Sprite origin;

        public SpeedDiceBindingBuf Buf
        {
            get => _buf;
            set
            {
                if (_buf == value) return;
                _buf = value;
                if (value != null && !string.IsNullOrEmpty(_buf.Artwork)) artworkCache = LoAModCache.FromAssembly(_buf).Artworks;
                if (dice == null) dice = GetComponent<SpeedDiceUI>();
                if (artworkCache != null) RefreshIcon();
            }
        }

        private void LateUpdate()
        {
            if (Buf?.IsDestroyed() == true)
            {
                if (dice != null && origin != null)
                {
                    dice.img_normalFrame.sprite = origin;
                }

                Destroy(this);
                return;
            }

            if (!originSet && dice?._normalDiceRoot?.activeSelf == true)
            {
                origin = dice.img_normalFrame.sprite;
                originSet = true;
            }

            if (dice == null || sp == null || !dice._normalDiceRoot.activeSelf) return;
            if (dice.img_normalFrame.sprite != sp) dice.img_normalFrame.sprite = sp;
        }

        private void RefreshIcon()
        {
            if (_buf == null) return;
            sp = artworkCache.GetNullable(_buf.Artwork);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (Buf == null) return;
            var name = Buf.bufActivatedName;
            var description = Buf.bufActivatedText;
            if (!string.IsNullOrEmpty(description))
            {
                SingletonBehavior<UIBattleOverlayManager>.Instance.EnableBufOverlay(name, description, null, gameObject);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Buf == null) return;
            SingletonBehavior<UIBattleOverlayManager>.Instance.DisableOverlay();
        }

    }
}
