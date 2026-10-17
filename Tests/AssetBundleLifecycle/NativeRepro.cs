using System;
using System.IO;
using System.Linq;
using LibraryOfAngela.AssetBundleData;
using System.Reflection;
using System.Threading.Tasks;
using LibraryOfAngela;
using UnityEngine;
using UnityEditor;
using UI;
public static class NativeRepro {
    const string package="heukhyeon.LCAngela.R";
    const string key="lobgelare_carmen_moveheaven";
    const string path=@"C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\LCAngelaRLocal\Assemblies\AssetBundle\fxyz\lobgelare_carmen";
    static void Hook(string name,params object[] args){typeof(LoAAssetBundles).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("NATIVE PASS "+message);}
    static async Task WaitFor(Func<bool> done){var until=DateTime.UtcNow.AddSeconds(15);while(!done()&&DateTime.UtcNow<until)await Task.Delay(10);}
    public static async void Run(){
        try {
            AssetBundle.UnloadAllAssetBundles(true);
            var info=new AssetBundleInfo {path=path,types=new AssetBundleType[]{
                new AssetBundleType.Invitation(package,64){IsAsync=true},
                new AssetBundleType.Invitation(package,6661){IsAsync=true}}};
            var mod=new TestMod();mod.AssetBundleConfig.infos.Add(info);LoAModCache.Instance.Clear();LoAModCache.Instance.Add(mod);
            LoAAssetBundles.Instance=new LoAAssetBundles();var manager=LoAAssetBundles.Instance;manager.Initialize();
            foreach(int id in new[]{64,6661}){
                StageController.Instance.stage.ClassInfo.id=new LorId(package,id);
                for(int i=0;i<5;i++){
                    Hook("Before_LoadBattleScene");
                    await WaitFor(()=>manager.LoadAsset<GameObject>(package,key,true)!=null);
                    Check(manager.LoadAsset<GameObject>(package,key,true)!=null,"stage "+id+" light-spread entry "+(i+1));
                    Hook("After_OnChangeUIPhase",UIPhase.Battle,UIPhase.Sephirah);
                    Check(manager.LoadAsset<GameObject>(package,key,true)==null,"stage "+id+" teardown "+(i+1));
                }
            }
            StageController.Instance.stage.ClassInfo.id=new LorId(package,64);
            Hook("Before_LoadBattleScene");
            await WaitFor(()=>manager.LoadAsset<GameObject>(package,key,true)!=null);
            var bundle=AssetBundle.GetAllLoadedAssetBundles().Single();
            var current=new ForceAsyncAssetBundleRequest();current.CheckInvitation();
            manager.currentForceSyncRequest=current;
            Hook("Before_LoadBattleScene");
            Check(!current.IsDestroyed() && ReferenceEquals(manager.currentForceSyncRequest,current) && ReferenceEquals(bundle,AssetBundle.GetAllLoadedAssetBundles().Single()),"alive completed bundle and request preserved");
            Hook("After_OnChangeUIPhase",UIPhase.Battle,UIPhase.Sephirah);
            Check(bundle==null && !ReferenceEquals(bundle,null) && current.IsDestroyed(),"all request bundles destroyed; native null differs from managed null");
            manager.currentForceSyncRequest=current;
            Hook("Before_LoadBattleScene");
            await WaitFor(()=>manager.LoadAsset<GameObject>(package,key,true)!=null);
            Check(manager.LoadAsset<GameObject>(package,key,true)!=null && !ReferenceEquals(current,manager.currentForceSyncRequest),"fully destroyed stale request reset and reentry reloaded");
            Debug.Log("NATIVE RESULT PASS request state guard");EditorApplication.Exit(0);
        } catch(Exception e){Debug.LogError("NATIVE RESULT FAIL "+e);EditorApplication.Exit(1);}
    }
}
