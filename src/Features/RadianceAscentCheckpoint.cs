using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityScenes=UnityEngine.SceneManagement.SceneManager;
namespace KO.HollowKnight8 {
// A shared highest landed platform applies only to story Radiance's final
// climb. Never infer the phase or checkpoint from a jump, damage or camera.
internal static class RadianceAscentCheckpoint {
 static readonly List<PlayMakerFSM> controllers=new List<PlayMakerFSM>();
 static int sceneHandle=-1;static float nextScan;
 static bool climbing,known;static Vector3 highest;static int reachedBy;
 internal static void Reset(){controllers.Clear();sceneHandle=-1;nextScan=0;climbing=known=false;highest=Vector3.zero;reachedBy=-1;}
 static bool Scope(CoopSession s){
  var scene=UnityScenes.GetActiveScene();var gm=GameManager.instance;
  if(s==null||!s.Active||!s.Gameplay||!gm||gm.IsLoadingSceneTransition||scene.name!="Dream_Final_Boss"||PvpMatch.Running){Reset();return false;}
  if(sceneHandle!=scene.handle){Reset();sceneHandle=scene.handle;}return true;
 }
 static bool Controller(Fsm f){
  if(f==null||!f.GameObject||!f.GameObject.activeInHierarchy||f.GameObject.scene.handle!=sceneHandle||f.GameObject.GetComponentInParent<HeroController>())return false;
  if(f.Name!="Control"&&f.Name!="Phase Control"&&f.Name!="Ascend")return false;
  for(var t=f.GameObject.transform;t;t=t.parent)if(t.name!=null&&t.name.IndexOf("Radiance",StringComparison.OrdinalIgnoreCase)>=0)return true;
  return false;
 }
 static bool Starts(string state)=>state!=null&&(state.Equals("Ascend",StringComparison.OrdinalIgnoreCase)||state.StartsWith("Ascend ",StringComparison.OrdinalIgnoreCase)||state.Equals("Climb",StringComparison.OrdinalIgnoreCase)||state.StartsWith("Climb ",StringComparison.OrdinalIgnoreCase));
 static bool Ends(string state)=>state=="Init"||state=="Death"||state=="Dead"||state=="Ending"||state=="Final"||state=="Final Antic"||state=="Final Start"||state=="Phase 6";
 internal static void Observed(FsmState state){
  var s=Plugin.Self?Plugin.Self.Session:null;if(state==null||!Scope(s)||!Controller(state.Fsm))return;
  string name=state.Fsm.ActiveState==null?state.Name:state.Fsm.ActiveState.Name;
  if(Ends(name)){climbing=known=false;return;}
  if(!climbing&&(Starts(name)||Starts(state.Name))){climbing=true;known=false;Diagnostics.Write("RADIANCE final ascent checkpoint enabled via "+name);foreach(var p in s.Players)Record(s,p);}
 }
 internal static void Tick(){
  var s=Plugin.Self?Plugin.Self.Session:null;if(!Scope(s))return;
  if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+1f;controllers.Clear();foreach(var f in UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>())if(f&&f.enabled&&Controller(f.Fsm))controllers.Add(f);}
  foreach(var f in controllers)if(f&&f.enabled&&Controller(f.Fsm)&&f.Fsm.ActiveState!=null)Observed(f.Fsm.ActiveState);
 }
 internal static void Record(CoopSession s,PlayerSlot p){
  if(!Scope(s)||!climbing||p==null||!p.Hero||!p.Alive||!p.Ready||!p.Connected||p.InputBlocked||p.SpawnPending||p.ArenaTransfer||p.AcidAssistActive||p.Down||p.Hazard)return;
  var c=p.Hero.cState;
  if(!c.onGround||c.jumping||c.doubleJumping||c.falling||c.recoiling||c.transitioning||c.dead||p.Hero.controlReqlinquished||!p.HasSafePoint||p.SafeAt!=Time.unscaledTime)return;
  var at=p.Hero.transform.position;
  if((at-p.SafePoint).sqrMagnitude>.0025f||known&&at.y<=highest.y+.05f||!s.IsSafe(at,p)||!SpawnSafety.ClearAt(p,at))return;
  highest=at;known=true;reachedBy=p.Index;Diagnostics.Write("RADIANCE highest safe platform P"+(reachedBy+1)+" "+highest);
 }
 internal static bool BeforeRecovery(CoopSession s,PlayerSlot p,ref Vector3? requested){
  if(!Scope(s)||!climbing||!known||p==null||!p.Hero)return true;
  Physics2D.SyncTransforms();
  // A transient beam must not select the room entrance instead of this ledge.
  if(!s.InRoom(highest)||!s.IsSafe(highest,p)||!SpawnSafety.ClearAt(p,highest)){p.HazardUntil=Time.time+.25f;return false;}
  requested=highest;p.Vitals.HazardPoint=highest;return true;
 }
 internal static bool Allows(bool original,CoopSession s,PlayerSlot p,Vector3 at){
  // Encounter bounds describe the platform fight below. Only this verified
  // climb checkpoint can bypass that old arena clamp; wall checks stay intact.
  return original||Scope(s)&&climbing&&known&&(at-highest).sqrMagnitude<.0025f&&s.InRoom(at)&&s.IsSafe(at,p)&&SpawnSafety.ClearAt(p,at);
 }
}
}
