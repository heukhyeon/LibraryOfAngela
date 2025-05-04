using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.BattleUI
{
    class LoACustomCardHandEffect : MonoBehaviour
    {
        private CustomCardHandEffect latestData;
        private GameObject latestEffect;
        private Transform parent;
        private Transform matchTarget;
        private GameObject checkTarget;
        private GameObject scaleTarget;

        public void InjectParent(GameObject instance, Transform parent, Transform matchTarget)
        {
            this.parent = parent;
            this.matchTarget = matchTarget;
            checkTarget = instance.name.Contains("Preview") ? instance.transform.parent.gameObject : instance;
            scaleTarget = instance;
        }

        public void Update()
        {
            if (!(latestEffect is null) && !(matchTarget is null))
            {
                latestEffect.transform.position = matchTarget.position;
                latestEffect.transform.rotation = matchTarget.rotation;
                latestEffect.transform.localScale = Vector3.Scale(latestData.scale, scaleTarget.transform.localScale);
            }
            if (!(checkTarget is null) && checkTarget.gameObject.activeSelf == false && !(latestEffect is null))
            {
                enabled = false;
            }
        }

        public void OnEnable()
        {
            EffectCanvasUI.effectRef++;
            latestEffect?.gameObject?.SetActive(true);
        }

        public void OnDisable()
        {
            EffectCanvasUI.effectRef--;
            latestEffect?.gameObject?.SetActive(false);
        }

        public void OnDestroy()
        {
            if (latestEffect != null)
            {
                Destroy(latestEffect);
                latestEffect = null;
            }
        }

        public void UpdateTarget(CustomCardHandEffect effect)
        {
            var effectExsists = !(effect is null);
            var latestEffectExists = !(latestEffect is null);
            enabled = effectExsists;

            if (!effectExsists && latestEffectExists)
            {
                Destroy(latestEffect);
                latestEffect = null;
                latestEffectExists = false;
            }


            if (latestData == effect) return;
            if (latestEffectExists)
            {
                Destroy(latestEffect);
                latestEffect = null;
            }

            if (!(effect is null))
            {
                var bundle = LoAModCache.Instance[effect.packageId].AssetBundles;
                var targetAsset = bundle[effect.assetKey];

                latestEffect = Instantiate(targetAsset, transform);
                latestEffect.SetActive(true);

                latestEffect.transform.localPosition = Vector3.zero;
                latestEffect.transform.localScale = effect.scale;
                latestEffect.transform.parent = parent;
                Transform[] componentsInChildren = latestEffect.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].gameObject.layer = LayerMask.NameToLayer("PP_Gacha");

                    if (componentsInChildren[i].gameObject.GetComponent<Renderer>() != null)
                    {
                        componentsInChildren[i].gameObject.GetComponent<Renderer>().sortingOrder = parent.GetSiblingIndex() * 2 + 1352;
                    }
                }
                if (latestEffect.GetComponent<Renderer>() != null)
                {
                    latestEffect.GetComponent<Renderer>().sortingOrder = parent.GetSiblingIndex() * 2 + 1352;
                }
            }
            latestData = effect;
        }
    }
}
