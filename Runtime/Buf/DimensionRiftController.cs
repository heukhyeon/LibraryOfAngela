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

        public string keywordIconId => "loa_dimensionrift_icon";

        public void OnRoundEndDimensionRift(BattleUnitBuf_loaDimensionRift buf) {
            buf._owner.bufListDetail.AddKeywordBufByEtc(LoAKeywordBuf.Rupture, buf.stack * 5, buf._owner);
            buf.Destroy();
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
                default:
                    return "Dimension Rift";
            }
        }
    }
}