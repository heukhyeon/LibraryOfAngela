using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Util
{
    class LoACardListControllerImpl : ILoACardListController
    {
        private List<BattleDiceCardModel> reserved;
        private List<BattleDiceCardModel> used;
        private List<BattleDiceCardModel> hand;
        private List<BattleDiceCardModel> discarded;

        public LoACardListControllerImpl(BattleAllyCardDetail detail)
        {
            reserved = detail._cardInReserved;
            used = detail._cardInUse;
            hand = detail._cardInHand;
            discarded = detail._cardInDiscarded;
        }

        public LoACardListControllerImpl(BattlePersonalEgoCardDetail detail)
        {
            reserved = detail._cardInReserved;
            used = detail._cardInUse;
            hand = detail._cardInHand;
            discarded = used;
        }


        public List<BattleDiceCardModel> GetList(LoACardListScope scope)
        {
            switch (scope)
            {
                case LoACardListScope.DISCARDED:
                    return discarded;
                case LoACardListScope.USED:
                    return used;
                case LoACardListScope.HAND:
                    return hand;
                case LoACardListScope.RESERVED:
                    return reserved;
            }
            // unreachable
            return null;
        }

        void ILoACardListController.Insert(LoACardListScope scope, BattleDiceCardModel card, int index)
        {
            var list = GetList(scope);
            if (index == -1) list.Add(card);
            else list.Insert(index, card);
        }

        bool ILoACardListController.Remove(LoACardListScope scope, BattleDiceCardModel card)
        {
            var list = GetList(scope);
            return list.Remove(card);
        }

        bool ILoACardListController.Remove(LoACardListScope scope, int index)
        {
            var list = GetList(scope);
            var cnt = list.Count;
            if (index < 0) return false;
            if (cnt - 1 < index) return false;
            list.RemoveAt(index);
            return true;
        }
    }
}
