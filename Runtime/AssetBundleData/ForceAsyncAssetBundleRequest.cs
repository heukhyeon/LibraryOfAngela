using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.AssetBundleData
{
    class ForceAsyncAssetBundleRequest
    {
        private bool isRunning;
        public bool IsDestroyed()
        {
            return !initRequire && requestTypes.Count > 0 &&
                requestTypes.All(type => !LoAAssetBundles.Instance.HasActiveAssetBundleRequest(type));
        }

        public void Initialize()
        {
            initRequire = true;
            try
            {
                CheckInvitation();
                CheckUnits();
            }
            finally
            {
                CompleteInitialization();
            }
        }

        public void Cancel()
        {
            if (isRunning || initRequire)
                Logger.Log($"Cancel Previous AssetBundle Request : {targetInvitationId}, Remaining : {requestCount}");
            reserved = false;
            isRunning = false;
            if (ReferenceEquals(LoAAssetBundles.Instance.currentForceSyncRequest, this))
                LoAAssetBundles.Instance.currentForceSyncRequest = null;
        }

        public void CompleteInitialization()
        {
            initRequire = false;
            if (requestCount == 0) Complete();
        }

        public bool initRequire;
        private Stopwatch watch;
        private readonly List<AssetBundleType> requestTypes = new List<AssetBundleType>();
        private int requestCount;
        private bool reserved;
        public LorId targetInvitationId;

        public bool IsValid()
        {
            if (isRunning)
            {
                Logger.Log("Sync AssetBundle Loading, Waiting");
                reserved = true;
                return true;
            }
            return false;
        }

        public void CheckInvitation()
        {
            var id = StageController.Instance.GetStageModel().ClassInfo.id;
            if (!id.IsWorkshop()) return;
            targetInvitationId = id;
            var key = new AssetBundleType.Invitation(id);
            if (!requestTypes.Contains(key)) requestTypes.Add(key);
            LoAAssetBundles.Instance.LoadAssetBundle(key, onComplete: OnLoaded,
                onLoadStart: isAsync => OnLoadStart(isAsync, key));
        }

        public void CheckUnits()
        {
            var units = new List<UnitBattleDataModel>();
            units.AddRange(StageController.Instance.GetCurrentWaveModel().UnitList);
            units.AddRange(StageController.Instance.GetCurrentStageFloorModel().GetUnitAddedBattleDataList());
            foreach (var unit in units)
            {
                var id2 = unit.unitData.bookItem.BookId;
                if (id2.IsBasic()) continue;
                var key = new AssetBundleType.CorePage(id2);
                if (!requestTypes.Contains(key)) requestTypes.Add(key);
                LoAAssetBundles.Instance.LoadAssetBundle(key, forceAsync: true, onComplete: OnLoaded,
                    onLoadStart: isAsync => OnLoadStart(isAsync, key));
            }
        }

        private void OnLoadStart(bool isOriginAsync, AssetBundleType key)
        {
            if (isOriginAsync) return;
            requestCount++;
            isRunning = true;
            if (watch == null)
            {
                watch = Stopwatch.StartNew();
            }
        }

        private void OnLoaded(bool isOriginAsync, bool success)
        {
            // Detaching the current request is enough to cancel its late callbacks.
            if (!ReferenceEquals(LoAAssetBundles.Instance.currentForceSyncRequest, this)) return;
            if (isOriginAsync) return;
            requestCount--;
            if (initRequire) return;
            if (requestCount == 0) Complete();
            else Logger.Log("Remain Sync AssetBundle Count :" + requestCount);
        }

        private void Complete()
        {
            if (!ReferenceEquals(LoAAssetBundles.Instance.currentForceSyncRequest, this)) return;
            LoAAssetBundles.Instance.currentForceSyncRequest = null;
            isRunning = false;
            if (watch != null)
            {
                watch.Stop();
                var logger = new StringBuilder($"Invitation Load AssetBundle Duration : {(watch.ElapsedMilliseconds / 1000.0)}s\n");
                logger.AppendLine("Loaded AssetBundleType:");
                foreach (var d in requestTypes) logger.AppendLine("-" + d);
                Logger.Log(logger.ToString());
            }
            if (reserved)
            {
                GameSceneManager.Instance.battleScene.gameObject.SetActive(true);
                BattleSceneRoot.Instance.StartBattle();
            }
        }
    }
}
