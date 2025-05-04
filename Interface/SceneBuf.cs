using LibraryOfAngela.Interface_External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    public abstract class SceneBuf : IHandleDiceStatBonus
    {
        public abstract string keywordId { get; }

        public virtual string keywordIconId
        {
            get
            {
                return keywordId;
            }
        }

        public string bufActivatedText
        {
            get
            {
                return currentDescription;
            }
        }

        public virtual float iconScale => 1.5f;

        public BattleUnitInformationUI_BuffSlot slot { get; set; }

        private string currentDescription;

        public Action onDescriptionChanged;

        public List<BattleUnitModel> Allys
        {
            get => BattleObjectManager.instance.GetAliveList(Faction.Player);
        }

        public List<BattleUnitModel> Enemys
        {
            get => BattleObjectManager.instance.GetAliveList(Faction.Enemy);
        }

        public virtual DiceStatBonus ConvertDiceStatBonus(BattleDiceBehavior behaviour, DiceStatBonus origin)
        {
            return origin;
        }

        public virtual void Init()
        {
            currentDescription = BattleEffectTextsXmlList.Instance.GetEffectTextDesc(keywordId);
        }

        public virtual void OnRoundStart()
        {

        }

        public virtual int GetDamageReduction(BattleUnitModel target)
        {
            return 0;
        }

        public virtual bool PreventBufDestroy(BattleUnitBuf buf)
        {
            return false;
        }

        public virtual void OnTakeDamageByAttack(BattleUnitModel target, BattleDiceBehavior atkDice, int dmg)
        {

        }

        public virtual void OnRoundEnd()
        {

        }

        public virtual void OnRoundEndTheLast()
        {

        }
    
        public void UpdateDescription(string desc)
        {
            currentDescription = desc;
            onDescriptionChanged?.Invoke();
        }
    }

    /// <summary>
    /// <see cref="EnemyTeamStageManager"/> 에 구현시 현재 무대 전체에 적용하는 버프를 적용한다.
    /// </summary>
    public interface ISceneBufProvider
    {
        SceneBuf CurrentScaneBuf { get; }
    }
}
