using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;
using SceneApi=UnityEngine.SceneManagement.SceneManager;

namespace KO.HollowKnight8 {
// The existing sequence owns the ordered draws. Keep the party held after
// those draws while the native challenge runs its boss introduction, then
// share the native return animation and a single control-release boundary.
internal static class ChallengeAnimationSync {
 enum Phase {None,Drawing,Native,Sheathing}
 sealed class Actor {
  internal PlayerSlot Player;internal HeroController Hero;internal tk2dSpriteAnimator Animator;
  internal tk2dSpriteAnimationClip End;
  internal Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip> Completed;
  internal Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip,int> Triggered;
 }
 static readonly List<Actor> actors=new List<Actor>();
 static Phase phase;static PlayerSlot owner;static Fsm raw;static PlayMakerFSM native;
 static FsmState endState;static FsmEvent deferred,completion;static bool sending;
 static string scene;static float nativeAt,endAt,lastUnscaled;
 internal static bool Pending=>phase!=Phase.None;
 internal static bool Holds(bool original,PlayerSlot p){
  if(original)return true;
  if(!Pending||p==null||(p==owner&&phase!=Phase.Sheathing))return false;
  return actors.Any(a=>a.Player==p&&Valid(a));
 }
 internal static bool Recovery(bool original)=>original||Pending;
 static bool Valid(Actor a)=>a!=null&&a.Player!=null&&a.Hero&&a.Player.Hero==a.Hero&&a.Player.Alive&&a.Player.Ready&&!a.Player.Down&&!a.Player.Hazard;
 internal static void Observed(FsmState state,PlayerSlot resolved){
  if(Pending||!ChallengeSequence.active||state==null||state.Fsm!=ChallengeSequence.raw||state.Name!="Challenge")return;
  var s=Plugin.Self?Plugin.Self.Session:null;if(s==null)return;
  owner=ChallengeSequence.owner;raw=ChallengeSequence.raw;native=ChallengeSequence.native;
  scene=SceneApi.GetActiveScene().name;lastUnscaled=Time.unscaledTime;phase=Phase.Drawing;
  Add(owner);
  // Use the actual frozen guest list, so joining/replacing an actor during
  // the introduction cannot silently add someone to the sequence.
  foreach(var g in ChallengeSequence.guests)Add(g.Player);
  ChallengeDrawAudio.Begin(raw,owner);
 }
 static void Add(PlayerSlot p){
  if(p==null||!p.Hero)return;
  var animator=p.Hero.GetComponent<tk2dSpriteAnimator>();
  var a=new Actor{Player=p,Hero=p.Hero,Animator=animator};
  if(animator){
   a.Completed=animator.AnimationCompleted;a.Triggered=animator.AnimationEventTriggered;
   a.End=FindEnd(animator);
   // Cloned animators may still carry a callback to another player's FSM.
   // Guests are visual followers, never native animation-event senders.
   if(p!=owner){animator.AnimationCompleted=null;animator.AnimationEventTriggered=null;}
  }
  actors.Add(a);
 }
 static tk2dSpriteAnimationClip FindEnd(tk2dSpriteAnimator animator){
  var clip=animator.GetClipByName("Challenge End");if(Usable(clip))return clip;
  if(animator.Library!=null&&animator.Library.clips!=null)
   foreach(var c in animator.Library.clips)if(Usable(c)&&IsReturn(c.name))return c;
  // A skin can omit the return clip. Reverse that skin's own draw frames,
  // rather than displaying frames from the primary player's sprite atlas.
  var start=animator.GetClipByName("Challenge Start");if(!Usable(start))return null;
  return new tk2dSpriteAnimationClip{name="Local8 Challenge Return",fps=start.fps,wrapMode=tk2dSpriteAnimationClip.WrapMode.Once,frames=start.frames.Reverse().ToArray()};
 }
 static bool Usable(tk2dSpriteAnimationClip c)=>c!=null&&c.frames!=null&&c.frames.Length>0&&c.fps>0;
 static bool IsReturn(string name){
  if(string.IsNullOrEmpty(name)||name.IndexOf("Challenge",StringComparison.OrdinalIgnoreCase)<0)return false;
  return new[]{"End","Return","Recover","Finish","Sheath"}.Any(x=>name.IndexOf(x,StringComparison.OrdinalIgnoreCase)>=0);
 }
 // Replacement for the old Finish: it used to restore guests immediately
 // and reset the initiator only 1.5 seconds into the native introduction.
 internal static void DrawsFinished(CoopSession s){
  if(!Pending){ChallengeSequence.Clear(true);return;}
  phase=Phase.Native;nativeAt=Time.time;
  ChallengeSequence.Clear(false);
  ChallengeSequence.recoveryGraceUntil=Time.unscaledTime+20f;
  Diagnostics.Write("CHALLENGE ordered draws complete; awaiting native return");
  // PlayMaker transitions compare event identity, not its name. A fresh
  // FsmEvent("FINISHED") never matches the registered native transition.
  Send(FsmEvent.GetFsmEvent("FINISHED"));
  NoticeReturn();
 }
 static void Send(FsmEvent evt){
  if(raw==null||evt==null)return;
  try{sending=true;using(PlayerContext.Enter(owner)){
   string before=raw.ActiveState==null?"none":raw.ActiveState.Name;
   raw.Event(FsmEvent.GetFsmEvent(evt.Name));
   Diagnostics.Write("CHALLENGE native event="+evt.Name+" state="+before+" -> "+(raw.ActiveState==null?"none":raw.ActiveState.Name));
  }}
  catch(Exception e){Diagnostics.Throttled("CHALLENGE native continuation",e);}
  finally{sending=false;}
 }
 internal static bool Intercept(Fsm fsm,FsmEvent evt){
  if(sending||phase!=Phase.Sheathing||fsm!=raw||evt==null||fsm.ActiveState!=endState||evt.Name!=(completion==null?"FINISHED":completion.Name))return false;
  deferred=evt;return true;
 }
 internal static void AfterState(FsmState state){
  if(phase==Phase.Native)NoticeReturn();
 }
 static void NoticeReturn(){
  var a=actors.Find(x=>x.Player==owner);
  if(a!=null&&Valid(a)&&a.Animator&&Usable(a.Animator.CurrentClip)&&IsReturn(a.Animator.CurrentClip.name)){
   a.End=a.Animator.CurrentClip;BeginReturn("native animation");
  }
 }
 static void BeginReturn(string why){
  if(phase!=Phase.Native)return;
  float duration=.1f;
  foreach(var a in actors)if(Valid(a)&&Usable(a.End))duration=Mathf.Max(duration,Mathf.Clamp((float)a.End.frames.Length/a.End.fps,.1f,2.5f));
  phase=Phase.Sheathing;endState=raw==null?null:raw.ActiveState;completion=ReturnEvent(endState);endAt=Time.time+duration;
  foreach(var a in actors){if(!Valid(a))continue;
   try{using(PlayerContext.Enter(a.Player)){
    a.Hero.RelinquishControl();a.Hero.StopAnimationControl();ActorRecovery.Freeze(a.Player);
    if(a.Animator&&Usable(a.End)){
     // Restart at frame zero even if the native action just played this clip.
     // Override this animator's FPS, never the shared library/skin clip FPS.
     a.Animator.Play(a.End,.00001f,a.End.frames.Length/duration);
    }
   }}catch(Exception e){Diagnostics.Throttled("CHALLENGE return P"+(a.Player.Index+1),e);}
  }
  Diagnostics.Write("CHALLENGE synchronized return actors="+actors.Count+" duration="+duration.ToString("0.00")+" via="+why);
 }
 static FsmEvent ReturnEvent(FsmState state){
  if(state==null||state.Actions==null)return null;
  foreach(var action in state.Actions){if(action==null)continue;
   var field=action.GetType().GetField("clipName");var clip=field==null?null:field.GetValue(action) as FsmString;
   if(clip==null||!IsReturn(clip.Value))continue;
   var complete=action.GetType().GetField("animationCompleteEvent");
   return (complete==null?null:complete.GetValue(action) as FsmEvent)??FsmEvent.GetFsmEvent("FINISHED");
  }
  return null;
 }
 // The old draw scheduler uses unscaled time; suspend all its deadlines
 // during pause so it cannot skip clips while Unity's animator is paused.
 internal static bool BeforeTick(CoopSession s){
  float now=Time.unscaledTime,delta=Mathf.Max(0,now-lastUnscaled);lastUnscaled=now;
  bool paused=GameManager.instance&&(GameManager.instance.isPaused||Time.timeScale<=0);
  if(!paused)return false;
  if(ChallengeSequence.active){
   ChallengeSequence.started+=delta;ChallengeSequence.ownerDeadline+=delta;ChallengeSequence.nextAt+=delta;
   foreach(var g in ChallengeSequence.guests)if(g.Played)g.EndsAt+=delta;
  }
  return true;
 }
 internal static void Tick(CoopSession s){
  if(!Pending)return;
  if(s==null||!s.Active||!native||raw==null||scene!=SceneApi.GetActiveScene().name||GameManager.instance&&GameManager.instance.IsLoadingSceneTransition){Reset();return;}
  if(GameManager.instance&&(GameManager.instance.isPaused||Time.timeScale<=0))return;
  var initiator=actors.Find(x=>x.Player==owner);
  if(!Valid(initiator)){Release(false);return;}
  if(phase==Phase.Native){
   NoticeReturn();
   // Support challenges that regain control without an explicit return state.
   // A bounded fallback also recovers a missing native completion callback.
   if(phase==Phase.Native&&(!initiator.Hero.controlReqlinquished&&initiator.Hero.acceptingInput||Time.time-nativeAt>=3f))BeginReturn("native control/fallback");
  }
  if(phase==Phase.Sheathing&&Time.time>=endAt)Release(true);
 }
 static void Release(bool resume){
  var evt=deferred;deferred=null;
  // Advance the native state while everyone is still held; then release the
  // complete cohort in one tick, including the initiator.
  if(resume&&raw!=null&&raw.ActiveState==endState&&(evt!=null||completion!=null))Send(evt??completion);
  phase=Phase.None;
  foreach(var a in actors){if(!Valid(a))continue;
   try{using(PlayerContext.Enter(a.Player)){ActorRecovery.Reset(a.Player);CoopSession.RestoreLivingVisuals(a.Player);}
    a.Player.ProtectionUntil=Time.time+1f;
   }catch(Exception e){Diagnostics.Throttled("CHALLENGE shared release",e);}
  }
  Diagnostics.Write("CHALLENGE party return complete; controls released together");
  Clear();ChallengeSequence.recoveryGraceUntil=Time.unscaledTime+1.5f;
 }
 static void Clear(){
  ChallengeDrawAudio.Reset();
  foreach(var a in actors)if(a.Player!=owner&&a.Animator){
   if(a.Animator.AnimationCompleted==null)a.Animator.AnimationCompleted=a.Completed;
   if(a.Animator.AnimationEventTriggered==null)a.Animator.AnimationEventTriggered=a.Triggered;
  }
  actors.Clear();phase=Phase.None;owner=null;raw=null;native=null;endState=null;deferred=completion=null;scene=null;nativeAt=endAt=lastUnscaled=0;sending=false;
 }
 internal static void Reset(){
  if(Pending&&(!GameManager.instance||!GameManager.instance.IsLoadingSceneTransition)&&scene==SceneApi.GetActiveScene().name)Release(false);
  else Clear();
 }
}
}
