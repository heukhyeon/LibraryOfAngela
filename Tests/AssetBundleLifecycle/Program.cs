using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LibraryOfAngela;
using UI;
using UnityEngine;
class Pump:SynchronizationContext {
    Queue<Action> queue=new Queue<Action>();
    public override void Post(SendOrPostCallback cb,object state){queue.Enqueue(()=>cb(state));}
    public void Drain(int limit=100){while(queue.Count>0 && limit-->0)queue.Dequeue()();}
}
class Program {
    static Pump pump=new Pump(); static int failures;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Hook(string method,params object[] args){typeof(LoAAssetBundles).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}
    static void CompleteAll(){foreach(var r in AssetBundle.requests)if(!r.isDone)r.Complete();pump.Drain();}
    static LoAAssetBundles Setup(int id=6661,bool waitingCore=false,bool syncInvitation=false,bool readyCore=false){
        StageController.Instance.stage.ClassInfo.id=new LorId("heukhyeon.LCAngela.R",id);
        AssetBundle.loads=0;AssetBundle.failSync=false;AssetBundle.requests.Clear();AssetBundle.completeAsyncImmediately=false;
        StageController.Instance.wave.UnitList.Clear();Logger.messages.Clear();LoAArtworks.onLoaded=null;
        var info=new AssetBundleInfo {path="AssetBundle/fxyz/lobgelare_carmen",types=new AssetBundleType[]{
            new AssetBundleType.Invitation("heukhyeon.LCAngela.R",id){IsAsync=!syncInvitation},
            new AssetBundleType.CorePage("heukhyeon.LCAngela.R",1){IsAsync=true}}};
        var mod=new TestMod();mod.AssetBundleConfig.infos.Add(info);
        if(waitingCore){
            mod.AssetBundleConfig.infos.Add(new AssetBundleInfo{path="waiting-core",type=new AssetBundleType.CorePage("heukhyeon.LCAngela.R",9)});
            var unit=new UnitBattleDataModel();unit.unitData.bookItem.BookId=new LorId("heukhyeon.LCAngela.R",9);StageController.Instance.wave.UnitList.Add(unit);
        }
        if(readyCore)mod.AssetBundleConfig.infos.Add(new AssetBundleInfo{path="ready-core",types=new AssetBundleType[]{
            new AssetBundleType.CorePage("heukhyeon.LCAngela.R",9),new AssetBundleType.Invitation("heukhyeon.LCAngela.R",700)}});
        LoAModCache.Instance.Clear();LoAModCache.Instance.Add(mod);
        LoAAssetBundles.Instance=new LoAAssetBundles();LoAAssetBundles.Instance.Initialize();return LoAAssetBundles.Instance;
    }
    static int Count(object request)=>(int)request.GetType().GetField("requestCount",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(request);
    static AssetBundleNode Node()=>new AssetBundleNode("test",new AssetBundleInfo{path="test"});
    static void Test(string name,Action run){try{run();Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+e.GetBaseException().Message);}}
    static void Main(){SynchronizationContext.SetSynchronizationContext(pump);
        foreach(int id in new[]{64,6661}){
            Test("light-spread start button reentry stage "+id,()=>{
                var manager=Setup(id);
                for(int i=0;i<5;i++){
                    Hook("Before_LoadBattleScene");CompleteAll();
                    Check(manager.LoadAsset<GameObject>("heukhyeon.LCAngela.R","lobgelare_carmen_moveheaven",true)!=null,"entry "+(i+1)+" missing prefab");
                    var old=manager.currentForceSyncRequest;
                    Hook("After_OnChangeUIPhase",UIPhase.Battle,UIPhase.Sephirah);
                    Check(manager.currentForceSyncRequest==null,"ended reception retained preparation");
                    Check(manager.LoadAsset<GameObject>("heukhyeon.LCAngela.R","lobgelare_carmen_moveheaven",true)==null,"ended reception retained prefab");
                }
                Check(AssetBundle.loads==5,"same-ID entry duplicated/missed native loads");
            });
        }
        Test("ordinary start button reloads after teardown",()=>{
            var manager=Setup();
            for(int i=0;i<5;i++){
                Hook("Before_OnClickGameStart");Hook("Before_LoadBattleScene");CompleteAll();
                Check(manager.LoadAsset<GameObject>("heukhyeon.LCAngela.R","lobgelare_carmen_moveheaven",true)!=null,"ordinary entry missing prefab");
                Hook("After_OnChangeUIPhase",UIPhase.Battle,UIPhase.Sephirah);
            }
            Check(AssetBundle.loads==5,"ordinary start duplicated/missed native loads");
        });
        Test("complete async-only preparation immediately",()=>{
            var manager=Setup();Hook("Before_LoadBattleScene");
            Check(manager.currentForceSyncRequest==null,"completed preparation remained current");CompleteAll();
        });
        Test("cancel running request logs and ignores its late callback",()=>{
            var manager=Setup(waitingCore:true);Hook("Before_LoadBattleScene");
            var old=manager.currentForceSyncRequest;
            Check(old!=null&&old.IsValid(),"waiting core request was not running");
            var starts=BattleSceneRoot.Instance.starts;
            Hook("After_OnChangeUIPhase",UIPhase.Battle,UIPhase.Sephirah);
            Check(manager.currentForceSyncRequest==null,"cancel kept request current");
            Check(Logger.messages.Exists(x=>x.Contains("Cancel Previous AssetBundle Request")),"running cancellation missing log");
            var current=new LibraryOfAngela.AssetBundleData.ForceAsyncAssetBundleRequest{initRequire=true};manager.currentForceSyncRequest=current;
            CompleteAll();
            Check(ReferenceEquals(current,manager.currentForceSyncRequest),"old callback replaced current preparation");
            Check(BattleSceneRoot.Instance.starts==starts,"old callback resumed canceled battle");
        });
        Test("waiting core completes and resumes reserved battle",()=>{
            var manager=Setup(waitingCore:true);Hook("Before_LoadBattleScene");
            var request=manager.currentForceSyncRequest;Check(request!=null&&request.IsValid(),"missing waiter");
            var starts=BattleSceneRoot.Instance.starts;CompleteAll();
            Check(manager.currentForceSyncRequest==null,"finished waiter remained current");
            Check(BattleSceneRoot.Instance.starts==starts+1,"finished waiter did not resume battle");
        });
        Test("synchronous invitation does not decrement waiting core counter",()=>{
            var manager=Setup(waitingCore:true,syncInvitation:true);Hook("Before_LoadBattleScene");
            Check(manager.currentForceSyncRequest!=null&&manager.currentForceSyncRequest.IsValid(),"sync invitation prematurely completed waiting core");
            CompleteAll();Check(manager.currentForceSyncRequest==null,"waiting core never completed");
        });
        Test("loaded sibling does not subtract a pending core load",()=>{
            var manager=Setup(waitingCore:true,readyCore:true);
            manager.LoadAssetBundle(new AssetBundleType.Invitation("heukhyeon.LCAngela.R",700));
            Hook("Before_LoadBattleScene");var request=manager.currentForceSyncRequest;
            Check(request!=null&&request.IsValid(),"loaded sibling canceled the pending core wait");
            Check(Count(request)==1,"pending core was not counted exactly once");
            AssetBundle.requests[0].Complete();pump.Drain();
            Check(Count(request)==1&&ReferenceEquals(request,manager.currentForceSyncRequest),"background completion subtracted pending core");
            CompleteAll();Check(Count(request)==0&&manager.currentForceSyncRequest==null,"pending core did not finish at zero");
        });
        Test("inline core completion finishes at zero",()=>{
            var manager=Setup(waitingCore:true);AssetBundle.completeAsyncImmediately=true;
            var request=new LibraryOfAngela.AssetBundleData.ForceAsyncAssetBundleRequest{initRequire=true};manager.currentForceSyncRequest=request;
            bool registered=false;
            LoAArtworks.onLoaded=info=>{if(info.path=="waiting-core")registered=Count(request)==1;};
            request.CheckInvitation();request.CheckUnits();
            Check(registered,"inline core callback ran before registration");
            Check(Count(request)==0,"inline completion left an unmatched decrement");
            request.CompleteInitialization();Check(manager.currentForceSyncRequest==null,"inline completion failed to finish");
        });
        Test("synchronous invitation balances registration before inline callback",()=>{
            var manager=Setup(syncInvitation:true);
            var request=new LibraryOfAngela.AssetBundleData.ForceAsyncAssetBundleRequest{initRequire=true};manager.currentForceSyncRequest=request;
            bool registered=false;LoAArtworks.onLoaded=info=>registered=Count(request)==1;
            request.CheckInvitation();Check(registered,"sync completion ran before registration");
            Check(Count(request)==0,"sync invitation left a negative counter");
            request.CompleteInitialization();Check(manager.currentForceSyncRequest==null,"sync invitation failed to finish");
        });
        Test("alive completed request is preserved",()=>{
            var manager=Setup();Hook("Before_LoadBattleScene");CompleteAll();
            var current=new LibraryOfAngela.AssetBundleData.ForceAsyncAssetBundleRequest();current.CheckInvitation();manager.currentForceSyncRequest=current;
            int loads=AssetBundle.loads;Hook("Before_LoadBattleScene");
            Check(!current.IsDestroyed()&&ReferenceEquals(current,manager.currentForceSyncRequest),"alive completed request was reset");
            Check(AssetBundle.loads==loads,"alive bundle reloaded");
            Check(!Logger.messages.Exists(x=>x.Contains("Reset Destroyed AssetBundle Request")),"alive bundle reported destroyed");
        });
        Test("pending request is preserved",()=>{
            var manager=Setup(waitingCore:true);Hook("Before_LoadBattleScene");var current=manager.currentForceSyncRequest;
            int loads=AssetBundle.loads;Hook("Before_LoadBattleScene");
            Check(!current.IsDestroyed()&&ReferenceEquals(current,manager.currentForceSyncRequest)&&Count(current)==1,"pending request was reset");
            Check(AssetBundle.loads==loads,"pending native load duplicated");CompleteAll();
        });
        Test("all destroyed requests are reset and reentry reloads",()=>{
            var manager=Setup(waitingCore:true);Hook("Before_LoadBattleScene");var current=manager.currentForceSyncRequest;CompleteAll();
            Hook("After_OnChangeUIPhase",UIPhase.Battle,UIPhase.Sephirah);
            Check(current.IsDestroyed(),"fully unloaded request still active");
            manager.currentForceSyncRequest=current;
            Hook("Before_LoadBattleScene");
            Check(!ReferenceEquals(current,manager.currentForceSyncRequest),"destroyed request retained");CompleteAll();
            Check(AssetBundle.loads==4,"reentry did not reload destroyed bundles");
            Check(Logger.messages.FindAll(x=>x.Contains("Reset Destroyed AssetBundle Request")).Count==1,"expected one defensive reset log");
        });
        Test("one pending operation preserves request after another is unloaded",()=>{
            var manager=Setup(waitingCore:true);Hook("Before_LoadBattleScene");
            AssetBundle.requests[0].Complete();pump.Drain();
            typeof(LoAAssetBundles).GetMethod("RemoveAssetBundles",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{AssetBundleLoadingType.INVITATION});
            var current=manager.currentForceSyncRequest;
            Hook("Before_LoadBattleScene");
            Check(!current.IsDestroyed()&&ReferenceEquals(current,manager.currentForceSyncRequest)&&Count(current)==1,"partial unload reset pending request");
            Check(AssetBundle.loads==2,"request guard attempted per-bundle repair");
            Check((bool)typeof(LoAAssetBundles).GetMethod("Before_StartBattle",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{BattleSceneRoot.Instance})==false,"pending core wait lost");CompleteAll();
        });
        Test("one live completed bundle preserves partially destroyed request",()=>{
            var manager=Setup(waitingCore:true);Hook("Before_LoadBattleScene");var current=manager.currentForceSyncRequest;CompleteAll();
            typeof(LoAAssetBundles).GetMethod("RemoveAssetBundles",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{AssetBundleLoadingType.INVITATION});
            manager.currentForceSyncRequest=current;Hook("Before_LoadBattleScene");
            Check(!current.IsDestroyed()&&ReferenceEquals(current,manager.currentForceSyncRequest),"live completed core did not preserve request");
            Check(AssetBundle.loads==2,"request guard attempted cache recovery");
        });
        Test("repeated scene-load hook does not duplicate same reception",()=>{
            var manager=Setup();Hook("Before_LoadBattleScene");Hook("Before_LoadBattleScene");CompleteAll();
            Check(AssetBundle.loads==1,"duplicate request in one reception");
        });
        Environment.ExitCode=failures==0?0:1;
    }
}
