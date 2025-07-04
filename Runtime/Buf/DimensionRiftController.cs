using LibraryOfAngela.Battle;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace LibraryOfAngela.Buf
{
    class DimensionRiftControllerImpl : DimensionRiftController, BufControllerImpl
    {
        public string keywordId => "LoADimensionRift";

        public string keywordIconId => "loa_dimension_rift_icon";

        public BufPositiveType positiveType => BufPositiveType.Negative;

        public int GetParamInBufDesc(BattleUnitBuf_loaDimensionRift buf)
        {
            return buf.stack * 3;
        }

        public void OnRoundEndDimensionRift(BattleUnitBuf_loaDimensionRift buf) {
            EffectDimensionRift(buf);
            buf._owner.bufListDetail.AddKeywordBufByEtc(LoAKeywordBuf.Rupture, buf.stack * 3, buf._owner);
            buf.Destroy();
        }

        private void EffectDimensionRift(BattleUnitBuf_loaDimensionRift buf)
        {
            var b = BufAssetLoader.LoadObject("loa_debuff_dimensionrift", buf._owner.view.atkEffectRoot, 2f);
            b.transform.localPosition = Vector3.zero;
            b.transform.localScale = Vector3.one * 0.3f;
        }

        public void OnTakeRuptureReduceStack(BattleUnitModel actor, BattleUnitBuf_loaRupture rupture, BattleUnitBuf_loaDimensionRift buf, ref int value, int originValue) {
            if (buf.stack > 0) {
                value = 0;
                if (--buf.stack <= 0) {
                    buf.Destroy();
                } else {
                    buf.OnAddBuf(-1);
                }
            }
        }

        public string GetBufActivatedText()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    return "因受击导致减少“破裂”层数时不会减少“破裂”层数而是改为减少1层“次元裂缝”。\n每一幕结束时在下一幕对自身施加{0}层“破裂”并移除本效果。";
                case "en":
                    return "When hit, instead of reducing Rupture stacks, lose 1 stack of this status.\nAt the end of the Scene, gain {0} Rupture next Scene and lose this status.";
                default:
                    return "피격시 파열 수치가 감소하는 대신 차원 균열 수치가 1 감소한다.\n막 종료시 다음 막에 파열 {0}을 얻고 소멸";
            }
        }

        public string GetBufName()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "kr":
                    return "차원 균열";
                case "cn":
                case "trcn":
                    return "次元裂缝";
                default:
                    return "Dimension Rift";
            }
        }

        public string GetKeywordText()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    return "因受击导致减少“破裂”层数时不会减少“破裂”层数而是改为减少1层“次元裂缝”。\n每一幕结束时在下一幕对自身施加X层“破裂”并移除本效果。";
                case "en":
                    return "When hit, instead of reducing Rupture stacks, lose 1 stack of this status.\nAt the end of the Scene, gain Rupture next Scene and lose this status";
                default:
                    return "한 막 동안 피격시 파열 수치의 감소를 막고 대신 수치가 1 감소하는 효과";
            }
        }

        public void AddAdditionalKeywordDesc()
        {

        }
    }
}