using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Workshop;

namespace LibraryOfAngela.EquipBook
{
    class LegacyLoAFaceData : LoACustomFaceData
    {
        string faceKey = "";
        List<ActionDetail> targetActions;

        public LegacyLoAFaceData(AdvancedSkinInfo info)
        {
            faceKey = info.overrideFaceSprite;
            targetActions = info.overrideFaceTypes;
            packageId = info.packageId;
        }

        public override string GetFrontFaceArtwork(ActionDetail action)
        {
            if (!targetActions.Contains(action)) return null;

            string key;
            switch (action)
            {
                case ActionDetail.Move:
                    key = "move";
                    break;
                case ActionDetail.Slash:
                case ActionDetail.Hit:
                case ActionDetail.Fire:
                    key = "attack";
                    break;
                case ActionDetail.Penetrate:
                    key = "side";
                    break;
                default:
                    key = "default";
                    break;
            }
            return faceKey + "_" + key;
        }

        public override string GetRearFaceArtwork(ActionDetail action)
        {
            return GetFrontFaceArtwork(action) + "_rear";
        }

        public override string GetSettingFaceArtwork()
        {
            return faceKey + "_setting";
        }
    }

    class OriginKeepFaceData : LoACustomFaceData
    {
        public override bool IsDestroyOriginalFace(string currentSkinName)
        {
            return false;
        }

        public override string GetFrontFaceArtwork(ActionDetail action)
        {
            return null;
        }

        public override string GetSettingFaceArtwork()
        {
            return null;
        }
    }

    class LazyLoAFaceComponent : MonoBehaviour
    {
        public string key;
        public string rearKey;

        public ActionDetail action;
        public ILoAArtworkCache artwork;
        private bool isInit = false;
        public SpriteRenderer renderer;
        public SpriteRenderer rearRenderer;
        public void Awake()
        {
            gameObject.SetActive(false);
        }

        public void OnEnable()
        {
            if (!isInit && artwork != null)
            {
                isInit = true;
                renderer.sprite = artwork[key];
                rearRenderer.sprite = artwork.GetNullable(rearKey);
            }
        }
    }
}
