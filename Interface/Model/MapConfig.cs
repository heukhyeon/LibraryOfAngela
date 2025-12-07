using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Model
{
    public abstract class MapConfig : LoAConfig
    {
        public abstract List<CustomMapData> GetMaps();
    }

    public class LoAMapManager : MapManager
    {
        public CustomMapData data;
        private Sprite mapBg;
        private Sprite floorSprite;
        protected SpriteRenderer bgRender { get; private set; }
        protected SpriteRenderer floorRender { get; private set; }
        private List<Action<MapManager>> updateCallbacks = new List<Action<MapManager>>();
        private bool isInvitation = false;
        private bool isDialogSet = false;
        private bool isCallbackSet = false;
        private bool renderInit = false;
        private List<string> dialogs;
        private CreatureDlgEffectUI _dlgEffect;
        private int dialogIdx = -1;

        private const float BORDER_SCALE = 6.5f;

        public static LoAMapManager Create(SephirahType sephira, CustomMapData data, bool isInvitation, ILoACustomMapMod mod)
        {
            var obj = new GameObject("LoACustomMap_");
            var type = data.managerType ?? typeof(LoAMapManager);
            var manager = obj.AddComponent(type) as LoAMapManager;
            manager.name = data.mapName;
            manager.sephirahType = sephira;
            manager.data = data;
            manager.mapSize = MapSize.L;
            manager.isCreature = !isInvitation;
            manager.wallCratersPrefabs = new GameObject[0];
            manager.isInvitation = isInvitation;
            manager.sephirahColor = data.frameVignetteColor;
            var targetAssetBundle = (mod as ILoACustomAssetBundleMod)?.AssetBundles;
            var targetArtworks = mod.Artworks;
            if (targetAssetBundle != null && data.bgmSource != null && isInvitation)
            {
                manager.mapBgm = data.bgmSource.Select(x =>
                {
                    var b = targetAssetBundle.LoadManullay<AudioClip>(x);
                    // Debug.Log("오디오 로딩 :::: " + "/" + x + "/" + (b != null));
                    return b;
                }).ToArray();
            }
            else
            {
                manager.mapBgm = new AudioClip[1] { null };
            }
            if (!string.IsNullOrEmpty(data.backgroundArtwork)) manager.mapBg = targetArtworks[data.backgroundArtwork];
            if (!string.IsNullOrEmpty(data.floorArtwork)) manager.floorSprite = targetArtworks[data.floorArtwork];
            Debug.Log($"LoA Map Create : {manager.mapBg != null} / {data.packageId} / {data.mapName} / {data.backgroundArtwork}");
            return manager;
        }

        public override void Awake()
        {
            base.Awake();
            borderFrame = new GameObject("Border");
            backgroundRoot = new GameObject("Background");
            backgroundRoot.transform.parent = transform;
            //backgroundRoot.layer = LayerMask.NameToLayer("Background_Back");
            borderFrame.transform.parent = transform;
        }

        private bool isInit = false;
        public bool isSpecialPick = false;

        protected virtual void OnDestroy()
        {
            updateCallbacks.Clear();
            if (this._dlgEffect != null && this._dlgEffect.gameObject != null)
            {
                UnityEngine.Object.Destroy(this._dlgEffect.gameObject);
                this._dlgEffect = null;
            }
            SingletonBehavior<CreatureDlgManagerUI>.Instance.Clear();
        }

        public override void InitializeMap()
        {
            base.InitializeMap();
            renderInit = true;
            if (isInvitation)
            {
                var kether = global::Util.LoadPrefab("LibraryMaps/KETHER_Map").GetComponent<KetherMapManager>();
                borderFrame = Instantiate(kether.borderFrame, transform);
                borderFrame.transform.localScale = new Vector3(BORDER_SCALE, BORDER_SCALE, 1f);

                Destroy(kether.gameObject);
                if (data.mapDialogs != null && data.mapDialogs.Length > 0)
                {
                    SetTextIdDialogs(data.mapDialogs.ToList());
                }
            }
            else
            {
                borderFrame = new GameObject("LoA_Dummy_BorderFrame");
            }

            var obj = new GameObject("BackgroundRender");
            var obj2 = new GameObject("FloorRender");
            bgRender = obj.AddComponent<SpriteRenderer>();
            floorRender = obj2.AddComponent<SpriteRenderer>();
            obj.transform.SetParent(backgroundRoot.transform);
            obj.layer = backgroundRoot.layer;
            obj2.transform.SetParent(transform);
            bgRender.sprite = mapBg;
            floorRender.sprite = floorSprite;
            floorRender.sortingOrder = 1;
            obj.transform.localScale = data.bgScale;
            obj.transform.position = data.bgPosition;
            obj2.transform.localScale = data.floorScale == Vector3.zero ? data.bgScale : data.floorScale;
            obj2.transform.position = data.floorPosition;
            if (data.isXRotatedFloor) obj2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _roots = new GameObject[] { gameObject };
        }

        public override GameObject GetScratch(int lv, Transform parent)
        {
            return null;
        }

        protected virtual void Update()
        {
            if (isCallbackSet)
            {
                foreach (var x in updateCallbacks)
                {
                    try
                    {
                        x(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            if (isDialogSet)
            {
                if (_dlgEffect?.DisplayDone != false)
                {
                    var nextIndex = dialogIdx + 1;
                    if (nextIndex <= 0 || nextIndex >= dialogs.Count)
                    {
                        nextIndex = 0;
                    }
                    _dlgEffect?.FadeOut();
                    _dlgEffect = SingletonBehavior<CreatureDlgManagerUI>.Instance.SetDlg(dialogs[nextIndex], null);
                    dialogIdx = nextIndex;
                    OnCreateDialog(_dlgEffect, dialogIdx);
                }
            }
        }

        public override void LateUpdate()
        {
            if (data == null) return;
            if (!isInit && borderFrame != null)
            {
                isInit = true;
                borderFrame.transform.localScale = new Vector3(BORDER_SCALE, BORDER_SCALE, 1f);
                var pos = borderFrame.transform.position;
                borderFrame.transform.position =  new Vector3(pos.x, 26f, pos.z);
                bgRender.gameObject.layer = LayerMask.NameToLayer("Background_Back");
            }
            base.LateUpdate();
            bgRender.transform.localPosition = data.bgPosition;
        }

        public void AddMapUpdateCallback(Action<MapManager> action)
        {
            isCallbackSet = true;
            updateCallbacks.Add(action);
        }

        public void UpdateMap(Sprite background, Sprite floor, bool showEffect)
        {
            if (!renderInit)
            {
                Debug.Log("LoA :: Renderer Not Initialized, Maybe 'base.InitializeMap()' Not Called in your overrided 'InitializeMap' Please Check");
                return;
            }

            if (background != null)
            {
                bgRender.sprite = background;
            }
            else
            {
                Debug.Log("LoA :: UpdateMap Called But Backgorund Sprite Null, Backgorund Update Skip");
            }
            if (floor != null)
            {
                floorRender.sprite = floor;
            }
            else
            {
                Debug.Log("LoA :: UpdateMap Called But Floor Sprite Null, Floor Update Skip");
            }

            if (showEffect)
            {
                BattleSceneRoot.Instance._mapChangeFilter.StartMapChangingEffect(Direction.LEFT, true);
            }
        }
    
        public void SetRawTextDialogs(List<string> dialogs)
        {
            this.dialogs = dialogs;
            isDialogSet = dialogs != null && dialogs.Count > 0;
            dialogIdx = -1;
            if (!isDialogSet)
            {
                _dlgEffect?.FadeOut();
            }
            CreatureDlgManagerUI.Instance.Init(isDialogSet);
        }

        public void SetTextIdDialogs(IEnumerable<string> dialogs)
        {
            var d = new List<string>();
            foreach (var l in dialogs)
            {
                d.Add(TextDataModel.GetText(l));
            }
            SetRawTextDialogs(d);
        }

        protected virtual void OnCreateDialog(CreatureDlgEffectUI effect, int idx)
        {

        }
    }
}
