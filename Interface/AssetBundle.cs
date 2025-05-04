using LibraryOfAngela.Interface_Internal;
using Sound;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    public class AssetBundleCache
    {
        public readonly string packageId;

        public AssetBundleCache(string packageId)
        {
            this.packageId = packageId;
        }
        public GameObject this[string key] => Find(key, false);

        public T LoadManullay<T>(string key, bool expectedNotLoaded = false) where T : UnityEngine.Object
        {
            return ServiceLocator.Instance.GetInstance<ILoARoot>().LoadAsset<T>(packageId, key, expectedNotLoaded);
        }

        private GameObject Find(string key, bool expectNotLoaded)
        {
            return ServiceLocator.Instance.GetInstance<ILoARoot>().LoadAsset<GameObject>(packageId, key, expectNotLoaded);
        }

        public GameObject Instantiate(string key, Transform parent, float autoDestruct)
        {
            var obj = UnityEngine.Object.Instantiate(Find(key, false), parent);
            if (autoDestruct > 0f)
            {
                obj.gameObject.AddComponent<AutoDestruct>().time = autoDestruct;
            }
            return obj;
        }

        public void InstantiateAsync(string key, Transform parent, Action<GameObject> onComplete)
        {
            var origin = Find(key, true);
            if (origin is null)
            {
                BattleSceneRoot.Instance.StartCoroutine(AsyncCreate(key, parent, onComplete));
            }
            else
            {
                var obj = UnityEngine.Object.Instantiate(origin, parent);
                onComplete(obj);
            }
        }

        private IEnumerator AsyncCreate(string key, Transform parent, Action<GameObject> onComplete)
        {
            int tryCnt = 0;
            bool result = false;
            while (parent != null && ++tryCnt < 100)
            {
                var obj = LoadManullay<GameObject>(key, true);
                if (obj != null)
                {
                    obj = UnityEngine.Object.Instantiate(obj, parent);
                    onComplete(obj);
                    result = true;
                    break;
                }
                for (int i = 0; i< 5; i++) yield return null;
            }
            if (!result && parent != null)
            {
                Debug.Log($"LoA ({packageId}) :: Async Instantiate But Not Found :: {key} ??");
            }
            yield break;
        }
    
        public void PlaySfx(string key)
        {
            SoundEffectManager.Instance.PlayClip(LoadManullay<AudioClip>(key));
        }
    }

    public class AssetBundleInfo
    {
        public string path;

        public AssetBundleType type;
        /// <summary>
        /// 하나의 에셋번들이 두가지 이상의 로딩 조건을 가질때 호출됩니다. <see cref="type"/> 이 선언되어있어도 이 값이 null이 아니라면 이 값을 우선시합니다.
        /// 두가지 이상의 서로 다른 해제 시기를 가진경우, 가장 빠른 로딩 시간을 가진 조건에 따라 로드하며, 가장 늦는 해제 조건을 가진 조건을 따릅니다.
        /// </summary>
        public AssetBundleType[] types;

        public override bool Equals(object obj)
        {
            if (obj is AssetBundleInfo info)
            {
                return path == info.path;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return path.GetHashCode();
        }

        public bool? IsAsync(AssetBundleType targetType)
        {
            if (types != null)
            {
                foreach (var t in types)
                {
                    if (t == targetType)
                    {
                        return t.IsAsync;
                    }
                }
            }
            if (type == targetType)
            {
                return type.IsAsync;
            }
            return null;
        }
       
    }

#pragma warning disable CS0660 // 형식은 == 연산자 또는 != 연산자를 정의하지만 Object.Equals(object o)를 재정의하지 않습니다.
#pragma warning disable CS0661 // 형식은 == 연산자 또는 != 연산자를 정의하지만 Object.Equals(object o)를 재정의하지 않습니다.
    public abstract class AssetBundleType
#pragma warning restore CS0660 // 형식은 == 연산자 또는 != 연산자를 정의하지만 Object.Equals(object o)를 재정의하지 않습니다.
#pragma warning restore CS0661 // 형식은 == 연산자 또는 != 연산자를 정의하지만 Object.Equals(object o)를 재정의하지 않습니다.
    {
        public virtual bool IsAsync { get; set; } = false;
        /// <summary>
        /// 기본적인 에셋번들 로딩 방식입니다.
        /// 
        /// 라오루의 기본 데이터 로딩이 완료되고 타이틀 화면이 노출되는 시점부터 데이터를 불러옵니다.
        /// 
        /// 이 로딩은 비동기로 이루어집니다.
        /// </summary>
        public readonly static AssetBundleType Default = new DefaultType();

        public static bool operator ==(AssetBundleType a, AssetBundleType b)
        {
            if (a is null)
            {
                return b is null;
            }

            return a?.Equals(b) == true;
        }

        public static bool operator !=(AssetBundleType a, AssetBundleType b)
        {
            if (a is null)
            {
                return !(b is null);
            }
            return a?.Equals(b) != true;
        }

        /// <summary>
        /// 캐릭터의 SD 를 로딩할때 사용합니다.
        /// 
        /// 해당 캐릭터가 렌더링되는 시점에 데이터를 불러옵니다.
        /// 
        /// 해당 SD 가 특정 접대에서만 보여지는경우 이 타입을 사용하지말고 <see cref="Invitation"/> 쪽을 사용해주세요.
        /// <see cref="isOnlyBattle">해당 번들이 접대중일때만 사용되는지 여부를 반환합니다.</see>
        /// </summary>
        public class Sd : AssetBundleType
        {
            public string skin;
            public bool isOnlyBattle;

            public Sd(string skin)
            {
                this.skin = skin;
            }

            public override bool Equals(object obj)
            {
                return obj is Sd s && s.skin == skin && s.isOnlyBattle == isOnlyBattle;
            }

            public override int GetHashCode()
            {
                return skin.GetHashCode() + (isOnlyBattle.GetHashCode() * 100000);
            }
        }

        private class DefaultType : AssetBundleType
        {
            public override bool Equals(object obj)
            {
                return obj is DefaultType;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        /// <summary>
        /// 접대 내부에서 사용되는 에셋들을 사용할때 사용합니다. 해당 접대의 접대 시작이 눌리는 순간 데이터를 불러옵니다.
        /// 
        /// async 값을 주입해 해당 데이터를 비동기로 불러올지 여부를 제어할 수 있습니다.
        /// </summary>
        public class Invitation : AssetBundleType
        {
            public readonly LorId id;
            

            public Invitation(LorId id)
            {
                this.id = id;
            }

            public Invitation(string packageId, int id)
            {
                this.id = new LorId(packageId, id);
            }

            public override bool Equals(object obj)
            {
                return obj is Invitation i && i.id == id;
            }

            public override int GetHashCode()
            {
                return id.GetHashCode() + -100000;
            }

            public override string ToString()
            {
                return "AssetBundleInfo(Invitation):" + id.ToString();
            }
        }
    
        /// <summary>
        /// 특정 핵심 책장에 대한 패시브 등에 에셋이 필요할때 사용합니다. 해당 핵심 책장을 장착한 캐릭터가 접대에 참여할때 데이터를 불러옵니다.
        /// </summary>
        public class CorePage : AssetBundleType
        {
            public LorId id;
 
            public CorePage(LorId id)
            {
                this.id = id;
            }

            public CorePage(string packageId, int id) : this(new LorId(packageId, id))
            {

            }

            public override bool Equals(object obj)
            {
                if (obj is CorePage e)
                {
                    var result =  e.id == id;
                    return result;
                }

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return id.GetHashCode() + 100000;
            }
            public override string ToString()
            {
                return "AssetBundleInfo(CorePage):" + id.ToString();
            }
        }

        /// <summary>
        /// 특정 에고의 사용 연출에 에셋이 필요할때 사용합니다. 해당 에고 페이지를 획득할때 데이터를 불러옵니다.
        /// </summary>
        public class Ego : AssetBundleType
        {
            public LorId id;

            public Ego(LorId id)
            {
                this.id = id;
            }

            public Ego(string packageId, int id) : this(new LorId(packageId, id))
            {

            }

            public override bool Equals(object obj)
            {
                if (obj is Ego e) return e.id == id;

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return id.GetHashCode();
            }

            public override string ToString()
            {
                return "AssetBundleInfo(Ego):" + id.ToString();
            }
        }
    
        /// <summary>
        /// 특정 배틀 페이지가 연출로서 vfx가 필요할때 사용합니다. 접대 시작시에 데이터를 불러옵니다.
        /// </summary>
        public class BattlePage : AssetBundleType
        {
            public LorId id;

            public BattlePage(LorId id)
            {
                this.id = id;
            }

            public BattlePage(string packageId, int id) : this(new LorId(packageId, id))
            {

            }

            public override bool Equals(object obj)
            {
                if (obj is BattlePage e) return e.id == id;

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return id.GetHashCode();
            }

            public override string ToString()
            {
                return "AssetBundleInfo(BattlePage):" + id.ToString();
            }
        }
    }


}
