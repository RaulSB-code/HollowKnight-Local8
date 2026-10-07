using System;
using System.Collections;
using GlobalEnums;
using UnityEngine;
namespace KO.HollowKnight8 {
// StartRecoil launches GameManager.FreezeMoment on the *hero*. StopAllCoroutines
// on a fallen knight used to interrupt that shared time ramp before its counter
// and time scale were restored, freezing every surviving actor and the boss.
// Keep the exact native hit-stop, hosted by the persistent GameManager instead.
internal static class HitStopIsolation {
 [ThreadStatic] static HeroController recoiling;
 static bool installed;
 static GameManager sharedHost;static Coroutine sharedStop;static bool running;static int generation;
 sealed class RecoilRoutine : IEnumerator,IDisposable {
  readonly IEnumerator original;readonly HeroController hero;
  internal RecoilRoutine(IEnumerator routine,HeroController owner){original=routine;hero=owner;}
  public object Current=>original.Current;
  public bool MoveNext(){var previous=recoiling;recoiling=hero;try{return original.MoveNext();}finally{recoiling=previous;}}
  public void Reset(){throw new NotSupportedException();}
  public void Dispose(){var disposable=original as IDisposable;if(disposable!=null)disposable.Dispose();}
 }
 internal static IEnumerator Wrap(IEnumerator original,HeroController hero){
  var s=Plugin.Self?Plugin.Self.Session:null;
  return original!=null&&hero&&s!=null&&s.Active&&s.Resolve(hero)!=null?new RecoilRoutine(original,hero):original;
 }
 internal static void Install(){if(installed)return;installed=true;On.GameManager.FreezeMoment_float_float_float_float+=Freeze;}
 internal static void Uninstall(){if(!installed)return;installed=false;On.GameManager.FreezeMoment_float_float_float_float-=Freeze;recoiling=null;}
 static IEnumerator Freeze(On.GameManager.orig_FreezeMoment_float_float_float_float orig,GameManager gm,float down,float wait,float up,float speed){
  var routine=orig(gm,down,wait,up,speed);var s=Plugin.Self?Plugin.Self.Session:null;
  if(!gm||!recoiling||s==null||!s.Active||s.Resolve(recoiling)==null)return routine;
  return Shared(gm,routine);
 }
 static IEnumerator Shared(GameManager gm,IEnumerator routine){
  // The hero waits for the same native routine. Cancelling that wait on death
  // no longer cancels GameManager's time ramp. No synthetic unpause or reset.
  if(!gm||routine==null)yield break;
  // Simultaneous native ramps can both clamp their final scale to zero while
  // the other one's counter is still held. Share one ramp across overlapping
  // player hits; each actor keeps its own recoil/animation and waits for it.
  if(!running||!sharedHost||sharedHost!=gm){sharedHost=gm;running=true;sharedStop=gm.StartCoroutine(Complete(gm,routine,++generation));}
  yield return sharedStop;
 }
 static IEnumerator Complete(GameManager gm,IEnumerator routine,int lease){
  try{yield return gm.StartCoroutine(routine);}
  finally{if(generation==lease){running=false;sharedHost=null;sharedStop=null;}}
 }
 internal static void Tick(){
  var runtime=Plugin.Self;var s=runtime?runtime.Session:null;var gm=GameManager.instance;var ui=UIManager.instance;
  if(s==null||!s.Active||!s.Gameplay||!gm||gm.isPaused||gm.TimeSlowed||gm.IsLoadingSceneTransition||!ui||ui.uiState!=UIState.PLAYING||runtime.Panel||PlayerContext.Current!=null||ScriptedParty.Active||CoopEnding.Active||Charms.NativeMenuOpen||PickupCard.Owner!=null||InteractionRouter.ActivePlayer!=null||ShopMenuRouting.MenuVisible||StagMenuRouting.HasOwner||PvpMatch.BlocksInput)return;
  foreach(var p in s.Players){var h=p.Hero;
   if(!h||!p.Ready||!p.Connected||!p.Alive||p.Down||p.Hazard||p.InputBlocked||p.SpawnPending||p.ArenaTransfer||p.Reviving||Time.unscaledTime<p.WakeUntil||Time.unscaledTime-p.LastDamageAt<3f||h.controlReqlinquished||h.cState.dead||h.cState.transitioning||h.IsDreamReturning||h.inAcid||h.cState.swimming||p.AcidAssistActive||EmergencyWarp.Active(p)||TransitionVote.Holding(p)||BenchSeats.Seated(p)||ChallengeSequence.Holds(p)||RoleSystem.BlockHero(h))continue;
   if(!(h.cState.recoilFrozen||h.cState.recoiling)||h.hero_state!=ActorStates.no_input)continue;
   // An interrupted hurt routine can also leave the actor in no_input after
   // the shared time ramp has finished. Clear only that proven stale recoil,
   // without cancelling coroutines, spells, health or another player's state.
   using(PlayerContext.Enter(p)){
    h.cState.recoilFrozen=false;Reflect.Call(h,"CancelDamageRecoil");h.RegainControl();h.StartAnimationControl();
   }
   Diagnostics.Write("DAMAGE stale recoil released P"+(p.Index+1));
  }
 }
}
}
