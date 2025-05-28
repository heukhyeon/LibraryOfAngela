using LibraryOfAngela.AssetBundleData;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using HarmonyLib;
using System.Collections.Concurrent;

namespace LibraryOfAngela
{
    enum AssetBundleLoadingType
    {
        // 명확한 용도가 정의되지 않은 경우입니다.
        // 로드 시간 : 게임 실행 직후 비동기
        // 이 타입은 게임 실행 내내 유지되지만, 명확한 로딩 시간을 보장해주지 않습니다. 하위 타입중 하나를 선택해주기를 권합니다.
        UNKNOWN = 0,
        // SD 의 스프라이트, 또는 SD 의 액션 스크립트 출력에 필요한 VFX 출력에 사용됩니다.
        // 로드 시간 : SD 렌더링 시도시
        // 이 타입은 게임 실행 내내 유지됩니다.
        SD = 1,
        // 접대 한정으로만 출력되는 오브젝트에 사용됩니다.
        // 로드 시간 : 접대 시작
        // 이 타입은 접대 종료시 즉시 해제됩니다.
        INVITATION = 2,
        // 특정 핵심 책장의 패시브 VFX 출력에 사용됩니다.
        // 로드 시간 : 접대 시작
        // 이 타입은 접대 종료시 즉시 해제됩니다.
        CORE_PAGE = 4,
        // 특정 전투 책장의 VFX 출력에 사용됩니다.
        // 로드 시간 : 덱 초기화
        // 이 타입은 접대 종료시 즉시 해제됩니다.
        BATTLE_PAGE = 8,
        // 특정 에고 책장의 배경, 실제 VFX 출력등에 사용됩니다.
        // 로드 시간 : 해당 책장 획득 직후
        // 이 타입은 접대 종료시 즉시 해제됩니다.
        EGO = 16,
    }

    class AssetBundleNode
    {

        public string packageId;

        public bool IsLoaded { get; private set; } = false;

        /// <summary>
        /// key : 객체의 이름
        /// value : 객체의 전체 경로
        /// </summary>
        public readonly Dictionary<string, string> keys = new Dictionary<string, string>();

        public readonly AssetBundleInfo info;

        public UnityEngine.AssetBundle Bundle { get; private set; }

        HashSet<AssetBundleLoadingType> refCounts = new HashSet<AssetBundleLoadingType>();
        private AssetBundleCreateRequest latestRequest;


        public AssetBundleNode(string packageId, AssetBundleInfo info)
        {
            this.packageId = packageId;
            this.info = info;
        }

        private string ValidPath
        {
            get
            {
                return PathProvider.ConvertValidPath(packageId, info.path);
            }
        }

        /// <summary>
        /// 해당 타입으로 리소스 로딩을 시도합니다.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>
        /// 실제로 에셋번들을 로딩했는지에 대한 여부입니다.
        /// true는 에셋번들을 로딩한경우 반환합니다.
        /// false는 이미 로딩된 경우를 반환합니다. 단, 이전에 로딩한 것과 다른 케이스로 로딩한경우 참조카운트를 증가시킵니다.
        /// </returns>
        public bool Load(AssetBundleLoadingType type)
        {
            if (refCounts.Contains(type)) return false;
            if (Bundle == null)
            {
                Bundle = UnityEngine.AssetBundle.LoadFromFile(ValidPath);
                IsLoaded = true;
                refCounts.Add(type);
                return true;
            }
            else
            {
                Logger.Log($"AssetBundleLoad Requested But Is Already Loaded And Add Type: {type} in {ValidPath} (Async : False)");
                refCounts.Add(type);
                return false;
            }
        }

        /// <summary>
        /// 해당 타입으로 리소스 로딩을 비동기로 시도합니다.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>
        /// 리턴 값의 반환은 실제로 에셋번들이 로딩이 완료된 이후에 이루어집니다. (단지 async/await 기능을 통해 그 실행이 스레드를 블록시키지 않습니다.)
        /// true는 에셋번들을 로딩한경우 반환합니다.
        /// false는 이미 로딩된 경우를 반환합니다. 단, 이전에 로딩한 것과 다른 케이스로 로딩한경우 참조카운트를 증가시킵니다.
        /// </returns>
        public async Task<bool> LoadAsync(AssetBundleLoadingType type)
        {
            if (refCounts.Contains(type))
            {
                if (LoAFramework.DEBUG)
                {
                    Logger.Log($"AssetBundleLoad Requested But Is Already Loaded For Type: {type} in {ValidPath}, Skip");
                }
                return false;
            }

            if (Bundle == null && latestRequest == null)
            {
                refCounts.Add(type);
                latestRequest = UnityEngine.AssetBundle.LoadFromFileAsync(ValidPath);
                latestRequest.allowSceneActivation = false;
                while (!latestRequest.isDone)
                {
                    if (latestRequest.progress == 0.9f) latestRequest.allowSceneActivation = true;
                    await Task.Yield();
                }
                if (!refCounts.Contains(type))
                {
                    Logger.Log($"AssetBundle Load Canceled?? : {ValidPath}");
                    latestRequest.assetBundle.Unload(true);
                    return false;
                }
                Bundle = latestRequest.assetBundle;
                IsLoaded = true;
                latestRequest = null;
                return true;
            }
            else
            {
                Logger.Log($"AssetBundleLoad Requested But Is Already Loaded And Add Type: {type} in {ValidPath} (Async : True)");
                refCounts.Add(type);
                return false;
            }
        }

