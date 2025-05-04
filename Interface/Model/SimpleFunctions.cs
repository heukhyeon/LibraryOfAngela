using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Model
{
    public class SimpleLambdas
    {
        public static bool OnlyParrying(BattlePlayingCardDataInUnitModel model)
        {
            var target = model.target;
            if (target.IsBreakLifeZero()) return false;
            if (model.card.GetSpec().Ranged == LOR_DiceSystem.CardRange.Far) return false;
            var targetCard = target.cardSlotDetail.cardAry[model.targetSlotOrder];
            var flag1 = targetCard?.isDestroyed == false  && targetCard?.target == model.owner && targetCard?.targetSlotOrder == model.slotOrder;
            var flag2 = target.cardSlotDetail.keepCard.cardBehaviorQueue.Count(x => !x.DiceDestroyed) > 0;
            return flag1 || flag2;
        }

        public static bool AlwaysNotMove(BattlePlayingCardDataInUnitModel model)
        {
            return false;
        }
    }
}
