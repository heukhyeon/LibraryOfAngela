using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace HarmonyLib {
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type t,string n) {} }
    public class HarmonyPostfix : Attribute {}
    public class HarmonyPrefix : Attribute {}
}
namespace UI { public enum UIPhase { Sephirah, Battle } public class UIControlManager {} public class UIController {} }
public class Singleton<T> where T:new() { public static T Instance = new T(); }
public class LorId {
    public string packageId; public int id; public LorId(string p,int i) {packageId=p;id=i;}
    public bool IsWorkshop()=>!string.IsNullOrEmpty(packageId); public bool IsBasic()=>!IsWorkshop();
    public override bool Equals(object o)=>o is LorId l && l.id==id && l.packageId==packageId;
    public override int GetHashCode()=>id ^ (packageId??"").GetHashCode();
    public static bool operator ==(LorId a,LorId b)=>Equals(a,b);
    public static bool operator !=(LorId a,LorId b)=>!Equals(a,b);
}
public class ClassInfo { public LorId id = new LorId("heukhyeon.LCAngela.R",6661); }
public class StageModel { public ClassInfo ClassInfo = new ClassInfo(); }
public class WaveModel { public List<UnitBattleDataModel> UnitList=new List<UnitBattleDataModel>(); }
public class FloorModel { public List<UnitBattleDataModel> GetUnitAddedBattleDataList()=>new List<UnitBattleDataModel>(); }
public class UnitBattleDataModel { public UnitData unitData=new UnitData(); }
public class UnitData { public Book bookItem=new Book(); } public class Book { public LorId BookId=new LorId("",1); }
public class StageController:Singleton<StageController> {
    public StageModel stage=new StageModel(); public WaveModel wave=new WaveModel();
    public StageModel GetStageModel()=>stage; public WaveModel GetCurrentWaveModel()=>wave;
    public FloorModel GetCurrentStageFloorModel()=>new FloorModel();
}
public class GlobalGameManager {}
public class BattleSceneRoot:Singleton<BattleSceneRoot> { public UnityEngine.GameObject gameObject=new UnityEngine.GameObject(); public int starts; public void StartBattle(){starts++;} public void StartCoroutine(IEnumerator e){} }
public class GameSceneManager:Singleton<GameSceneManager> { public BattleSceneRoot battleScene=BattleSceneRoot.Instance; }
namespace UnityEngine {
    public class Object { public static GameObject Instantiate(GameObject o,Transform t)=>o; }
    public class Transform:Object {} public class AudioClip:Object {}
    public class GameObject:Object { public GameObject gameObject=>this; public T AddComponent<T>() where T:new()=>new T(); public void SetActive(bool b){} }
    public class Debug {public static void Log(string s){} }
    public class AssetBundleCreateRequest {
        public bool isDone; public float progress; public bool allowSceneActivation; public AssetBundle assetBundle;
        public void Complete(bool fail=false){assetBundle=fail?null:new AssetBundle();isDone=true;progress=1;}
    }
    public class AssetBundle:Object {
        // Match Unity's destroyed-object null comparison, not managed reference null.
        public static bool operator ==(AssetBundle a,AssetBundle b){
            bool an=ReferenceEquals(a,null)||a.unloaded, bn=ReferenceEquals(b,null)||b.unloaded;
            return an&&bn || !an&&!bn&&ReferenceEquals(a,b);
        }
        public static bool operator !=(AssetBundle a,AssetBundle b)=>!(a==b);
        public override bool Equals(object o)=>ReferenceEquals(this,o);
        public override int GetHashCode()=>base.GetHashCode();
        public static int loads; public static bool failSync; public static bool completeAsyncImmediately; public bool unloaded;
        public static List<AssetBundleCreateRequest> requests=new List<AssetBundleCreateRequest>();
        public static AssetBundle LoadFromFile(string p){loads++;return failSync?null:new AssetBundle();}
        public static AssetBundleCreateRequest LoadFromFileAsync(string p){loads++;var r=new AssetBundleCreateRequest();requests.Add(r);if(completeAsyncImmediately)r.Complete();return r;}
        public void Unload(bool all){unloaded=true;}
        public string[] GetAllAssetNames()=>new[]{"assets/lobgelare_carmen_moveheaven.prefab"};
        public T LoadAsset<T>(string n) where T:Object=>unloaded?null:(T)(Object)new GameObject();
    }
}
namespace Sound { public class SoundEffectManager:Singleton<SoundEffectManager>{public void PlayClip(UnityEngine.AudioClip c){} } }
namespace LibraryOfAngela.Interface_Internal {
    public interface ILoARoot { T LoadAsset<T>(string p,string k,bool b) where T:UnityEngine.Object; }
    public class ServiceLocator:Singleton<ServiceLocator>{public T GetInstance<T>()=>default(T);}
}
namespace LibraryOfAngela.EquipBook {} namespace LibraryOfAngela.Implement {} namespace LibraryOfAngela.Util {}
namespace LibraryOfAngela.Extension {
    public static class DictExt { public static V SafeGet<K,V>(this Dictionary<K,V> d,K k){V v;return d.TryGetValue(k,out v)?v:default(V);} }
}
namespace LibraryOfAngela {
    public class AutoDestruct {public float time;}
    public static class Logger {public static List<string> messages=new List<string>(); public static void Log(string s,bool b=false){messages.Add(s);} public static void LogError(Exception e){Console.WriteLine(e);} }
    public static class PathProvider {public static string ConvertValidPath(string p,string path)=>path;}
    public static class LoAFramework {public static bool DEBUG=false;}
    public static class InternalExtension {public static void SetRange(Type t){} }
    public static class SkinRenderPatch {public static void RestoreRecycleableBundles(){} public static void RestoreBundleLoaded(AssetBundleInfo i){} }
    public class LoAArtworks:Singleton<LoAArtworks> {public static Action<AssetBundleInfo> onLoaded; public void OnAssetBundleLoaded(AssetBundleInfo i,string p,UnityEngine.AssetBundle b){onLoaded?.Invoke(i);} public void OnAssetBundleUnloaded(AssetBundleInfo i){} }
    public class TestConfig {public string packageId="heukhyeon.LCAngela.R";public List<AssetBundleInfo> infos=new List<AssetBundleInfo>();public List<AssetBundleInfo> GetAssetBundleInfos()=>infos;}
    public class TestMod {public TestConfig AssetBundleConfig=new TestConfig();}
    public class LoAModCache {public static List<TestMod> Instance=new List<TestMod>();}
}
