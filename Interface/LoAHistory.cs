using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela
{
    public class LoAHistoryModel
    {

    }

    public static class LoAHistory
    {
        public static int TotalAddedUnitCount { get => ServiceLocator.Instance.GetInstance<ILoAHistoryController>().GetTotalAddedUnitCount(); }

        public static int TotalDieCount { get => ServiceLocator.Instance.GetInstance<ILoAHistoryController>().GetTotalDieUnitCount(); }

        public static int CurrentWaveDieCount { get => ServiceLocator.Instance.GetInstance<ILoAHistoryController>().GetCurrentWaveDieCount(); }

        public static T GetHistory<T>(this BattleUnitModel owner, bool force = true) where T : LoAHistoryModel, new()
        {
            return ServiceLocator.Instance.GetInstance<ILoAHistoryController>().GetHistory<T>(owner.UnitData.unitData, force);
        }

        public static T GetHistory<T>(this UnitDataModel owner, bool force = false) where T : LoAHistoryModel, new()
        {
            return ServiceLocator.Instance.GetInstance<ILoAHistoryController>().GetHistory<T>(owner, force);
        }
    }

    public interface ILoAHistoryController
    {
        int GetTotalAddedUnitCount();

        int GetTotalDieUnitCount();

        int GetCurrentWaveDieCount();


        T GetHistory<T>(UnitDataModel owner, bool force) where T : LoAHistoryModel, new();
    }
}
