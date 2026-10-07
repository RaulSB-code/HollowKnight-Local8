using System;
using System.Collections.Generic;
using GlobalEnums;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace KO.HollowKnight8 {
// Own only the short visual transition. Bench saving, door access checks,
// party voting and scene loading remain in the existing implementations.
internal static class InteractionMotion {
 enum Phase { Seating, Seated, Standing, Door }
 sealed class Motion {
  internal PlayerSlot Player; internal HeroController Hero; internal CoopSession Session;
  internal tk2dSpriteAnimator Animator; internal Phase Phase; internal string Loop;
  internal Vector3 From,To,Ground; internal float Elapsed,Duration;
  internal bool LeaveRequested,WasBlocked; internal int Scene;
  internal GameManager Manager; internal GameManager.SceneLoadInfo Route;
 }
 static readonly Dictionary<PlayerSlot,Motion> motions=new Dictionary<PlayerSlot,Motion>();
 static readonly List<Motion> snapshot=new List<Motion>();
 static readonly List<Motion> resetSnapshot=new List<Motion>();
 static PlayerSlot Resolve(Component component){var s=Plugin.Self==null?null:Plugin.Self.Session;return s==null||!component?null:s.Resolve(component);}
 static tk2dSpriteAnimationClip Clip(tk2dSpriteAnimator a,params string[] names){
  if(!a)return null;foreach(string name in names){var clip=a.GetClipByName(name);if(clip!=null&&clip.frames!=null&&clip.frames.Length>0&&clip.fps>0)return clip;}return null;
 }
 static float Duration(tk2dSpriteAnimationClip clip){return InteractionMotionRules.Duration(clip==null?0:clip.frames.Length,clip==null?0:clip.fps);}
 static Motion Make(PlayerSlot p){return new Motion{Player=p,Hero=p.Hero,Session=Plugin.Self.Session,Animator=p.Hero.GetComponent<tk2dSpriteAnimator>(),Scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle};}
 // Replacement for the custom seat's instantaneous position assignment.
 internal static void BenchPlace(Transform target,Vector3 at){
  var p=Resolve(target);if(p==null){target.position=at;return;}
  var m=Make(p);m.Phase=Phase.Seating;m.From=target.position;m.Ground=m.From;m.To=at;motions[p]=m;
 }
 // 'Sit' is the native hop onto a bench; 'Sit Idle' is the seated loop.
 internal static void BenchPlay(tk2dSpriteAnimator animator,string loop){
  var p=Resolve(animator);Motion m;
  if(p==null||!motions.TryGetValue(p,out m)){animator.Play(loop);return;}
  m.Loop=loop;var clip=Clip(animator,"Sit","Sit Start","Sit Down");
  if(clip==null||clip.name==loop){m.Phase=Phase.Seated;m.Hero.transform.position=m.To;animator.Play(loop);return;}
  m.Duration=Duration(clip);animator.Play(clip.name);
  Diagnostics.Write("ANIMATION bench enter P"+(p.Index+1)+" clip="+clip.name);
 }
 // The seat still owns the actor while the animation plays. Its LateUpdate
 // must use the moving pose instead of snapping back to the final seat.
 internal static void BenchPosition(Transform target,Vector3 at){
  var p=Resolve(target);Motion m;
  if(p==null||!motions.TryGetValue(p,out m)){target.position=at;return;}
  if(m.Phase==Phase.Seating)m.To=at;
  if(m.Phase==Phase.Seated){target.position=at;m.To=at;return;}
  if(m.Phase!=Phase.Seating&&m.Phase!=Phase.Standing){target.position=at;return;}
  target.position=Position(m); 
 }
 static Vector3 Position(Motion m){
  float t=InteractionMotionRules.Progress(m.Elapsed,m.Duration);
  Vector3 at=Vector3.Lerp(m.From,m.To,InteractionMotionRules.Ease(t));
  at.y+=InteractionMotionRules.Hop(t);at.z=(float)CoopRules.PlayerDepth(m.Player.Index);return at;
 }
 internal static void BenchLeave(PlayerSlot p){
  Motion m;if(p==null||!motions.TryGetValue(p,out m)||!BenchSeats.Custom(p)){BenchSeats.Leave(p);return;}
  if(m.Phase==Phase.Standing)return;
  if(m.Phase==Phase.Seating){m.LeaveRequested=true;return;}
  var clip=Clip(m.Animator,"Get Off","Sit End");
  if(clip==null){BenchSeats.Leave(p);return;}
  m.Phase=Phase.Standing;m.From=p.Hero.transform.position;m.Elapsed=0;m.Duration=Duration(clip);
  float direction=p.Actions!=null&&p.Actions.moveVector.X<-.1f?-1f:1f;
  // Prefer the ground point used to board this bench. A clear nearby point
  // lets the exit face the requested side without crossing a wall.
  var wanted=new Vector3(m.From.x+direction*.65f,m.Ground.y,m.From.z);
  m.To=SpawnSafety.Path(p,m.From,wanted)?wanted:(SpawnSafety.ClearAt(p,m.Ground)?m.Ground:m.From);
  using(PlayerContext.Enter(p)){if(direction<0)p.Hero.FaceLeft();else p.Hero.FaceRight();p.Hero.StopAnimationControl();m.Animator.Play(clip.name);}
  Diagnostics.Write("ANIMATION bench leave P"+(p.Index+1)+" clip="+clip.name);
 }
 internal static void BenchReleased(PlayerSlot p){Motion m;if(p!=null&&motions.TryGetValue(p,out m)&&m.Phase!=Phase.Door)motions.Remove(p);}
 // Called only by Doorways after its original prompt, proximity, key and
 // CanInteract validation. Never intercept arbitrary story/dream transitions.
 internal static void Door(GameManager manager,GameManager.SceneLoadInfo route){
  var s=Plugin.Self==null?null:Plugin.Self.Session;var p=PlayerContext.Current;
  if(s==null||!s.Active||p==null||!p.Hero||!p.Alive||route==null||route.HeroLeaveDirection!=GatePosition.door){manager.BeginSceneTransition(route);return;}
  if(motions.ContainsKey(p))return;
  var m=Make(p);var clip=Clip(m.Animator,"Enter","Idle To Enter Door","Enter Door");
  if(clip==null){manager.BeginSceneTransition(route);return;}
  // A native FSM may already have started this exact animation. Let it keep
  // its own completion and loading sequence rather than playing it twice.
  if(m.Animator.CurrentClip!=null&&m.Animator.CurrentClip.name==clip.name&&p.Hero.controlReqlinquished){manager.BeginSceneTransition(route);return;}
  m.Phase=Phase.Door;m.Manager=manager;m.Route=route;m.From=m.To=p.Hero.transform.position;m.Duration=Duration(clip);m.WasBlocked=p.InputBlocked;
  motions[p]=m;p.InputBlocked=true;
  using(PlayerContext.Enter(p)){Revival.End(p);Reflect.Call(p.Hero,"CancelAttack");p.Hero.RelinquishControl();p.Hero.StopAnimationControl();ActorRecovery.Freeze(p);m.Animator.Play(clip.name);}
  p.ProtectionUntil=Mathf.Max(p.ProtectionUntil,Time.time+m.Duration+.15f);
  Diagnostics.Write("ANIMATION door enter P"+(p.Index+1)+" clip="+clip.name+" to="+route.SceneName+"/"+route.EntryGateName);
 }
 static bool Valid(Motion m){return Plugin.Self!=null&&Plugin.Self.Session==m.Session&&m.Session.Active&&m.Player.Hero==m.Hero&&m.Hero&&m.Player.Ready&&m.Player.Connected&&m.Player.Alive&&m.Scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;}
 static void ReleaseDoor(Motion m,bool restore){
  motions.Remove(m.Player);m.Player.InputBlocked=m.WasBlocked;
  if(restore&&m.Player.Hero==m.Hero&&m.Hero&&m.Player.Alive&&m.Player.Ready){using(PlayerContext.Enter(m.Player)){ActorRecovery.Reset(m.Player);CoopSession.RestoreLivingVisuals(m.Player);}}
 }
 internal static void Tick(){
  if(motions.Count==0)return;snapshot.Clear();snapshot.AddRange(motions.Values);
  foreach(var m in snapshot){
   Motion current;if(!motions.TryGetValue(m.Player,out current)||current!=m)continue;
   var gm=GameManager.instance;
   if(!Valid(m)||!gm||gm.IsLoadingSceneTransition){
    if(m.Phase==Phase.Door)ReleaseDoor(m,Valid(m)&&gm&&!gm.IsLoadingSceneTransition);else {motions.Remove(m.Player);if(m.Player.Hero==m.Hero&&m.Scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle&&gm&&!gm.IsLoadingSceneTransition)BenchSeats.Leave(m.Player);}continue;
   }
   if(gm.isPaused)continue;
   if(m.Phase==Phase.Seated)continue;
   if(m.Phase!=Phase.Door&&!BenchSeats.Custom(m.Player)){motions.Remove(m.Player);continue;}
   m.Elapsed+=Mathf.Max(0,Time.deltaTime);
   if(m.Phase!=Phase.Door)m.Hero.transform.position=Position(m);else ActorRecovery.Freeze(m.Player);
   if(m.Elapsed<m.Duration)continue;
   if(m.Phase==Phase.Seating){
    m.Hero.transform.position=m.To;m.Phase=Phase.Seated;
    if(m.Animator&&!string.IsNullOrEmpty(m.Loop))m.Animator.Play(m.Loop);
    if(m.LeaveRequested)BenchLeave(m.Player);
   }else if(m.Phase==Phase.Standing){m.Hero.transform.position=m.To;BenchSeats.Leave(m.Player);}
   else{
    // Restore only this actor before the existing vote captures its normal
    // state. The vote still hides entrants and chooses the same destination.
    ReleaseDoor(m,true);
    if(m.Manager&&!m.Manager.IsLoadingSceneTransition&&m.Player.Alive){using(PlayerContext.Enter(m.Player))m.Manager.BeginSceneTransition(m.Route);}
   }
  }
  snapshot.Clear();
 }
 internal static void ResetDoors(){
  resetSnapshot.Clear();resetSnapshot.AddRange(motions.Values);
  foreach(var m in resetSnapshot)if(m.Phase==Phase.Door)ReleaseDoor(m,false);
  resetSnapshot.Clear();
 }
 internal static void Reset(){ResetDoors();motions.Clear();resetSnapshot.Clear();}
}
}
