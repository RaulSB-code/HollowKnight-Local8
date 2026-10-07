using System;
using UnityEngine;
using GlobalEnums;
using Scenes=UnityEngine.SceneManagement.SceneManager;
namespace KO.HollowKnight8 {
// An inactive parked GameObject is not a death: Alive includes activeInHierarchy.
// Retain committed flights across that deliberate suspension.
// Room waiting and FinishSpawn reset native movement. Retain the flight that
// each knight actually had, before waiting hides/parks that knight.
internal static class CrystalDashTransit {
 sealed class Flight {
  internal PlayerSlot Player;internal HeroController Hero;internal string From,To,Gate;
  internal int Count;internal bool Committed,Entering;internal float Age,Attempt;
 }
 static readonly Flight[] flights=new Flight[8];
 static CoopSession Session=>Plugin.Self?Plugin.Self.Session:null;
 static bool Living(PlayerSlot p)=>p!=null&&p.Index>=0&&p.Index<8&&p.Hero&&!p.Down&&!p.Hero.cState.dead&&p.Connected&&!p.Hazard&&!p.Retiring&&!p.ArenaTransfer;
 static bool Ordinary(GameManager.SceneLoadInfo info){
  string from=Scenes.GetActiveScene().name;
  return info!=null&&info.GetType()==typeof(GameManager.SceneLoadInfo)&&!string.IsNullOrEmpty(info.SceneName)&&!string.IsNullOrEmpty(info.EntryGateName)&&
   !from.StartsWith("GG_",StringComparison.OrdinalIgnoreCase)&&!from.StartsWith("Dream_",StringComparison.OrdinalIgnoreCase)&&
   !info.SceneName.StartsWith("GG_",StringComparison.OrdinalIgnoreCase)&&!info.SceneName.StartsWith("Dream_",StringComparison.OrdinalIgnoreCase)&&
   (info.HeroLeaveDirection==GatePosition.left||info.HeroLeaveDirection==GatePosition.right)&&!BossSequenceController.IsInSequence&&!PvpMatch.Running&&!ScriptedParty.Active&&!CoopEnding.Active;
 }
 internal static void CaptureVote(PlayerSlot p,string room){
  if(!Living(p)||!p.Alive||!p.Ready||!p.Hero.cState.superDashing||EmergencyWarp.Active(p))return;
  flights[p.Index]=new Flight{Player=p,Hero=p.Hero,From=Scenes.GetActiveScene().name,To=room,Count=CrystalDashCoop.Participants(p)};
 }
 // Substitute only the already accepted PrepareTransition call in the native
 // route. No pending vote, door, dream, arena or cinematic can start a flight.
 internal static void Prepare(CoopSession s,PlayerSlot entrant,GameManager.SceneLoadInfo info){
  bool normal=Ordinary(info);
  if(s!=null)foreach(var p in s.Players){
   if(p.Index<0||p.Index>=8)continue;var old=flights[p.Index];flights[p.Index]=null;
   if(!normal||!Living(p)||EmergencyWarp.Active(p))continue;
   if(old==null||old.Player!=p||old.Hero!=p.Hero||old.Committed||old.From!=Scenes.GetActiveScene().name||old.To!=info.SceneName||old.Age>8){
    if(!p.Ready||!p.Hero.cState.superDashing)continue;
    old=new Flight{Player=p,Hero=p.Hero,Count=CrystalDashCoop.Participants(p),From=Scenes.GetActiveScene().name};
   }
   old.Committed=true;old.To=info.SceneName;old.Gate=info.EntryGateName;old.Age=0;flights[p.Index]=old;
  }
  s.PrepareTransition(entrant);
  if(normal&&s.Primary!=null&&s.Primary.Hero){
   // The vanilla entrance honours P1's own exit flag, not the winning voter's.
   s.Primary.Hero.exitedSuperDashing=flights[s.Primary.Index]!=null;
  }
 }
 internal static void Tick(){
  var s=Session;var gm=GameManager.instance;
  if(s==null||!s.Active){Reset();return;}if(gm&&gm.isPaused)return;
  for(int i=0;i<flights.Length;i++){
   var f=flights[i];if(f==null)continue;var p=f.Player;f.Age+=Mathf.Max(0,Time.unscaledDeltaTime);
   if(!Living(p)||p.Hero!=f.Hero||!s.Players.Contains(p)||s.TeamWipe||f.Age>20){Cancel(p);continue;}
   string room=Scenes.GetActiveScene().name;
   if(!f.Committed){if(room!=f.From||f.Age>8)Cancel(p);continue;}
   if(room!=f.To){if(room!=f.From)Cancel(p);continue;}
   if(!gm||gm.IsLoadingSceneTransition||!gm.HasFinishedEnteringScene||!s.Gameplay||!p.Ready||p.SpawnPending||!p.Hero.gameObject.activeInHierarchy)continue;
   if(p.InputBlocked||p.Vitals.AtBench||ScriptedParty.Active||CoopEnding.Active||EmergencyWarp.Active(p)){Cancel(p);continue;}
   if(p.Hero.cState.superDashing){CrystalDashCoop.ContinueFlight(p,f.Count);flights[i]=null;Diagnostics.Write("CRYSTAL room flight restored P"+(i+1)+" linked="+f.Count);continue;}
   if(f.Entering){f.Attempt+=Time.unscaledDeltaTime;if(f.Attempt>.4f){Cancel(p);using(PlayerContext.Enter(p))ActorRecovery.Reset(p);Diagnostics.Write("CRYSTAL room resume unavailable P"+(i+1)+"; control restored");}continue;}
   TransitionPoint gate=null;
   if(TransitionPoint.TransitionPoints!=null)foreach(var candidate in TransitionPoint.TransitionPoints)if(candidate&&candidate.name==f.Gate){gate=candidate;break;}
   if(!gate||(gate.GetGatePosition()!=GatePosition.left&&gate.GetGatePosition()!=GatePosition.right)||!p.Hero.proxyFSM||!p.Hero.superDash){Cancel(p);continue;}
   // Check the full body and a short forward sweep. Do not restart inside a
   // wall, nor move a stopped knight across terrain to make it fly again.
   bool right=gate.GetGatePosition()==GatePosition.left;
   Vector3 at=p.Hero.transform.position;
   if(!SpawnSafety.Path(p,at,at+new Vector3(right?.65f:-.65f,0,0))){Diagnostics.Write("CRYSTAL room resume blocked P"+(i+1)+" at="+at+"; terrain/hazard sweep");Cancel(p);continue;}
   f.Entering=true;f.Attempt=0;
   try{using(PlayerContext.Enter(p)){
    if(right)p.Hero.FaceRight();else p.Hero.FaceLeft();
    p.Hero.cState.transitioning=false;p.Hero.exitedSuperDashing=false;
    p.Hero.proxyFSM.SendEvent("HeroCtrl-EnterSuperDash");
   }
   if(p.Hero.cState.superDashing){CrystalDashCoop.ContinueFlight(p,f.Count);flights[i]=null;}
   }catch(Exception ex){Cancel(p);if(Living(p)&&p.Hero==f.Hero&&p.Ready)using(PlayerContext.Enter(p))ActorRecovery.Reset(p);Diagnostics.Throttled("CRYSTAL native room resume",ex);}
  }
 }
 internal static void Cancel(PlayerSlot p){if(p!=null&&p.Index>=0&&p.Index<8)flights[p.Index]=null;}
 internal static void Reset(){Array.Clear(flights,0,flights.Length);}
}
}
