using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    public struct LoAEmotionSelector
    {
        public readonly bool exists;
        public readonly string packageId;
        public readonly string title;
        public readonly string artwork;
        public readonly int level;
        public readonly Queue<EmotionCardXmlInfo> cards;
        public readonly BattleUnitModel[] targetUnits;

        internal LoAEmotionSelector(string packageId, string title, string artwork, int level, Queue<EmotionCardXmlInfo> cards, BattleUnitModel[] targetUnits)
        {
            exists = true;
            this.packageId = packageId;
            this.title = title;
            this.artwork = artwork;
            this.level = level;
            this.cards = cards;
            this.targetUnits = targetUnits;
        }
    }

    public class EmotionSelectBuilder
    {
        private readonly string packageId;
        private string title;
        private string artwork;
        private int level;
        private Queue<EmotionCardXmlInfo> cards = new Queue<EmotionCardXmlInfo>();
        private BattleUnitModel[] targetUnits;
        private Action<BattleUnitModel, EmotionCardXmlInfo> onSelect;

        public EmotionSelectBuilder(ILoAMod mod)
        {
            packageId = mod.packageId;
        }

        public EmotionSelectBuilder SetTitle(string title)
        {
            this.title = title;
            return this;
        }

        public EmotionSelectBuilder SetArtwork(string artwork)
        {
            this.artwork = artwork;
            return this;
        }

        public EmotionSelectBuilder SetLevel(int level)
        {
            this.level = level;
            return this;
        }

        public EmotionSelectBuilder SetCards(List<EmotionCardXmlInfo> infos)
        {
            cards.Clear();
            infos.ForEach(cards.Enqueue);
            return this;
        }

        public EmotionSelectBuilder SetTargetUnits(params BattleUnitModel[] targetUnit)
        {
            targetUnits = targetUnit;
            return this;
        }

        public EmotionSelectBuilder SetOnSelect(Action<BattleUnitModel, EmotionCardXmlInfo> onSelect)
        {
            this.onSelect = onSelect;
            return this;
        }

        public void Show()
        {
            var info = new EmotionPannelInfo { packageId = packageId, title = title, artwork = artwork, level = level, cards = cards.ToList(), matchedTarget = targetUnits?.ToList(), onSelect = onSelect };
            ServiceLocator.Instance.GetInstance<ILoARoot>().ShowEmotionSelectUI(info);
        }
    }
}
