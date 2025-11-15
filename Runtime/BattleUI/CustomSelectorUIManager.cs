using LibraryOfAngela.Extension;
using LibraryOfAngela.Model;
using LOR_DiceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace LibraryOfAngela.BattleUI
{
    public class CustomSelectorUIManager : MonoBehaviour
    {
        private static CustomSelectorUIManager _instance;

        public static CustomSelectorUIManager Instance
        {
            get
            {
                if (_instance is null)
                {
                    var asset = LoAFramework.BattleUiBundle.LoadAsset<GameObject>("Assets/CustomSelector/LoACustomSelector.prefab");
                    var obj = Instantiate(asset, SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.transform.parent);
                    _instance = obj.AddComponent<CustomSelectorUIManager>();
                }
                return _instance;

            }
        }

        public static void LazyInit()
        {
            if (_instance != null) return;
            var path = LoAFramework.battleUiAssetPath;
            if (path is null) return;
            var request = AssetBundle.LoadFromFileAsync(path);
            request.completed += (b) =>
            {
                LoAFramework.BattleUiBundle = request.assetBundle;
                Instance.LazyInitialize();
            };
        }

        private CustomSelectorUI ui;
        private CustomSelectorModel currentModel;
        private List<CustomSelectorUiComponent> components = new List<CustomSelectorUiComponent>();
        private int currentSelected = 0;
        private int currentMax = 0;
        private bool isLoadRequire = false;
        private Queue<CustomSelectorModel> modelQueue = new Queue<CustomSelectorModel>();
        private GameObject volume;
        private const string RENDER_VOLUME_PATH = "Assets/Bundle/Framework/LoAPostProcessVolume.prefab";
        public static bool IsSaveLoaded = false;
        public static bool IsAssetLoaded = false;
        public void LazyInitialize()
        {
            ui = GetComponent<CustomSelectorUI>();
            var canvas = GetComponent<Canvas>();
            var targetCanvas = SingletonBehavior<BattleManagerUI>.Instance.ui_levelup._canvas;
            var l = LayerMask.NameToLayer("PP_Gacha");
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = UnityEngine.Object.FindObjectsOfType<Camera>()
                .FirstOrDefault(d => (d.cullingMask & (1 << l)) != 0);
            canvas.sortingOrder = targetCanvas.sortingOrder + 10;
            canvas.sortingLayerName = "PP_Gacha";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.planeDistance = targetCanvas.planeDistance;
            canvas.enabled = false;

            var tr = transform as RectTransform;
            var targetTr = targetCanvas.transform as RectTransform;
            tr.anchorMin = targetTr.anchorMin;
            tr.anchorMax = targetTr.anchorMax;
            tr.sizeDelta = targetTr.sizeDelta;
            var text = SingletonBehavior<BattleManagerUI>.Instance.ui_emotionInfoBar.GetComponentInChildren<TextMeshProUGUI>();
            ui.title.font = text.font;
            ui.title.fontMaterial = text.fontMaterial;
            ui.title.fontSize = 30f;

            // ui.scrollView.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            GameSceneManager.Instance.StartCoroutine(LazyInstantiate());
            volume = Instantiate(LoAFramework.BattleUiBundle.LoadAsset<GameObject>(RENDER_VOLUME_PATH), transform.parent);


            /*            var res = Resources.FindObjectsOfTypeAll<PostProcessResources>();
                        var layer = gameObject.AddComponent<PostProcessLayer>();
                        layer.Init(res[0]);
                        layer.volumeTrigger = canvas.worldCamera.transform;
                        layer.volumeLayer = canvas.worldCamera.cullingMask;*/

        }

        private IEnumerator LazyInstantiate()
        {
            yield return new WaitForSeconds(1f);
            var width = 300f;
            var originCard = BattleManagerUI.Instance.ui_levelup._canvas.GetComponentInChildren<BattleDiceCardUI>(true);
            var originEmotion = BattleManagerUI.Instance.ui_levelup._canvas.GetComponentInChildren<EmotionPassiveCardUI>(true);
            for (int i = 0; i < 20; i++)
            {
                var c = new GameObject($"LoASelector{i + 1}");
                c.transform.SetParent(ui.scrollView.content);
                c.transform.localScale = new Vector3(1f, 1f, 1f);
                c.transform.localPosition = new Vector3(400f + (i * 350), -130f, 0f);
                var com = c.gameObject.AddComponent<CustomSelectorUiComponent>();
                com.manager = this;
                com.Init(originCard, originEmotion);
                components.Add(com);
                currentMax++;
                width += 400f;
                for (int j = 0; j < 30; j++) yield return YieldCache.waitFrame;
            }
            ui.scrollView.content.sizeDelta = new Vector2(width, ui.scrollView.content.sizeDelta.y);
            ui.scrollView.horizontalScrollbar.value = 0f;
            var l = LayerMask.NameToLayer("PP_Gacha");
            foreach (var tr in GetComponentsInChildren<Transform>())
            {
                tr.gameObject.layer = l;
            }

            if (!isShowing)
            {
                gameObject.SetActive(false);
                volume.SetActive(false);
            }

        }

        private void OnDestroy()
        {
            _instance = null;
        }

        private bool isShowing = false;
        public void Init(CustomSelectorModel model)
        {
            if (currentModel != null)
            {
                Logger.Log($"CustomSelector Requested, But Already Other Selector Visible. So Enqueued : {model.title} // (Queue : {modelQueue.Count})");
                modelQueue.Enqueue(model);
                return;
            }
            try
            {
                Logger.Log($"CustomSelector Requested : {model.title} // {model.cards?.Count} // {model.emotions?.Count}");
                this.currentModel = model;
                currentSelected = 0;
                string title = model.title;
                var flag = true;
                if (StageController.Instance.phase == StageController.StagePhase.ApplyLibrarianCardPhase)
                {
                    flag = false;
                }
                else if (!isLoadRequire && !BattleSceneRoot.Instance.currentMapObject.IsRunningEffect)
                {
                    flag = false;
                }
                isLoadRequire = flag;
                if (flag)
                {
                    BattleSceneRoot.Instance.currentMapObject.SetRunningState(true);
                }

                if (model.maxSelectCount > 0)
                {
                    title += $" (0/{model.maxSelectCount})";
                }
                ui.title.text = title;
                var isCardMode = model.cards != null;
                var width = 300f;

                if (isCardMode)
                {
                    int max = model.cards.Count - 1;

                    if (max > currentMax) max = currentMax;
                    for (int i = 0; i < currentMax; i++)
                    {
                        components[i].CardInfo = i <= max ? model.cards[i] : null;
                        components[i].IsSelected = false;
                        if (i <= max) width += 400f;
                    }
                }
                else
                {
                    int max = model.emotions.Count - 1;
                    if (max > currentMax) max = currentMax;
                    for (int i = 0; i < currentMax; i++)
                    {
                        components[i].EmotionInfo = i <= max ? model.emotions[i] : null;
                        components[i].IsSelected = false;
                        if (i <= max) width += 400f;
                    }
                }


                ui.scrollView.content.sizeDelta = new Vector2(width, ui.scrollView.content.sizeDelta.y);
                ui.scrollView.horizontalNormalizedPosition = 0f;
                volume.gameObject.SetActive(true);
                if (string.IsNullOrEmpty(model.artwork))
                {
                    ui.artwork.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    ui.artwork.transform.parent.gameObject.SetActive(true);
                    ui.artwork.sprite = UISpriteDataManager.instance.GetStoryIcon(model.artwork).icon;
                }
                if (!isShowing)
                {
                    ui.scrollView.content.gameObject.SetActive(false);
                    ui.title.gameObject.SetActive(false);
                    isShowing = true;
                    ui.Show(model.color);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

        }

        public void OnSelected(CustomSelectorUiComponent component)
        {
            currentSelected += component.IsSelected ? -1 : 1;
            component.IsSelected = !component.IsSelected;
            if (currentSelected >= currentModel.maxSelectCount)
            {
                OnConfirm();
            }
            else if (currentModel.maxSelectCount > 0)
            {
                string title = currentModel.title + $" ({currentSelected}/{currentModel.maxSelectCount})";
                ui.title.text = title;
            }
        }

        public void OnConfirm()
        {
            if (currentModel is null) return;

            if (currentModel.cards != null)
            {
                var result = new List<DiceCardXmlInfo>();
                components.ForEach(x =>
                {
                    if (x.IsSelected) result.Add(x.CardInfo);
                });
                currentModel.onSelect(new CustomSelectorModel.CardResult { cards = result });
            }
            else
            {
                var result = new List<EmotionCardXmlInfo>();
                components.ForEach(x =>
                {
                    if (x.IsSelected) result.Add(x.EmotionInfo);
                });
                currentModel.onSelect(new CustomSelectorModel.EmotionResult { emotions = result });
            }
            Close();
        }

        public void Close()
        {
            currentModel = null;

            if (modelQueue.Count > 0)
            {
                Init(modelQueue.Dequeue());
            }
            else
            {
                Logger.Log($"CustomSelector Hide");
                isShowing = false;
                ui.Hide();
                ui.title.gameObject.SetActive(false);
                volume.gameObject.SetActive(false);
                components.ForEach(d =>
                {
                    if (d.isCardMode) d.CardInfo = null;
                    else d.EmotionInfo = null;
                });
                if (isLoadRequire)
                {
                    BattleSceneRoot.Instance.enabled = true;
                    BattleSceneRoot.Instance.currentMapObject.SetRunningState(false);
                }
            }
        }
    }

    public class CustomSelectorUiComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        private BattleDiceCardUI card;
        private EmotionPassiveCardUI emotion;
        private DiceCardXmlInfo _cardInfo;
        private EmotionCardXmlInfo _emotionInfo;
        private GameObject selectedObj;
        public bool isCardMode = true;

        public CustomSelectorUIManager manager;
        public bool IsSelected
        {
            get
            {
                if (selectedObj is null)
                {
                    var asset = LoAFramework.BattleUiBundle.LoadAsset<GameObject>("Assets/CustomSelector/LobgelaRe_UI_Card.prefab");
                    selectedObj = Instantiate(asset, transform);
                    selectedObj.gameObject.SetActive(false);
                    selectedObj.transform.SetParent(emotion._rootRect.GetChild(0));
                    //selectedObj.transform.SetParent(card.vibeRect);
                    //selectedObj.transform.localPosition = new Vector3(120f, 550f, 0f);

                }
                return selectedObj.activeSelf;
            }
            set
            {
                if (value)
                {
                    if (selectedObj is null)
                    {
                        var asset = LoAFramework.BattleUiBundle.LoadAsset<GameObject>("Assets/CustomSelector/LobgelaRe_UI_Card.prefab");
                        selectedObj = Instantiate(asset, transform);
                    }
                    if (isCardMode)
                    {
                        selectedObj.transform.SetParent(card.vibeRect);
                        selectedObj.transform.localPosition = new Vector3(120f, 550f, 0f);
                    }
                    else
                    {
                        selectedObj.transform.SetParent(emotion._rootRect.GetChild(0));
                        selectedObj.transform.localPosition = new Vector3(160f, 640f, 0f);
                    }
                    selectedObj.SetActive(true);
                }
                else if (selectedObj != null)
                {
                    selectedObj.SetActive(false);
                }
            }
        }

        public DiceCardXmlInfo CardInfo
        {
            get => _cardInfo;
            set
            {
                if (_cardInfo == value) return;
                _cardInfo = value;
                isCardMode = !(value is null);
                if (card is null) return;
                if (isCardMode)
                {
                    var c = BattleDiceCardModel.CreatePlayingCard(value);
                    if (value.IsEgo())
                    {
                        c.ResetCoolTime();
                        c.SetMaxCooltime();
                        c.SetCurrentCostMax();
                    }
                    card.SetCard(c);
                    card.gameObject.SetActive(true);
                }
                else
                {
                    card.gameObject.SetActive(false);
                }
            }
        }

        public EmotionCardXmlInfo EmotionInfo
        {
            get => _emotionInfo;
            set
            {
                if (_emotionInfo == value) return;
                _emotionInfo = value;
                isCardMode = value is null;
                if (emotion is null) return;
                if (isCardMode)
                {
                    emotion.gameObject.SetActive(false);
                }
                else
                {
                    emotion.gameObject.SetActive(true);
                    emotion.Init(value);
                }
            }
        }



        private void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer("PP_Gacha");
            gameObject.AddComponent<CanvasRenderer>();
        }

        public void Init(BattleDiceCardUI originCard, EmotionPassiveCardUI originEmotion)
        {
            card = CreateDiceCardUI(originCard);
            emotion = CreateEmotionUI(originEmotion);

            if (isCardMode && _cardInfo != null)
            {
                card.SetCard(BattleDiceCardModel.CreatePlayingCard(_cardInfo));
                card.gameObject.SetActive(true);
            }
            else if (!isCardMode && _emotionInfo != null)
            {
                emotion.Init(_emotionInfo);
                emotion.gameObject.SetActive(true);
            }
        }

        private BattleDiceCardUI CreateDiceCardUI(BattleDiceCardUI target)
        {
            var v = Instantiate(target.gameObject, transform);
            var com = v.GetComponent<BattleDiceCardUI>();
            com.scaleOrigin = new Vector3(0.25f, 0.25f, 0.25f);
            com.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            com.SetCard(BattleDiceCardModel.CreatePlayingCard(ItemXmlDataList.instance.GetCardItem(1)));
            com.transform.localPosition = Vector3.zero;
            var trigger = com.GetComponentInChildren<UICustomSelectable>(true);
            trigger.SubmitEvent = new UnityEventBasedata();
            trigger.SubmitEvent.AddListener(Click);
            com.enabled = false;
            com.gameObject.SetActive(false);
            return com;
        }

        private EmotionPassiveCardUI CreateEmotionUI(EmotionPassiveCardUI emotionTarget)
        {
            var v = Instantiate(emotionTarget.gameObject, transform);
            v.transform.localPosition = new Vector3(-70f, -40f, 0f);
            v.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var ret = v.GetComponent<EmotionPassiveCardUI>();
            ret.enabled = false;
            ret.gameObject.SetActive(false);
            return ret;
        }


        private bool _init = false;
        private void LateUpdate()
        {
            if (!_init)
            {
                _init = true;
                var pos = transform.localPosition;
                pos.z = 0f;
                transform.localPosition = pos;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
            if (isCardMode) card.ShowDetail();
            else emotion.OnEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.SetAsFirstSibling();
            if (isCardMode) card.HideDetail();
            else emotion.OnExit();
        }

        public void Click(BaseEventData eventData)
        {
            manager.OnSelected(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Click(eventData);
        }
    }
}
