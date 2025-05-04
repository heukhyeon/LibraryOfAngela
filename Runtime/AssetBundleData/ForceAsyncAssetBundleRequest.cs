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
        public bool initRequire;
        private Stopwatch watch;
        private List<AssetBundleType> requestTypes;
        private int requestCount;
        private bool reserved;

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
            var key = new AssetBundleType.Invitation(id);
            var response = LoAAssetBundles.Instance.LoadAssetBundle(key, forceAsync: true, onComplete: (isAsync, success) => OnLoaded(isAsync, success));

            if (response.syncAssetBundleCount > 0)
            {
                var cnt = response.syncAssetBundleCount - response.syncLoadedAssetBundleCount;
                if (cnt > 0)
                {
                    isRunning = true;
                    watch = Stopwatch.StartNew();
                    requestTypes = new List<AssetBundleType>();
                    requestTypes.Add(key);
                    requestCount += cnt;
                }
            }
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
                var response = LoAAssetBundles.Instance.LoadAssetBundle(key, forceAsync: true, onComplete: (isAsync, success)=> OnLoaded(isAsync, success));
                if (response.syncAssetBundleCount > 0)
                {
                    var cnt = response.syncAssetBundleCount - response.syncLoadedAssetBundleCount;
                    if (cnt > 0)
                    {
                        requestCount += cnt;
                        if (!isRunning)
                        {
                            isRunning = true;
                            watch = Stopwatch.StartNew();
                            requestTypes = new List<AssetBundleType>();
                        }
                        requestTypes.Add(key);
                    }
    
                }
            }
        }

        private void OnLoaded(bool isOriginAsync, bool success)
        {
            if (!isOriginAsync)
            {
                requestCount--;
                if (requestCount == 0)
                {
                    LoAAssetBundles.Instance.currentForceSyncRequest = default(ForceAsyncAssetBundleRequest);
                    isRunning = false;
                    watch.Stop();
                    var logger = new StringBuilder($"Invitation Load AssetBundle Duration : {(watch.ElapsedMilliseconds / 1000.0)}s\n");
                    logger.AppendLine("Loaded AssetBundleType:");
                    foreach (var d in requestTypes)
                    {
                        logger.Append("-");
                        logger.Append(d.ToString());
                        logger.Append("\n");
                    }
                    Logger.Log(logger.ToString());
                    if (reserved)
                    {
                        GameSceneManager.Instance.battleScene.gameObject.SetActive(true);
                        BattleSceneRoot.Instance.StartBattle();
                    }
                }
                else
                {
                    Logger.Log("Remain Sync AssetBundle Count :" + requestCount);
                }
            }
        }
    }
}
