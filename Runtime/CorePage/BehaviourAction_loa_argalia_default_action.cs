using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.CorePage
{
    class BehaviourAction_loa_argalia_default_action : BehaviourActionBase
    {
        public override List<RencounterManager.MovingAction> GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        {
			var type = self.data.actionDetail;
            // Guard, Evasion
            int ordinal = (int)type;
            if (ordinal < 4) return base.GetMovingAction(ref self, ref opponent);
			// Fire ~ Special
			if (ordinal >= 10) type = (ActionDetail)RandomUtil.Range(4, 6);

			switch(type)
            {
				case ActionDetail.Slash:
					return ArgaliaUpper(ref self, ref opponent);
				case ActionDetail.Penetrate:
					return ArgaliaMove(ref self, ref opponent);
				case ActionDetail.Hit:
					return ArgaliaGaroSlash(ref self, ref opponent);
				default:
					return base.GetMovingAction(ref self, ref opponent);

			}
        }

        // Token: 0x06001141 RID: 4417 RVA: 0x0008BEA4 File Offset: 0x0008A0A4
        private List<RencounterManager.MovingAction> ArgaliaMove(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
		{
			List<RencounterManager.MovingAction> list = new List<RencounterManager.MovingAction>();
			bool flag = false;
			if (opponent.behaviourResultData != null)
			{
				flag = opponent.behaviourResultData.IsFarAtk();
			}
			if (self.result == Result.Win && self.data.actionType == ActionType.Atk && !flag)
			{
				RencounterManager.MovingAction movingAction = new RencounterManager.MovingAction(ActionDetail.S1, CharMoveState.MoveForward, 30f, false, 0.4f, 1f);
				movingAction.customEffectRes = "FX_Mon_Argalia_Flash";
				movingAction.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
				new RencounterManager.MovingAction(ActionDetail.S1, CharMoveState.Stop, 0f, true, 0.4f, 1f).SetEffectTiming(EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT);
				list.Add(movingAction);
				opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Stop, 1f, false, 0.1f, 1f));
			}
			else
			{
				list = base.GetMovingAction(ref self, ref opponent);
			}
			return list;
		}

		// Token: 0x06001142 RID: 4418 RVA: 0x0008BF6C File Offset: 0x0008A16C
		private List<RencounterManager.MovingAction> ArgaliaGaroSlash(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
		{
			List<RencounterManager.MovingAction> list = new List<RencounterManager.MovingAction>();
			bool flag = false;
			if (opponent.behaviourResultData != null)
			{
				flag = opponent.behaviourResultData.IsFarAtk();
			}
			if (self.result == Result.Win && self.data.actionType == ActionType.Atk && !flag)
			{
				RencounterManager.MovingAction movingAction = new RencounterManager.MovingAction(ActionDetail.S1, CharMoveState.Stop, 0f, true, 0.125f, 1f);
				movingAction.customEffectRes = "FX_Mon_Argalia_Slash_H";
				movingAction.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
				new RencounterManager.MovingAction(ActionDetail.S1, CharMoveState.Stop, 0f, true, 0.125f, 1f).SetEffectTiming(EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT);
				list.Add(movingAction);
				opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Knockback, 1f, true, 0.125f, 1f));
			}
			else
			{
				list = base.GetMovingAction(ref self, ref opponent);
			}
			return list;
		}

		// Token: 0x06001143 RID: 4419 RVA: 0x0008C034 File Offset: 0x0008A234
		private List<RencounterManager.MovingAction> ArgaliaUpper(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
		{
			List<RencounterManager.MovingAction> list = new List<RencounterManager.MovingAction>();
			bool flag = false;
			if (opponent.behaviourResultData != null)
			{
				flag = opponent.behaviourResultData.IsFarAtk();
			}
			if (self.result == Result.Win && self.data.actionType == ActionType.Atk && !flag)
			{
				RencounterManager.MovingAction movingAction = new RencounterManager.MovingAction(ActionDetail.Slash, CharMoveState.Stop, 0f, true, 0.125f, 1f);
				movingAction.customEffectRes = "FX_Mon_Argalia_Slash_Up";
				movingAction.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
				new RencounterManager.MovingAction(ActionDetail.Slash, CharMoveState.Stop, 0f, true, 0.125f, 1f).SetEffectTiming(EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT);
				list.Add(movingAction);
				opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Knockback, 1f, true, 0.125f, 1f));
			}
			else
			{
				list = base.GetMovingAction(ref self, ref opponent);
			}
			return list;
		}

		// Token: 0x06001144 RID: 4420 RVA: 0x0008C0FC File Offset: 0x0008A2FC
		private List<RencounterManager.MovingAction> Func4(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
		{
			List<RencounterManager.MovingAction> list = new List<RencounterManager.MovingAction>();
			bool flag = false;
			if (opponent.behaviourResultData != null)
			{
				flag = opponent.behaviourResultData.IsFarAtk();
			}
			if (self.result == Result.Win && self.data.actionType == ActionType.Atk && !flag)
			{
				RencounterManager.MovingAction movingAction = new RencounterManager.MovingAction(ActionDetail.S3, CharMoveState.Stop, 0f, false, 0.125f, 1f);
				movingAction.customEffectRes = "FX_Mon_Argalia_Slash_Down";
				movingAction.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
				new RencounterManager.MovingAction(ActionDetail.S3, CharMoveState.Stop, 0f, true, 0.125f, 1f).SetEffectTiming(EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT);
				list.Add(movingAction);
				opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Knockback, 1f, true, 0.125f, 1f));
			}
			else
			{
				list = base.GetMovingAction(ref self, ref opponent);
			}
			return list;
		}
	}
}