        /// <summary>
        /// 해당 타입에 대한 리소스 해제를 시도합니다.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>
        /// 번들에 대한 실제 할당 해제를 시도했는지를 반환합니다.
        /// true 인 경우, 해당 타입이 제거됨으로서 번들의 참조카운트가 0이 되었기에 번들을 제거합니다.
        /// false 인 경우, 이미 번들이 해제되었거나 참조카운트를 제거했으나 남은 참조카운트가 존재하기에 번들을 해제하지 않은경우입니다.
        /// </returns>
        public bool RemoveNode(AssetBundleLoadingType type)
        {
            // 이미 할당 해제 된 경우
            if (Bundle == null) return false;
            // 참조 카운트에 해당 타입이 존재하지 않은 경우
            if (!refCounts.Remove(type)) return false;
            if (refCounts.Count == 0)
            {
                Bundle.Unload(true);
                Bundle = null;
                IsLoaded = false;
                return true;
            }
            else
            {
                Logger.Log($"AssetBundle Remove Node : {info.path} (From {type}, Remain : {string.Join(",", refCounts)})");
            }
            return false;
        }
    }

    class AsyncAssetBundleRequest
    {
        public AssetBundleInfo info;
        public AssetBundleLoadingType type;
        public bool isOriginAsync = true;
        public Action<bool, bool> action;
    }

    struct AssetBundleLoadResponse
    {
        public int syncAssetBundleCount;
        public int asyncAssetBundleCount;
        public int syncLoadedAssetBundleCount;
    }

    class LoAAssetBundles : Singleton<LoAAssetBundles>
    {
        private List<AssetBundleNode> nodes = new List<AssetBundleNode>();
        // 전체 범위는 nodes를 벗어나지 않음. 로딩이 완료된 번들들만 있음 
        private Dictionary<string, List<AssetBundleNode>> keyNodes = new Dictionary<string, List<AssetBundleNode>>();
        // 전체 범위는 nodes를 벗어나지 않음
        private Dictionary<AssetBundleInfo, AssetBundleNode> loadRequireNodes = new Dictionary<AssetBundleInfo, AssetBundleNode>();

        private HashSet<AssetBundleType> loadedTargets = new HashSet<AssetBundleType>();

        private Queue<AsyncAssetBundleRequest> asyncQueue = new Queue<AsyncAssetBundleRequest>();
        private bool asyncStart = false;
        public ForceAsyncAssetBundleRequest currentForceSyncRequest;
        
        public void Initialize()
        {
            foreach (var mod in LoAModCache.Instance.Select(x => x.AssetBundleConfig))
            {
                foreach (var info in mod.GetAssetBundleInfos())
                {
                    if (info.types == null && info.type is null) info.type = AssetBundleType.Default;
                    info.packageId = mod.packageId;
                    var node = new AssetBundleNode(mod.packageId, info);
                    loadRequireNodes[info] = node;
                    nodes.Add(node);
                    if (info.type == AssetBundleType.Default && info.types is null)
                    {
                        asyncQueue.Enqueue(new AsyncAssetBundleRequest { type = AssetBundleLoadingType.UNKNOWN, info = info });
                    }
                }
            }
            InternalExtension.SetRange(GetType());
        }

        // 메인 화면 복귀시 에셋 언로드
        [HarmonyPatch(typeof(UIControlManager), "OnChangeUIPhase")]
        [HarmonyPostfix]
        private static void After_OnChangeUIPhase(UIPhase prev, UIPhase current)
        {
            if (current == UIPhase.Sephirah)
            {
                Instance.RemoveAssetBundle(AssetBundleLoadingType.INVITATION);
                Instance.RemoveAssetBundle(AssetBundleLoadingType.BATTLE_PAGE);
                Instance.RemoveAssetBundle(AssetBundleLoadingType.EGO);
                Instance.RemoveAssetBundle(AssetBundleLoadingType.CORE_PAGE);
                Instance.loadedTargets.RemoveWhere(x =>
                {
                    if (x is AssetBundleType.Sd sd)
                    {
                        return sd.isOnlyBattle;
                    }
                    return true;
                });
                SkinRenderPatch.RestoreRecycleableBundles();
            }
        }

