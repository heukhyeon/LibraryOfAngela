using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
class VerifyGameIL {
 static string[] dirs;
 static int Main(string[] args) {
  dirs=args;
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=> {
   var name=new AssemblyName(e.Name).Name+".dll";
   foreach(var dir in dirs) { var path=Path.Combine(dir,name); if(File.Exists(path)) return Assembly.LoadFrom(path); }
   return null;
  };
  try { return Verify(); }
  catch(Exception e) { for(var x=e;x!=null;x=x.InnerException) { Console.WriteLine(x.GetType().FullName); Console.WriteLine(x.Message); Console.WriteLine(x.StackTrace); } return 1; }
 }
 static int Verify() {
  var game=Assembly.LoadFrom(Path.Combine(dirs[2],"Assembly-CSharp.dll"));
  var runtime=Assembly.LoadFrom(Path.Combine(dirs[0],"LoARuntime.dll"));
  var patch=runtime.GetType("LibraryOfAngela.Battle.AutoBattlePatch",true);
  Check(game,patch,"BattleAllyCardDetail","PlayTurnAutoForPlayer",new[]{"SortCandidates","ResolvePlayerTarget","ResolveTargetSlot","AvailableForPlayer"});
  Check(game,patch,"BattleAllyCardDetail","PlayTurnAutoForEnemy",new[]{"SortCandidates","ResolveEnemyTarget","ResolveTargetSlot"});
  Check(game,patch,"BattleUnitModel","CheckCardAvailable",new[]{"AllowAutoSelection"});
  Check(game,patch,"EnemyUnitTargetSetter","SelectTargetUnit",new[]{"SelectSuggestedTarget"});
  Console.WriteLine("PASS installed game DLL transpiler anchors and control-flow stack verification"); return 0;
 }
 static void Check(Assembly game,Type patch,string type,string name,string[] required) {
  var original=AccessTools.Method(game.GetType(type),name);
  var generator=PatchProcessor.CreateILGenerator(original);
  var before=PatchProcessor.GetOriginalInstructions(original,generator);
  var trans=AccessTools.Method(patch,"Trans_"+name);
  object[] args=trans.GetParameters().Length==2?new object[]{before,generator}:new object[]{before};
  var after=((IEnumerable<CodeInstruction>)trans.Invoke(null,args)).ToList();
  foreach(var need in required) if(!after.Any(c=>(c.operand as MethodInfo)?.Name==need)) throw new Exception(name+" missing "+need);
  VerifyStack(after,original);
  Console.WriteLine("VERIFIED "+type+"."+name+" ("+after.Count+" instructions)");
 }
 static int PopCount(StackBehaviour b) {
  switch(b) {
   case StackBehaviour.Pop0:return 0;
   case StackBehaviour.Pop1:case StackBehaviour.Popi:case StackBehaviour.Popref:return 1;
   case StackBehaviour.Pop1_pop1:case StackBehaviour.Popi_pop1:case StackBehaviour.Popi_popi:case StackBehaviour.Popi_popi8:case StackBehaviour.Popi_popr4:case StackBehaviour.Popi_popr8:case StackBehaviour.Popref_pop1:case StackBehaviour.Popref_popi:return 2;
   case StackBehaviour.Popi_popi_popi:case StackBehaviour.Popref_popi_pop1:case StackBehaviour.Popref_popi_popi:case StackBehaviour.Popref_popi_popi8:case StackBehaviour.Popref_popi_popr4:case StackBehaviour.Popref_popi_popr8:case StackBehaviour.Popref_popi_popref:return 3;
   default:throw new Exception("Unknown pop "+b);
  }
 }
 static int PushCount(StackBehaviour b) {
  switch(b) {
   case StackBehaviour.Push0:return 0;
   case StackBehaviour.Push1:case StackBehaviour.Pushi:case StackBehaviour.Pushi8:case StackBehaviour.Pushr4:case StackBehaviour.Pushr8:case StackBehaviour.Pushref:return 1;
   case StackBehaviour.Push1_push1:return 2;
   default:throw new Exception("Unknown push "+b);
  }
 }
 static void VerifyStack(List<CodeInstruction> codes,MethodBase original) {
  var labels=new Dictionary<Label,int>(); for(int i=0;i<codes.Count;i++) foreach(var l in codes[i].labels) labels[l]=i;
  var heights=new Dictionary<int,int>(); var queue=new Queue<KeyValuePair<int,int>>(); queue.Enqueue(new KeyValuePair<int,int>(0,0));
  // Exception handlers are independent roots.
  for(int i=0;i<codes.Count;i++) foreach(var b in codes[i].blocks) {
   if(b.blockType==ExceptionBlockType.BeginFinallyBlock || b.blockType==ExceptionBlockType.BeginFaultBlock) queue.Enqueue(new KeyValuePair<int,int>(i,0));
   if(b.blockType==ExceptionBlockType.BeginCatchBlock || b.blockType==ExceptionBlockType.BeginExceptFilterBlock) queue.Enqueue(new KeyValuePair<int,int>(i,1));
  }
  while(queue.Count>0) {
   var item=queue.Dequeue(); int i=item.Key,h=item.Value;
   if(i>=codes.Count) throw new Exception("Fall-through past body");
   if(heights.TryGetValue(i,out var seen)) { if(seen!=h) throw new Exception("Inconsistent stack at "+i+": "+seen+" vs "+h); continue; } heights[i]=h;
   var c=codes[i]; int pop,push;
   if(c.opcode==OpCodes.Ret) { pop=(original as MethodInfo)?.ReturnType==typeof(void)?0:1; push=0; }
   else if(c.opcode==OpCodes.Call || c.opcode==OpCodes.Callvirt || c.opcode==OpCodes.Newobj) {
    var m=(MethodBase)c.operand; pop=m.GetParameters().Length+(!m.IsStatic && c.opcode!=OpCodes.Newobj?1:0);
    push=c.opcode==OpCodes.Newobj || (m is MethodInfo && ((MethodInfo)m).ReturnType!=typeof(void))?1:0;
   } else { pop=PopCount(c.opcode.StackBehaviourPop); push=PushCount(c.opcode.StackBehaviourPush); }
   if(h<pop) throw new Exception("Stack underflow at "+i+" "+c); h=h-pop+push;
   if(c.opcode==OpCodes.Ret) { if(h!=0) throw new Exception("Unbalanced ret at "+i); continue; }
   if(c.opcode==OpCodes.Throw || c.opcode==OpCodes.Rethrow || c.opcode==OpCodes.Endfinally || c.opcode==OpCodes.Endfilter) continue;
   if(c.opcode==OpCodes.Leave || c.opcode==OpCodes.Leave_S) h=0;
   if(c.operand is Label) queue.Enqueue(new KeyValuePair<int,int>(labels[(Label)c.operand],h));
   if(c.operand is Label[]) foreach(var l in (Label[])c.operand) queue.Enqueue(new KeyValuePair<int,int>(labels[l],h));
   if(c.opcode.FlowControl!=FlowControl.Branch) queue.Enqueue(new KeyValuePair<int,int>(i+1,h));
  }
 }
}
