using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace KO.HollowKnight8 {
internal static class DoorRouteFix {
 struct Route {internal string Scene,Entry;}
 static readonly Dictionary<TransitionPoint,Route> routes=new Dictionary<TransitionPoint,Route>();
 static readonly HashSet<int> reported=new HashSet<int>();
 static int frame=-1,sceneHandle=-1;
 static bool Loadable(string name){if(string.IsNullOrEmpty(name))return false;try{return Application.CanStreamedLevelBeLoaded(name);}catch{return false;}}
 static Route Read(TransitionPoint gate){
  if(!gate)return default(Route);
  int scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
  if(sceneHandle!=scene){routes.Clear();reported.Clear();sceneHandle=scene;frame=-1;}
  if(frame!=Time.frameCount){routes.Clear();frame=Time.frameCount;}
  Route route;if(routes.TryGetValue(gate,out route))return route;
  string nativeScene=null,nativeEntry=null;bool ambiguous=false;
  if(gate.isADoor){
   // Only this door and its own children. Never borrow a nearby/sibling door's destination.
   foreach(var fsm in gate.GetComponentsInChildren<PlayMakerFSM>(true)){
    if(!fsm||fsm.Fsm==null)continue;GameManager.SceneLoadInfo info;
    if(!Doorways.Destination(fsm.Fsm,out info))continue;
    if(nativeScene!=null&&(nativeScene!=info.SceneName||nativeEntry!=info.EntryGateName)){ambiguous=true;break;}
    nativeScene=info.SceneName;nativeEntry=info.EntryGateName;
   }
  }
  bool valid=DoorRouteRules.Resolve(gate.targetScene,gate.entryPoint,nativeScene,nativeEntry,ambiguous,Loadable,out route.Scene,out route.Entry);
  routes[gate]=route;
  if((!valid||route.Scene!=gate.targetScene||route.Entry!=gate.entryPoint)&&reported.Add(gate.GetInstanceID()))
   Diagnostics.Write("DOOR ROUTE "+(valid?"normalized":"deferred to native")+" object="+gate.name+" component="+gate.targetScene+"/"+gate.entryPoint+" route="+route.Scene+"/"+route.Entry);
  return route;
 }
 internal static string GateScene(TransitionPoint gate){return Read(gate).Scene;}
 internal static string GateEntry(TransitionPoint gate){return Read(gate).Entry;}
 internal static bool BeforeTransition(GameManager.SceneLoadInfo info){
  var s=Plugin.Self==null?null:Plugin.Self.Session;
  if(s==null||!s.Active||info==null||info.GetType()!=typeof(GameManager.SceneLoadInfo)||!DoorRouteRules.GateName(info.SceneName)||Loadable(info.SceneName))return true;
  string scene,entry;
  if(DoorRouteRules.Resolve(info.SceneName,info.EntryGateName,null,null,false,Loadable,out scene,out entry)){
   Diagnostics.Write("DOOR ROUTE request normalized "+info.SceneName+"/"+info.EntryGateName+" -> "+scene+"/"+entry);
   // Preserve camera, timing, visualization and all other native transition settings.
   info.SceneName=scene;info.EntryGateName=entry;return true;
  }
  // Stop before voting, fading, suspending the roster or starting the asynchronous loader.
  Diagnostics.Write("DOOR ROUTE rejected gate-as-scene "+info.SceneName+"/"+info.EntryGateName);
  return false;
 }
}
}
