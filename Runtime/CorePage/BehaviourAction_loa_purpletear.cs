using Battle.DiceAttackEffect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.CorePage
{
    class BehaviourAction_loa_purpletear : BehaviourActionBase
    {
        public override List<RencounterManager.MovingAction> GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        {

            var result = base.GetMovingAction(ref self, ref opponent);
            if (self.result != Result.Win) return result;
            foreach(var x in result)
            {
                if (x.actionDetail == ActionDetail.Hit)
                {
                    x.customEffectRes = "loa_purpletear_H";
                    x.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
                    break;
                }
                else if (x.actionDetail == ActionDetail.Penetrate)
                {
                    x.customEffectRes = "loa_purpletear_Z";
                    x.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
                    break;
                }
                else if (x.actionDetail == ActionDetail.Slash)
                {
                    x.customEffectRes = "loa_purpletear_J";
                    x.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
                    break;
                }
            }

            return result;
        }
	}

    class DiceAttackEffect_loa_purpletear_H : DiceAttackEffect
    {
        public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
        {
            base.Initialize(self, target, destroyTime);
            var obj = global::Util.LoadPrefab("Battle/DiceAttackEffects/New/FX/Mon/PurpleTear/FX_Mon_PurpleTear_H", self.charAppearance.atkEffectRoot);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            GameObject gameObject = global::Util.LoadPrefab("Battle/DiceAttackEffects/New/FX/Mon/PurpleTear/FX_Mon_PurpleTear_UnATK_H", target.charAppearance.atkEffectRoot);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-30f, 30f));
            gameObject.transform.localScale = Vector3.one;
        }
    }

    class DiceAttackEffect_loa_purpletear_Z : DiceAttackEffect
    {
        public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
        {
            base.Initialize(self, target, destroyTime);
            var obj = global::Util.LoadPrefab("Battle/DiceAttackEffects/New/FX/Mon/PurpleTear/FX_Mon_PurpleTear_Z", self.charAppearance.atkEffectRoot);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            GameObject gameObject = global::Util.LoadPrefab("Battle/DiceAttackEffects/New/FX/Mon/PurpleTear/FX_Mon_PurpleTear_UnATK_Z", target.charAppearance.atkEffectRoot);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-5f, 5f));
            gameObject.transform.localScale = Vector3.one;
        }
    }

    class DiceAttackEffect_loa_purpletear_J : DiceAttackEffect
    {
        public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
        {
            base.Initialize(self, target, destroyTime);
            var obj = global::Util.LoadPrefab("Battle/DiceAttackEffects/New/FX/Mon/PurpleTear/FX_Mon_PurpleTear_J", self.charAppearance.atkEffectRoot);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            GameObject gameObject = global::Util.LoadPrefab("Battle/DiceAttackEffects/New/FX/Mon/PurpleTear/FX_Mon_PurpleTear_UnATK_J", target.charAppearance.atkEffectRoot);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-360f, 360f));
            gameObject.transform.localScale = Vector3.one;
        }
    }
}
