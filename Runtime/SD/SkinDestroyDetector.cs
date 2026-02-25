using LibraryOfAngela.EquipBook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.SD
{
    class SkinDestroyDetector : MonoBehaviour
    {
        [SerializeField]
        private string packageId;
        [SerializeField]
        private string assetName;
        private bool injected = false;

        public void Register(string packageId, string assetName, bool immediate)
        {
            this.packageId = packageId;
            this.assetName = assetName;
            if (immediate)
            {
                LoAAssetBundles.Instance.UpdateSdResourceRef(packageId, assetName, 1);
                injected = true;
            }
            //Logger.Log("스킨 등록 :" + key.skinName);
        }

        void Awake()
        {
            if (packageId != null && !injected)
            {
                LoAAssetBundles.Instance.UpdateSdResourceRef(packageId, assetName, 1);
                injected = true;
            }
        }

        void OnDestroy()
        {
            if (injected)
            {
                LoAAssetBundles.Instance.UpdateSdResourceRef(packageId, assetName, -1);
                injected = false;
            }
        }
    }
}
