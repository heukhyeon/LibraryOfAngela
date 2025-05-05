using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Model
{
    public class CustomSelectorModel
    {
        public class Result
        {
            public virtual List<DiceCardXmlInfo> GetResultCards()
            {
                return new List<DiceCardXmlInfo>();
            }

            public virtual List<EmotionCardXmlInfo> GetResultEmotions()
            {
                return new List<EmotionCardXmlInfo>();
            }
        }

        public class CardResult : Result
        {
            public List<DiceCardXmlInfo> cards;

            public override List<DiceCardXmlInfo> GetResultCards()
            {
                return cards;
            }
        }

        public class EmotionResult : Result
        {
            public List<EmotionCardXmlInfo> emotions;

            public override List<EmotionCardXmlInfo> GetResultEmotions()
            {
                return emotions;
            }
        }

        public readonly List<EmotionCardXmlInfo> emotions;
        public readonly List<DiceCardXmlInfo> cards;
        public readonly string title;
        public readonly int selectCount;
        public readonly int maxSelectCount;
        public readonly Color color = new Color(1f, 1f, 1f, 1f);
        public readonly Action<Result> onSelect;
        public readonly string artwork;

        private CustomSelectorModel(
            List<EmotionCardXmlInfo> emotions,
            List<DiceCardXmlInfo> cards,
            string title,
            string artwork,
            int selectCount,
            int maxSelectCount,
            Color color,
            Action<Result> onSelect
            )
        {
            this.emotions = emotions;
            this.cards = cards;
            this.title = title;
            this.artwork = artwork;
            this.selectCount = selectCount;
            this.maxSelectCount = maxSelectCount;
            this.color = color;
            this.onSelect = onSelect;
        }


        public class Builder
        {
            private List<EmotionCardXmlInfo> emotions = null;
            private List<DiceCardXmlInfo> cards = null;
            private string artwork = null;
            private string title = "Please Select";
            private int selectCount = 1;
            private int maxSelectCount = -1;
            private Color color;

            public Builder SetArtwork(string artwork)
            {
                this.artwork = artwork;
                return this;
            }

            public Builder SetTitle(string title)
            {
                this.title = title;
                return this;
            }

            public Builder SetEmotions(List<EmotionCardXmlInfo> emotions)
            {
                if (cards != null) cards = null;
                this.emotions = emotions;
                return this;
            }

            public Builder SetCards(List<DiceCardXmlInfo> cards)
            {
                if (emotions != null) emotions = null;
                this.cards = cards;
                return this;
            }

            public Builder SetSelectCount(int selectCount, int maxSelectCount = -1)
            {
                this.selectCount = selectCount;
                this.maxSelectCount = maxSelectCount;
                return this;
            }
            public Builder SetStoryArtwork(string story)
            {
                this.artwork = story;
                return this;
            }

            public Builder SetColor(Color color)
            {
                this.color = color;
                return this;
            }

            public void Show(Action<Result> onSelect)
            {
                LoA.ShowCustomSelector(new CustomSelectorModel(emotions, cards, title, artwork, selectCount, maxSelectCount, color, onSelect));
            }
        }

    }
}
