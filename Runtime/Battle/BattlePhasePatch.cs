using HarmonyLib;
using LibraryOfAngela.BattleUI;
using LibraryOfAngela.Interface_External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Battle
{
    class BattlePhasePatch
    {

        public static void EnqueueReservePassive(BattleUnitPassiveDetail owner, LorId id)
        {
            reservePassiveModels.Enqueue(new ReservePassiveModel
            {
                owner = owner,
                id = id
            });
        }

        public static bool IsWaveStartCalled { get; set; }

        [HarmonyPatch(typeof(BattleUnitModel), "OnWaveStart")]
        [HarmonyPrefix]
        private static void Before_OnWaveStart(BattleUnitModel __instance)
        {
            try
            {
                __instance.allyCardDetail.GetAllDeck().ForEach(x =>
                {
                    if (!x.exhaust && x._script is IDiceCardSelfAbilityOnWaveStart ability)
                    {
                        ability.OnWaveStart(__instance, x);
                    }
                });
                foreach (var controller in BattleInterfaceCache.Of<IAllCharacterBufController>(__instance))
                {
                    BattlePatch.InjectAllControllerBuf(controller);
                }
                foreach (var controller in BattleInterfaceCache.Of<IHandleNewCharacter>(__instance))
                {
                    foreach (var unit in BattleObjectManager.instance.GetAliveList())
                    {
                        controller.OnNewCharacterRegister(unit);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            finally
            {
                IsWaveStartCalled = true;
            }
        }

        [HarmonyPatch(typeof(StageController), "set_phase")]
        [HarmonyPrefix]
        private static void Before_set_phase(ref StageController.StagePhase value)
        {
            if (!isForceHandlePhase)
            {
                var f = HandlePhase(value);
                value = f;
            }
        }

        [HarmonyPatch(typeof(StageController), "set_phase")]
        [HarmonyPostfix]
        private static void After_set_phase(StageController.StagePhase value)
        {
            CallPhaseCallbacks(value);
            expectedPhase = value;
            isForceHandlePhase = false;
        }

        private static StageController.StagePhase expectedPhase;
        [HarmonyPatch(typeof(StageController), nameof(StageController.OnFixedUpdate))]
        [HarmonyPrefix]
        private static void Before_OnFixedUpdate(StageController __instance)
        {
            // 뒤돌프모드는 프로퍼티 설정 안하고 독자적으로 필드를 바로 할당시킴. 재할당 요청
            // 바닐라도 타이밍에 따라 필드를 부르는 경우가 있음
            // 초기 할당은 어긋날수 있으므로 드로우 이후로 판정
            if (IsWaveStartCalled && __instance._phase != expectedPhase && __instance._phase > StageController.StagePhase.DrawCardPhase)
            {
                var nextPhase = HandlePhase(__instance._phase);
                if (nextPhase != __instance._phase)
                {
                    isForceHandlePhase = true;
                    __instance.phase = nextPhase;
                }
                else
                {
                    expectedPhase = nextPhase;
                    CallPhaseCallbacks(__instance.phase);
                }
            }
        }

        private static StageController.StagePhase HandlePhase(StageController.StagePhase value)
        {
            try
            {
                if (LoAFramework.DEBUG) Logger.Log($"Phase Detect :: {value}");
                foreach (var x in BattleObjectManager.instance.GetAliveList(false).SelectMany(x => BattleInterfaceCache.Of<IHandleCustomPhase>(x)))
                {
                    var after = x.HandleCustomPhase(value);
                    if (after != value)
                    {
                        if (LoAFramework.DEBUG)
                        {
                            var builder = new StringBuilder("Phase Change Detect ::\n");
                            builder.AppendLine($"- Current : {value}");
                            builder.AppendLine($"- Next : {after} (From {x.GetType().FullName})");
                            Logger.Log(builder.ToString());
                        }
                        value = after;
                        break;
                    }
                }
                bool flag = false;
                foreach (var x in reservePassiveModels)
                {
                    flag = true;
                    var p = x.owner.AddPassive(x.id);
                    x.owner._readyPassiveList.Remove(p);
                    x.owner._passiveList.Add(p);
                    p.OnCreated();
                    if (value == StageController.StagePhase.RoundStartPhase_UI) p.OnWaveStart();
                    else if (value == StageController.StagePhase.RoundStartPhase_System) p.OnRoundStart();
                }
                if (flag) reservePassiveModels.Clear();
                if (value == StageController.StagePhase.EndBattle)
                {
                    IsWaveStartCalled = false;
                    CustomCardEffect.Instance.ClearThisRoundCardEffects();
                }
            }
            catch (Exception e)
            {
                Logger.Log("OnError in CustomHandlePahse");
                Logger.LogError(e);
            }
            return value;
        }

        private static void CallPhaseCallbacks(StageController.StagePhase value)
        {
            if (nextPhaseExists)
            {
                foreach (var action in nextPhaseExecuteActionQueue)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
                nextPhaseExecuteActionQueue.Clear();
                nextPhaseExists = false;
            }

            if (phaseCallbackExists && phaseCallbacks.ContainsKey(value))
            {
                var callbacks = phaseCallbacks[value];
                bool removeExists = false;

                foreach (var action in callbacks)
                {
                    try
                    {
                        action.callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                    removeExists = removeExists || action.onlyOnce;
                }

                if (removeExists)
                {
                    callbacks.RemoveAll(x => x.onlyOnce);
                    if (callbacks.Count == 0)
                    {
                        phaseCallbacks.Remove(value);
                        phaseCallbackExists = phaseCallbacks.Count > 0;
                    }
                }
            }
        }


        private static Queue<Action> nextPhaseExecuteActionQueue = new Queue<Action>();

        private static Dictionary<StageController.StagePhase, List<PhaseListener>> phaseCallbacks = new Dictionary<StageController.StagePhase, List<PhaseListener>>();
        private static Queue<ReservePassiveModel> reservePassiveModels = new Queue<ReservePassiveModel>();

        private static bool nextPhaseExists = false;
        private static bool phaseCallbackExists = false;
        private static bool isForceHandlePhase = false;
        

        public static void AddPhaseCallback(StageController.StagePhase? phase, Action callback, bool onlyOnce)
        {
            if (phase is null)
            {
                nextPhaseExecuteActionQueue.Enqueue(callback);
                nextPhaseExists = true;
            }
            else
            {
                var p = phase.Value;
                if (!phaseCallbacks.ContainsKey(p)) phaseCallbacks[p] = new List<PhaseListener>();
                phaseCallbacks[p].Add(new PhaseListener { callback = callback, onlyOnce = onlyOnce });
                phaseCallbackExists = true;
            }
        }

        public static void ClearResource()
        {
            if (IsWaveStartCalled)
            {
                IsWaveStartCalled = false;
                CustomCardEffect.Instance.ClearThisRoundCardEffects();
            }
            if (nextPhaseExists)
            {
                nextPhaseExecuteActionQueue.Clear();
                nextPhaseExists = false;
            }
            if (phaseCallbackExists)
            {
                foreach (var p in phaseCallbacks)
                {
                    p.Value.Clear();
                }
                phaseCallbacks.Clear();

                phaseCallbackExists = false;
            }
        }

        private struct PhaseListener
        {
            public Action callback;
            public bool onlyOnce;
        }
    }
}
