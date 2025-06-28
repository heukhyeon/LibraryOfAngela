using LibraryOfAngela.Battle;
using LibraryOfAngela.Interface_Internal;

namespace LibraryOfAngela
{
    class LoAInternalImpl : Singleton<LoAInternalImpl>, ILoAInternal
    {
        void ILoAInternal.RegisterLimbusParryingWinDice(LimbusDiceAbility ability)
        {
            BattleResultPatch.RegisterLibmusDice(ability);
        }
    }
}
