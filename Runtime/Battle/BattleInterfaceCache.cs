using LibraryOfAngela.CorePage;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace LibraryOfAngela.Battle
{
    class BattleEffectCache
    {
        private BattleUnitModel owner;
        private IEnumerable<ILoABattleEffect> passives;
        private List<ILoABattleEffect> bufs;
        private IEnumerable<ILoABattleEffect> emotions;
        private ILoABattleEffect cardAbility;
        private IEnumerable<ILoABattleEffect> diceAbility;
        private Dictionary<Type, int> stackCnt = new Dictionary<Type, int>();
        private bool isPassiveChanged = true;

        // 최초 생성
        public BattleEffectCache(BattleUnitModel unit)
        {
            owner = unit;
            emotions = unit.emotionDetail
                .PassiveList
                .SelectMany(x => x.AbilityList)
                .Where(emotion =>
                {
                    var card = emotion?._emotionCard;
                    return card?.Owner?.emotionDetail?.PassiveList?.Contains(card) == true;
                })
                .OfType<ILoABattleEffect>();
            passives = unit.passiveDetail._passiveList.OfType<ILoABattleEffect>().ToArray();
        }

        public void OnDie()
        {
            if (owner?.IsDead() == true)
            {
                owner = null;
                passives = null;
                bufs = null;
                emotions = null;
                cardAbility = null;
                diceAbility = null;
                isPassiveChanged = false;
            }
        }

        // 패시브 목록 초기화 시점
        public void OnPassiveCreated(BattleUnitPassiveDetail passive)
        {
            if (isPassiveChanged)
            {
                isPassiveChanged = false;
                passives = passive._passiveList.OfType<ILoABattleEffect>().ToArray();
            }
        }

        // 패시브 목록 변동 여부 트리거 1 (패시브 생성)
        public void AddPassive()
        {
            isPassiveChanged = true;
        }

        // 패시브 목록 변동 여부 트리거 2 (패시브 생성)
        public void DestroyPassive()
        {
            isPassiveChanged = true;
        }

        // 버프 생성 
        public void BufInit(ILoABattleEffect eff)
        {
            if (bufs == null) bufs = new List<ILoABattleEffect>();
            bufs.Add(eff);
        }

        // 버프 소멸
        public void BufDestroyed(BattleUnitBuf buf)
        {
            if (buf is ILoABattleEffect eff && buf._destroyed && bufs != null)
            {
                bufs.Remove(eff);
            }
        }

        // 책장 효과 할당
        public void OnUseCard_before(BattlePlayingCardDataInUnitModel card)
        {
            cardAbility = card.cardAbility as ILoABattleEffect;
        }

        // 책장 효과 할당 해제 (책장이 광역 책장인경우)
        public void OnEndAreaAttack(BattlePlayingCardDataInUnitModel card)
        {
            var range = card.card?.GetSpec()?.Ranged;
            if (range ==  LOR_DiceSystem.CardRange.FarArea || range == LOR_DiceSystem.CardRange.FarAreaEach)
            {
                cardAbility = null;
            }
        }

        // 책장 효과 할당 해제 (책장이 일반 책장인경우)
        public void OnEndBattle()
        {
            cardAbility = null;
        }

        // 주사위 효과 할당
        public void BeforeRollDice(BattleDiceBehavior behaviour)
        {
            diceAbility = behaviour.abilityList.OfType<ILoABattleEffect>();
        }

        // 주사위 효과 할당 해제
        public void AfterAction()
        {
            diceAbility = null;
        }

        public IEnumerable<T> ToEnumerable<T>() where T : ILoABattleEffect
        {
            var key = typeof(T);
            if (!stackCnt.ContainsKey(key)) stackCnt[key] = 0;
            try
            {
                if (++stackCnt[key] >= 5)
                {
                    throw new Exception("LoA StackOverflow!!!!");
                }

                foreach (var effect in Emit<T>(emotions))
                {
                    yield return effect;
                }
                foreach (var effect in Emit<T>(passives))
                {
                    yield return effect;
                }
                foreach (var effect in Emit<T>(bufs))
                {
                    if ((effect as BattleUnitBuf).IsDestroyed() != true)
                    {
                        yield return effect;
                    }
                }

                if (cardAbility is T card) yield return card;

                foreach (var effect in Emit<T>(diceAbility))
                {
                    yield return effect;
                }
            }
            finally
            {
                stackCnt[key]--;
            }
        }

        private IEnumerable<T> Emit<T>(IEnumerable<ILoABattleEffect> effects) where T : ILoABattleEffect
        {
            if (effects == null) yield break;
            foreach (var effect in effects)
            {
                if (effect is T ef) yield return ef;
            }
        }
    }


    class BattleInterfaceCache : Singleton<BattleInterfaceCache>
    {

        private Dictionary<BattleUnitModel, BattleEffectCache> caches = new Dictionary<BattleUnitModel, BattleEffectCache>();

        public void Initialize()
        {
            
        }

        [HarmonyPatch(typeof(BattleObjectManager), "RegisterUnit")]
        [HarmonyPostfix]
        private static void After_RegisterUnit(BattleUnitModel unit)
        {
            if (unit is null) return;
            if (Instance.caches.ContainsKey(unit)) return;
            Instance.caches[unit] = new BattleEffectCache(unit);
            if (BattlePatch.IsWaveStartCalled)
            {
                foreach (var cache in Instance.caches.Values.SelectMany(d => d.ToEnumerable<IAllCharacterBufController>()))
                {
                    var buf = cache.CreateBuf(unit);
                    if (buf != null)
                    {
                        cache.bufs.Add(buf);
                        if (buf._owner is null) unit.bufListDetail.AddBuf(buf);
                    }
                }
                foreach (var e in Instance.caches.Values.SelectMany(d => d.ToEnumerable<IHandleNewCharacter>()))
                {
                    try
                    {
                        e.OnNewCharacterRegister(unit);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }
                }
            }
            AdvancedSkinInfoPatch.Instance.RegisterDialog(unit.UnitData.unitData);
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnDie")]
        [HarmonyPostfix]
        private static void After_OnDie(BattleUnitModel __instance)
        {
            foreach(var controller in Of<IAllCharacterBufController>(__instance))
            {
                controller.bufs.RemoveAll(x =>
                {
                    x.Destroy();
                    return true;
                });
            }
            Instance.caches.SafeGet(__instance)?.OnDie();
        }

        [HarmonyPatch(typeof(BattleObjectManager), "UnregisterUnit")]
        [HarmonyPostfix]
        private static void After_UnregisterUnit(BattleUnitModel unit)
        {
            if (unit is null) return;
            if (BattlePatch.IsWaveStartCalled)
            {
                foreach (var cache in Instance.caches.Values.SelectMany(d => d.ToEnumerable<IAllCharacterBufController>()))
                {
                    cache.bufs.RemoveAll(x =>
                    {
                        if (x._owner == unit)
                        {
                            x.Destroy();
                            return true;
                        }
                        return false;
                    });
                }
            }
            Instance.caches.Remove(unit);
        }

        [HarmonyPatch(typeof(BattleObjectManager), "Clear")]
        [HarmonyPostfix]
        private static void After_Clear()
        {
            foreach(var unit in Instance.caches.Keys.ToList())
            {
                After_UnregisterUnit(unit);
            }
        }

        [HarmonyPatch(typeof(BattleUnitPassiveDetail), "OnCreated")]
        [HarmonyPostfix]
        private static void After_OnCreated(BattleUnitPassiveDetail __instance)
        {
            Instance.caches.SafeGet(__instance?._self)?.OnPassiveCreated(__instance);
        }

        [HarmonyPatch(typeof(BattleUnitPassiveDetail), "AddPassive", new Type[] { typeof(LorId) })]
        [HarmonyPostfix]
        private static void After_AddPassive(BattleUnitPassiveDetail __instance)
        {
            Instance.caches.SafeGet(__instance?._self)?.AddPassive();
        }

        [HarmonyPatch(typeof(BattleUnitPassiveDetail), "AddPassive", new Type[] { typeof(PassiveAbilityBase) })]
        [HarmonyPostfix]
        private static void After_AddPassive_Base(BattleUnitPassiveDetail __instance)
        {
            Instance.caches.SafeGet(__instance?._self)?.AddPassive();
        }

        [HarmonyPatch(typeof(BattleUnitPassiveDetail), "DestroyPassive")]
        [HarmonyPostfix]
        private static void After_DestroyPassive(BattleUnitPassiveDetail __instance)
        {
            Instance.caches.SafeGet(__instance?._self)?.DestroyPassive();
        }

        [HarmonyPatch(typeof(BattleUnitPassiveDetail), "DestroyPassiveAll")]
        [HarmonyPostfix]
        private static void After_DestroyPassiveAll(BattleUnitPassiveDetail __instance)
        {
            Instance.caches.SafeGet(__instance?._self)?.DestroyPassive();
        }

        [HarmonyPatch(typeof(BattleUnitBuf), "Init")]
        [HarmonyPostfix]
        private static void After_BufInit(BattleUnitBuf __instance)
        {
            if (!__instance.IsDestroyed() && __instance is ILoABattleEffect eff)
            {
                var bufDetail = __instance._owner?.bufListDetail;
                if (bufDetail is null) return;

                if (bufDetail._bufList.Contains(__instance) || bufDetail._readyBufList.Contains(__instance) || bufDetail._readyReadyBufList.Contains(__instance))
                {
                    Instance.caches.SafeGet(bufDetail._self)?.BufInit(eff);
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitBuf), "Destroy")]
        [HarmonyPostfix]
        private static void After_BufDestroy(BattleUnitBuf __instance)
        {
            Instance.caches.SafeGet(__instance._owner)?.BufDestroyed(__instance);
        }

        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnUseCard_before")]
        [HarmonyPostfix]
        private static void After_OnUseCard_before(BattlePlayingCardDataInUnitModel __instance)
        {
            Instance.caches.SafeGet(__instance.owner)?.OnUseCard_before(__instance);
        }

        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnEndAreaAttack")]
        [HarmonyPostfix]
        private static void After_OnEndAreaAttack(BattlePlayingCardDataInUnitModel __instance)
        {
            Instance.caches.SafeGet(__instance.owner)?.OnEndAreaAttack(__instance);
        }

        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "OnEndBattle")]
        [HarmonyPostfix]
        private static void After_OnEndBattle(BattlePlayingCardDataInUnitModel __instance)
        {
            foreach(var effect in Of<IEndUseCard>(__instance.owner))
            {
                effect.OnEndBattle(__instance);
            }

            Instance.caches.SafeGet(__instance.owner)?.OnEndBattle();
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), "BeforeRollDice")]
        [HarmonyPostfix]
        private static void After_BeforeRollDice(BattleDiceBehavior __instance)
        {
            Instance.caches.SafeGet(__instance.owner)?.BeforeRollDice(__instance);
        }

        [HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), "AfterAction")]
        [HarmonyPostfix]
        private static void After_AfterAction(BattlePlayingCardDataInUnitModel __instance)
        {
            Instance.caches.SafeGet(__instance.owner)?.AfterAction();
        }

        public static IEnumerable<T> Of<T>(BattleUnitModel owner) where T : ILoABattleEffect
        {
            if (owner == null || !Instance.caches.ContainsKey(owner)) return Array.Empty<T>();
            else
            {
                return Instance.caches[owner].ToEnumerable<T>();
            }
        }

        public static IEnumerable<T> Of<T>(BattleUnitBuf owner) where T : ILoABattleEffect => Of<T>(owner._owner);
    }
}
