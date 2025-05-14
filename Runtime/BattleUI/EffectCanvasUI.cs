using LibraryOfAngela.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace LibraryOfAngela.BattleUI
{
    class EffectCanvasUI : MonoBehaviour
    {
        public Canvas canvas { get; private set; }
        public Camera camera { get; private set; }

        private Canvas originCanvas { get; set; }

        private Canvas renderCanvas { get; set; }

        public RawImage renderImage { get; set; }

        private RenderTexture texture { get; set; }

        private static int _effectRef = 0;
        public static int effectRef
        {
            get => _effectRef;
            set
            {
                _effectRef = value;
            }
        }

        private static int LAYER = LayerMask.NameToLayer("PP_Gacha");
        private GameObject volume;

        private const string RENDER_TEXTURE_PATH = "Assets/Bundle/Framework/EffectTexture.renderTexture";
        private const string RENDER_ASSET_PATH = "Assets/Bundle/Framework/EffectAsset.prefab";
        private const string RENDER_VOLUME_PATH = "Assets/Bundle/Framework/LoAPostProcessVolume.prefab";

        public static EffectCanvasUI Create(Canvas origin, Camera originCamera)
        {
            var canvas = Instantiate(origin.gameObject, origin.transform.parent).GetComponent<Canvas>();
            foreach (Transform child in canvas.transform)
            {
                Destroy(child.gameObject);
            }
            Destroy(canvas.GetComponent<BattleUnitCardsInHandUI>());
            Destroy(canvas.GetComponent<BattleUnitInformationUI>());
            var renderCanvas = Instantiate(origin.gameObject, origin.transform.parent).GetComponent<Canvas>();
            foreach (Transform child in renderCanvas.transform)
            {
                Destroy(child.gameObject);
            }
            Destroy(renderCanvas.GetComponent<BattleUnitCardsInHandUI>());
            Destroy(renderCanvas.GetComponent<GraphicRaycaster>());
            Destroy(renderCanvas.GetComponent<BattleUnitInformationUI>());

            var ui = canvas.gameObject.AddComponent<EffectCanvasUI>();
            ui.canvas = canvas;
            ui.originCanvas = origin;
            ui.renderCanvas = renderCanvas;
            ui.canvas.gameObject.layer = LAYER;
            ui.gameObject.name = "LoACanvas";
            renderCanvas.gameObject.name = "LoARenderCanvus";

            ui.camera = Instantiate(originCamera.gameObject, originCamera.transform.parent).GetComponent<Camera>();
            ui.camera.CopyFrom(originCamera);
            ui.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            ui.camera.clearFlags = CameraClearFlags.SolidColor;
            ui.camera.depth = -1;
            ui.camera.cullingMask = LayerMask.GetMask("PP_Gacha");
            var res = Resources.FindObjectsOfTypeAll<PostProcessResources>();
            var layer = ui.camera.gameObject.AddComponent<PostProcessLayer>();
            layer.Init(res[0]);
            layer.volumeTrigger = ui.camera.transform;
            layer.volumeLayer = ui.camera.cullingMask;

            ui.renderImage = Instantiate(LoAFramework.BattleUiBundle.LoadAsset<GameObject>(RENDER_ASSET_PATH), renderCanvas.transform).GetComponent<RawImage>();
            ui.renderImage.raycastTarget = false;
            ui.texture = LoAFramework.BattleUiBundle.LoadAsset<RenderTexture>(RENDER_TEXTURE_PATH);
            ui.volume = Instantiate(LoAFramework.BattleUiBundle.LoadAsset<GameObject>(RENDER_VOLUME_PATH), ui.camera.transform.parent);
            ui.camera.targetTexture = ui.texture;
            ui.renderImage.texture = ui.texture;

            Logger.Log("LoAEffectCanvasUI Create Success");
            return ui;
        }

        public void LateUpdate()
        {
            var visible = StageController.Instance.phase == StageController.StagePhase.ApplyLibrarianCardPhase && effectRef > 0;

            if (visible != camera.gameObject.activeSelf)
            {
                camera.gameObject.SetActive(visible);
                renderImage.gameObject.SetActive(visible);
                volume.gameObject.SetActive(visible);
            }

            if (volume != null)
            {
                volume.layer = LAYER;
            }
            if (originCanvas.sortingOrder >= renderCanvas.sortingOrder)
            {
                renderCanvas.sortingOrder = originCanvas.sortingOrder + 1000;
            }
        }
    }
}
