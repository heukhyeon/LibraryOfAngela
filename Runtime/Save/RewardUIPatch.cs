using HarmonyLib;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;

namespace LibraryOfAngela.Save
{
    class RewardUIPatch
    {
        private static List<ClearReward> currentRewards = null;
        private static int skipState = -1;
        private static LorId skipStageId;

        public static void ReserveSkipResult(LorId stageId, bool isWin)
        {
            skipStageId = stageId;
            skipState = isWin ? 0 : 1;
        }

        public static bool TrySkipResult()
        {
            if (skipState == -1) return false;

            var stageId = StageController.Instance.GetStageModel()?.ClassInfo?.id;
            if (stageId != skipStageId)
            {
                skipState = -1;
                skipStageId = null;
                return false;
            }

            var controller = UI.UIController.Instance;
            var panel = controller.GetUIPanel(UIPanelType.BattleResult) as UIBattleResultPanel;
            if (panel == null)
            {
                skipState = -1;
                skipStageId = null;
                return false;
            }

            // OnClickGameEnd only reads the result's win state. Supply it without opening the result panel.
            AccessTools.Field(typeof(UIBattleResultPanel), "resultWavedata").SetValue(panel,
                new TestBattleResultData { iswin = skipState == 0 });
            skipState = -1;
            skipStageId = null;
            controller.OnClickGameEnd();
            return true;
        }

        public static void ReserveRewardUI(StageClassInfo info, List<ClearReward> rewards)
        {
            if (rewards == null || rewards.Count == 0) return;
            rewards.ForEach(x => x.packageId = info.id.packageId);
            if (LibraryModel.Instance.ClearInfo.GetClearCount(info.id) != 1) return;
            currentRewards = rewards;

        }

        public static void CheckShowRewardUI()
        {
            if (currentRewards != null)
            {
                UIGachaResultPopup.Instance.SetData(currentRewards.OrderBy(x =>
                {
                    if (x.type == DropItemType.Equip) return -10000000 + x.id;
                    else return x.id;
                }).Select(x =>
                {
                    var id = new LorId(x.packageId, x.id);
                    return new BookDropResult
                    {
                        id = id,
                        hasLimit = false,
                        number = 1,
                        itemType = x.type,
                        bookInstanceId = x.type == DropItemType.Equip ? (BookInventoryModel.Instance.GetBookListAll().Find(d => d.BookId == id)?.instanceId ?? -1) : -1,
                    };
                }).ToList(), SephirahType.Keter);
                UIGachaResultPopup.Instance.txt_floorName.text = "Clear Rewards";
                UIGachaResultPopup.Instance.Open();
                UIGachaResultPopup.Instance.StartRevealAnim();
                currentRewards = null;
            }
        }
    }
}
