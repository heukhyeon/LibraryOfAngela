using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Extension
{
    /// <summary>
    /// LoAFramework, 일반 모드들 가리지 않고 공통적으로 사용하는 확장 함수의 모음입니다.
    /// </summary>
    public static class CommonExtension
    {
        public static BindingFlags allFlag
        {
            get
            {
                return BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;
            }
        }

        /// <summary>
        /// 객체의 필드나 프로퍼티를 리플렉션을 사용해 획득합니다.
        /// </summary>
        /// <typeparam name="T">반환될 객체 타입</typeparam>
        /// <param name="owner">객체를 소유한 객체, null 인경우 static 필드를 조회합니다.</param>
        /// <param name="fieldName">얻고자 하는 객체의 필드 이름.</param>
        /// <param name="mustNotNull">필드가 존재하고, 객체가 null일때 에러를 반환할지 여부입니다.</param>
        /// <returns></returns>
        public static T GetFieldValue<T>(this object owner, string fieldName, bool mustNotNull = false)
        {
            Type type;
            bool isStatic = false;
            if (owner is Type t)
            {
                type = t;
                isStatic = true;
            }
            else
            {
                type = owner.GetType();
            }
            var field = type.GetField(fieldName, allFlag);
            if (field != null)
            {
                var value = isStatic ? field.GetValue(null) : field.GetValue(owner);
                if (mustNotNull && value == null) throw new Exception($"From GetFieldValue : {type.Name}.${fieldName} is Null");
                return (T)value;
            }
            else
            {
                var property = type.GetProperty(fieldName, allFlag);
                if (property == null)
                {
                    throw new Exception($"From GetFieldValue : Field And Property is Not Exist, Please Check Type or Name : {type.Name} / {fieldName}");
                }
                var value = isStatic ? property.GetValue(null) : property.GetValue(owner);
                if (mustNotNull && value == null) throw new Exception($"From GetFieldValue : {type.Name}.${fieldName} is Null");
                return (T)value;
            }
        }

        /// <summary>
        /// 객체의 필드나 프로퍼티를 리플렉션을 사용해 할당합니다.
        /// </summary>
        /// <param name="owner"> 객체를 소유한 객체</param>
        /// <param name="fieldName">할당하고자 하는 객체의 필드 이름</param>
        /// <param name="value">할당할 값</param>
        public static void SetFieldValue(this object owner, string fieldName, object value)
        {
            var field = owner.GetType().GetField(fieldName, allFlag);
            if (field != null)
            {
                field.SetValue(owner, value);
            }
            else
            {
                var property = owner.GetType().GetProperty(fieldName, allFlag);
                property?.SetValue(owner, value);
            }
        }

        /// <summary>
        /// Dictionary 에 키가 없는경우 에러를 발행하지 않고 대신 null을 반환하게 하는 래핑 함수입니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="owner"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T SafeGet<T, R>(this Dictionary<R, T> owner, R key) where T : class
        {
            if (key == null || owner == null) return null;

            T output;
            if (owner.TryGetValue(key, out output)) return output;
            else return null;
        }

        public static void TypeForEach<T>(this IEnumerable owner, Action<T> action)
        {
            foreach(var obj in owner)
            {
                try
                {
                    if (obj is T b)
                    {
                        action(b);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public static T FindType<T>(this IEnumerable owner) where T : class
        {
            foreach (var obj in owner)
            {
                if (obj is T b)
                {
                    return b;
                }
            }
            return null;
        }

        public static void ReservePassive(this BattleUnitPassiveDetail owner, LorId id)
        {
            if (owner == null) return;
            ServiceLocator.Instance.GetInstance<ILoARoot>().ReservePassive(owner, id);
        }

        public static void UpdatePhase(this StageController owner, StageController.StagePhase phase)
        {
            owner.phase = phase;
        }

        public static int SpeedDiff(this BattleDiceBehavior behaviour)
        {
            var owner = behaviour.card.owner;
            var mySpeed = owner.speedDiceResult[behaviour.card.slotOrder].value;
            var targetSpeed = behaviour.card.target.speedDiceResult[behaviour.card.targetSlotOrder].value;
            return mySpeed - targetSpeed;
        }

        public static bool TryDraw(this BattleAllyCardDetail owner, int count)
        {
            var handCount = owner.GetHand().Count;
            owner.DrawCards(count);
            return handCount != owner.GetHand().Count;
        }

        private static void ActionExceptOwner(BattleUnitModel owner, Action<BattleUnitModel> action)
        {
            BattleObjectManager.instance.GetAliveList(owner.faction).ForEach(x =>
            {
                if (x != owner)
                {
                    try
                    {
                        action(x);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            });
        }

        public static Vector3 Copy(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }

        public static void ActionExceptOwner(this BattleUnitBuf buf, Action<BattleUnitModel> action) => ActionExceptOwner(buf.GetFieldValue<BattleUnitModel>("_owner"), action);

        public static void ActionExceptOwner(this PassiveAbilityBase buf, Action<BattleUnitModel> action) => ActionExceptOwner(buf.Owner, action);

        public static void ActionExceptOwner(this DiceCardSelfAbilityBase buf, Action<BattleUnitModel> action) => ActionExceptOwner(buf.owner, action);
    
        public static T RandomOne<T>(this IEnumerable<T> owner, Predicate<T> condition = null)
        {
            var ret = owner.OrderBy(x =>
            {
                if (condition?.Invoke(x) == false) return 10000;
                return RandomUtil.Range(1, 20);
            }).FirstOrDefault();

            if (condition?.Invoke(ret) == false) return default(T);
            return ret;
        }

        public static R First<T, R>(this IEnumerable<T> owner, Predicate<R> condition = null) where R : T
        {
            foreach(var item in owner)
            {
                if (item is R t && condition?.Invoke(t) != false)
                {
                    return t;
                }
            }
            return default(R);
        }

        public static IEnumerable<T> RandomTake<T>(this IEnumerable<T> owner, int count, Predicate<T> condition = null)
        {
            return owner.Where(x => condition?.Invoke(x) != false).OrderBy(x => RandomUtil.Range(1, 5)).Take(count);
        }
    }
}