        // 접대 시작시 접대에 대응하는 에셋 로드
        [HarmonyPatch(typeof(UI.UIController), "OnClickGameStart")]
        [HarmonyPrefix]
        private static void Before_OnClickGameStart()
        {
            try
            {
                Instance.currentForceSyncRequest = new ForceAsyncAssetBundleRequest();
                Instance.currentForceSyncRequest.initRequire = true;
                Instance.currentForceSyncRequest.CheckInvitation();
                Instance.currentForceSyncRequest.CheckUnits();
                Instance.currentForceSyncRequest.initRequire = false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // 다른 모드 등에서 접대 이외의 방법으로 전투 시작시
        [HarmonyPatch(typeof(GlobalGameManager), "LoadBattleScene")]
        [HarmonyPrefix]
        private static void Before_LoadBattleScene()
        {
            var id = StageController.Instance.GetStageModel()?.ClassInfo?.id;
            if (Instance.currentForceSyncRequest?.targetInvitationId != id)
            {
                Before_OnClickGameStart();
            }
        }


        [HarmonyPatch(typeof(BattleSceneRoot), "StartBattle")]
        [HarmonyPrefix]
        private static bool Before_StartBattle(BattleSceneRoot __instance)
        {
            if (Instance.currentForceSyncRequest?.IsValid() == true)
            {
                GameSceneManager.Instance.battleScene.gameObject.SetActive(false);
                return false;
            }
            return true;
        }


        internal T LoadAsset<T>(string packageId, string name, bool expectNotExists) where T : UnityEngine.Object
        {
            var realKey = name.ToLower();
            var node = keyNodes.SafeGet(packageId)?.Find(x => x.keys.ContainsKey(realKey) == true);
            if (node == null)
            {
                if (!expectNotExists)
                {
                    Logger.Log($"LoA AssetBundle : Not Matched Asset {realKey} in {packageId}, Please Check");
                }
                return null;
            }
            var result = node.Bundle?.LoadAsset<T>(node.keys[realKey]);
            if (result == null)
            {
                Logger.Log($"LoA AssetBundle : Asset is Null : {name} in {packageId}");
            }
            return result;
        }

        public void LoadAssetBundleAll(string packageId, string path)
        {
            var targets = new List<AsyncAssetBundleRequest>();
            foreach (var target in loadRequireNodes)
            {
                if (target.Value.packageId == packageId && target.Key.path == path)
                {
                    targets.Add(new AsyncAssetBundleRequest { info = target.Key, type = AssetBundleLoadingType.INVITATION });
                }
            }
            foreach (var t in targets) asyncQueue.Enqueue(t);
        }

        public AssetBundleLoadResponse LoadAssetBundle(AssetBundleType target, Action<bool, bool> onComplete = null, bool forceAsync = false)
        {
            var ret = default(AssetBundleLoadResponse);
            if (loadedTargets.Contains(target))
            {
                if (LoAFramework.DEBUG)
                {
                    Logger.Log($"AssetBundle Load Requested, But Always Loaded, Skip : {target}");
                }
                return ret;
            }
            loadedTargets.Add(target);
            var loadType = CreateLoadingType(target);
            StringBuilder logger = null;
            if (LoAFramework.DEBUG)
            {
                logger = new StringBuilder($"LoA AssetBundle Load Requested : {target}\n");
            }
            var targets = new List<Tuple<bool, bool, AssetBundleInfo>>();
            foreach (var t in loadRequireNodes)
            {
                var isAsync = t.Key.IsAsync(target);
                if (isAsync != null)
                {
                    if (logger != null) logger.AppendLine($"- Load Target : {t.Key.path}");
                    if (isAsync.Value)
                    {
                        ret.asyncAssetBundleCount++;
                    }
                    else
                    {
                        ret.syncAssetBundleCount++;
                    }
                    targets.Add(new Tuple<bool, bool, AssetBundleInfo>(isAsync.Value, forceAsync || isAsync.Value, t.Key));
                }
                else if (logger != null)
                {
                    logger.AppendLine($"- Skip Target : {t.Key.path}");
                }
            }
            foreach (var t in targets)
            {
                if (t.Item2)
                {
                    LoadAssetBundleAsync(new AsyncAssetBundleRequest { type = loadType, info = t.Item3, action = onComplete, isOriginAsync = t.Item1 });
                }
                else
                {
                    var res = LoadAssetBundleSync(t.Item3, loadType);
                    onComplete?.Invoke(false, res);
                    ret.syncLoadedAssetBundleCount++;
                }
            }
            if (logger != null) Logger.Log(logger.ToString(), true);
            return ret;
        }

        private bool LoadAssetBundleSync(AssetBundleInfo info, AssetBundleLoadingType type)
        {
            var target = loadRequireNodes.SafeGet(info);
            if (target is null)
            {
                Logger.Log("Try AssetBundle Load But NotFound ::" + info.ToString());
                return false;
            }
            var result = target.Load(type);
            OnAssetBundleLoadTryComplete(target, result, true);
            return result;
        }

        private async void LoadAssetBundleAsync(AsyncAssetBundleRequest request)
        {
            var target = loadRequireNodes.SafeGet(request.info);
            if (target is null) return;

            var result = await target.LoadAsync(request.type);

            OnAssetBundleLoadTryComplete(target, result, false);
            request.action?.Invoke(request.isOriginAsync, result);
        }

        private void OnAssetBundleLoadTryComplete(AssetBundleNode node, bool result, bool isSync)
        {
            var strBuilder = new StringBuilder("");
            try
            {
                var success = result && node.Bundle != null;
                if (success)
                {
                    if (keyNodes.ContainsKey(node.packageId)) keyNodes[node.packageId].Add(node);
                    else keyNodes[node.packageId] = new List<AssetBundleNode> { node };
                    foreach (var value in node.Bundle.GetAllAssetNames())
                    {
                        var key = Path.GetFileNameWithoutExtension(value);
                        node.keys[key] = value;
                        if (LoAFramework.DEBUG) strBuilder.AppendLine($"- {key}");
                    }
                }
                loadRequireNodes.Remove(node.info);
                if (success)
                {
                    if (isSync)
                    {
                        strBuilder.Insert(0, $"AssetBundle Loaded (Sync : {isSync}) : {node.info.path}");
                        Logger.Log(strBuilder.ToString());
                    }
                    else
                    {
                        strBuilder.Insert(0, $"- {node.info.path}\n");
                        bundleLoadQueue.Enqueue(strBuilder);
                    }
               
                }

                LoAArtworks.Instance.OnAssetBundleLoaded(node.info, node.packageId, node.Bundle);
            }
            catch (Exception e)
            {
                Logger.Log($"Error in AssetBundle Load (Bundle Exists: {node.Bundle != null} // {result}) Complete -> {node.info.path}");
                Logger.LogError(e);
            }

        }

        private AssetBundleLoadingType CreateLoadingType(AssetBundleType target)
        {
            AssetBundleLoadingType loadType = AssetBundleLoadingType.UNKNOWN;
            if (target is AssetBundleType.Invitation)
            {
                loadType = AssetBundleLoadingType.INVITATION;
            }
            else if (target is AssetBundleType.CorePage)
            {
                loadType = AssetBundleLoadingType.CORE_PAGE;
            }
            else if (target is AssetBundleType.BattlePage)
            {
                loadType = AssetBundleLoadingType.BATTLE_PAGE;
            }
            else if (target is AssetBundleType.Ego)
            {
                loadType = AssetBundleLoadingType.EGO;
            }
            else if (target is AssetBundleType.Sd t)
            {
                loadType = AssetBundleLoadingType.SD;
                if (t.isOnlyBattle) loadType = AssetBundleLoadingType.INVITATION;
            }
            return loadType;
        }

        private void RemoveAssetBundle(AssetBundleLoadingType type)
        {
            StringBuilder logger = null;
            foreach (var node in nodes)
            {
                if (node.IsLoaded && node.RemoveNode(type))
                {
                    keyNodes[node.packageId].Remove(node);
                    loadRequireNodes[node.info] = node;
                    node.keys.Clear();
                    LoAArtworks.Instance.OnAssetBundleUnloaded(node.info);
                    if (logger is null)
                    {
                        logger = new StringBuilder("AssetBundle Unloaded\n");
                    }
                    logger.AppendLine("-" + node.info.path);
                }
            }
            if (logger != null) Logger.Log(logger.ToString());
        }

        private ConcurrentQueue<StringBuilder> bundleLoadQueue = new ConcurrentQueue<StringBuilder>();

        public async void LoopAsyncAssetBundleLoad()
        {
            if (asyncStart) return;

            Logger.Log("Async Load Start");

            asyncStart = true;

            int cnt = 0;
            while (true)
            {
                if (bundleLoadQueue.Count != cnt)
                {
                    cnt = bundleLoadQueue.Count;
                }
                else if (cnt > 0)
                {
                    var totalBuilder = new StringBuilder("AssetBundleLoad Async Request Log\n");
                    while (bundleLoadQueue.TryDequeue(out StringBuilder str))
                    {
                        totalBuilder.Append(str.ToString());
                    }
                    Logger.Log(totalBuilder.ToString());
                }

                if (asyncQueue.Count == 0)
                {
                    await Task.Delay(500);
                    continue;
                }
                if (currentForceSyncRequest?.initRequire == true)
                {
                    await Task.Delay(10);
                    continue;
                }
                var target = asyncQueue.Dequeue();
                LoadAssetBundleAsync(target);
            }
        }
    }
}
